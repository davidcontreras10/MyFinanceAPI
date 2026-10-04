# Architecture

How the backend is organized. For what the app is for, see [product.md](product.md).

## Project layout and dependency direction

```
MyFinanceWebApiCore  (ASP.NET Core host: Controllers, Startup/Program, auth middleware, file readers)
        -> MyFinanceBackend    (services: business logic, repository interfaces, IUnitOfWork)
              -> EFDataAccess      (EF Core: MyFinanceContext, entities, migrations, repo impls, EFUnityOfWork)
              -> MongoDB           (Mongo repo impls — GPT classification cache)
        -> MyFinanceModel     (shared DTOs / client view models / enums, referenced by every layer)
```

- `MyFinanceBackend` defines repository *interfaces* (`Data/I*Repository.cs`); `EFDataAccess` and
  `MongoDB` provide the concrete implementations. Services in `MyFinanceBackend` never reference EF or
  Mongo types directly — only the interfaces and `IUnitOfWork`.
- All DI registration happens in one place: `MyFinanceWebApiCore/Startup.cs` →
  `RegisterServices`/`RegisterMongoDB`/`RegisterFileReaders`. When adding a new service or repository,
  register it there (services and repos are `Scoped`; the EF `DbContext` is scoped, Mongo `IMongoDatabase`
  is a singleton).

## Unit of Work + Repository pattern

`IUnitOfWork` (`MyFinanceBackend/Data/IUnitOfWork.cs`), implemented by `EFUnityOfWork`, aggregates all EF
repositories and exposes explicit transaction control: `StartTransactionAsync` / `CommitTransactionAsync`
/ `RollbackAsync` / `SaveAsync`. Services that need atomicity across multiple repositories manage this
manually (see `DebtRequestService` for the reference pattern: start transaction → mutate through multiple
repos → commit, with rollback on failure). Simpler services just call a repository and `SaveAsync`.

## Account hierarchy (main accounts and sub-accounts)

The rules are in [product.md](product.md#account-hierarchy-rules); this section is how they are implemented.

The product concept is **main account → sub-accounts**, but it is stored as `AccountInclude` rows on the
*child*: `AccountId` = the sub-account, `AccountIncludeId` = its main account (the name reads backwards),
plus a `CurrencyConverterMethodId` for when the two currencies differ.

- The structure rules (1–3) are enforced on create and edit by `AccountHierarchyValidator`.
  `accountIncludes` is still an array in the API, but a request with more than one entry is rejected with 400.
- The entity, currency and exchange-method rules (5–7) are enforced by `AccountLinkRules`, applied in
  `AccountService.AddAccountAsync` (create only). The server stores the method it decides and ignores the
  client's value unless the user has to choose. A violation is a 400 with a readable message.

Direction matters: `CurrencyConverter.CurrencyIdOne` is the source (the sub-account's currency) and
`CurrencyIdTwo` the target (the main account's). Default methods carry a placeholder entity id, so "same
currency" is handled before looking up by entity. The parent candidate list (`accountIncludeViewModels`)
already applies the same rules: `methodIds` has only the valid methods (one, pre-selected, when
determined), `requiresMethodChoice` says the user must pick, and `requiredFinancialEntityId` is the entity
a sub-account of that main account must have.

Adding a spend to a sub-account writes one `Spend` and one `SpendOnPeriod` per account involved (the
sub-account, flagged `IsOriginal`, and its main account), converting currency per link. These rows are
created when the spend is added, so changing the hierarchy later does not rewrite history.

`GET /api/Accounts/{accountGroupId}` and the edit view model return `parentAccountId`, `parentAccountName`
and `subAccounts` (each with its own `accountGroupId`, since they may not be in the requested group).
Parent candidates (`accountIncludeViewModels`) carry `hasParent`, so the UI can leave out accounts that are
already sub-accounts. Deleting a main account removes its links, which turns its sub-accounts into top-level
accounts; the UI warns about this using `subAccounts`.

## Sub-services

Cross-cutting logic that's reused by more than one top-level service lives in a "sub-service"
(`I*SubService` / `*SubService` in `MyFinanceBackend/Services`), e.g. `IAppTransactionsSubService`
(creating/confirming app transactions by account, used by both transfers and debt requests) and
`IExpensesClassificationSubService` (GPT-based expense classification, used by bank transaction import
flows). Prefer extending an existing sub-service over duplicating logic across services.

## AI-assisted expense classification

`ExpensesClassificationSubService` classifies imported bank transactions into spend categories via GPT,
behind `IBankTrxCategorizationRepository` (impl: `GptBankTrxCategorizationRepository`, calls OpenAI's
chat completions API — configured under `OpenAI` in `appsettings.json`). Classification results are
cached in MongoDB (`IGptClassifiedExpensesCacheRepository`) keyed by normalized description/amount/
currency/account ownership, so previously-seen expenses skip the GPT call on subsequent imports. When
touching this flow, be aware there are two distinct outputs: cached lookups vs. fresh GPT classification,
merged before being returned to the caller.

Fresh classification calls use `OpenAI:Model`, defaulting to `gpt-6-luna` with reasoning disabled.
To switch to the supported alternative, set `"Model": "gpt-4o-mini"` under `OpenAI` in
`appsettings.local.json`, or set the environment variable `OpenAI__Model=gpt-4o-mini`, then restart the API.
Use `gpt-6-luna` to switch back. Model selection is explicit, with no automatic fallback or second paid
call. Both models use standard service tier, JSON mode, temperature 0.2 and a 4096-token completion limit;
the reasoning parameter is omitted for GPT-4o mini. Unsupported model settings fail before calling OpenAI.
Changing this setting does not invalidate existing cached classifications; use the comparison endpoint
for fresh calls to both models without cache writes.

The GPT repository requests JSON mode (`response_format: json_object`) with an `expenses` array inside
the response object; parsing also accepts the older bare-array fixture in `FakeOpenAIResponse.json`.
It checks completion/refusal status, transaction IDs, allowed category/account IDs and confidence values
before returning results for caching. Original descriptions, amounts and currencies come from the bank data.
OpenAI failures become `ServiceException` responses (502, or 504 for timeouts), with the upstream status,
request ID, error code/type, finish reason and failure stage in `dataObject`. JSON parsing errors also include
the field path and line/position; warnings include those details without logging transaction payloads or API keys.

`GET /api/Tests/classify-expenses` uses fixed Scotiabank reference numbers to load pending bank transactions.
Categories, account hints and recent classification history come from the database for the authenticated user.
It can return an empty list when those references are no longer pending, or skip OpenAI when all results are cached.

`POST /api/Tests/classify-expenses/compare?financialEntityId=6&sampleSize=10` compares `gpt-4o-mini`
with `gpt-6-luna` (`reasoning_effort: none`). It replays up to 25 processed, user-owned transactions from
the last three months, loading categories, hinted accounts and history once. Both calls share identical
messages, JSON mode, temperature and a 4096-token output limit, use standard service tier, and run sequentially
without retries. It does not read or write the classification cache or modify transactions; each invocation
does make two paid OpenAI calls. It does not change the configured default model.
The response includes inputs and each model's classifications, elapsed HTTP-call time, returned model,
request ID, input/output/cached/cache-write/reasoning token counts, and estimated USD cost. A model failure
does not suppress the other result, and reported usage is retained even when classification validation fails.
Cost uses standard public rates dated `PricingAsOf`, not an invoice: uncached input, cached input and cache
writes are charged separately; completion tokens already include reasoning tokens, so those are not added
again. Missing usage or a nonstandard returned service tier leaves cost unknown rather than reporting zero.
Rates must be reviewed when pricing changes. Single-run latency and cost are observations, not benchmarks
or guarantees, and classification accuracy is not scored.

Account eligibility comes from `Account.AiClassificationHint`: only the current user's accounts with a
nonblank hint are offered to GPT. A hint describes which transactions belong in that account, with useful
merchant examples and exclusions. Spend categories come separately from the user's spend types; account
notes and default spend categories do not substitute for a hint. The dedicated account endpoint
`PUT /api/Accounts/{accountId}/ai-classification-hint` sets or clears this field for the authenticated
owner; GET on the same route reads the hint, including accounts with no hint.
Normal account create/edit DTOs remain unchanged. See [account-ai-hints.md](account-ai-hints.md)
for the request contract. Clearing a hint removes the account from fresh AI candidate lists, not from
existing classifications or cached results.
Current account hints take precedence over historical examples. Identical descriptions, such as digital-service
IVA, can route to different accounts depending on the associated purchase's amount and currency.
Prompts serialize categories, accounts and inputs as separate JSON lists; historical examples provide category
guidance while current hints govern account routing.
The prompt separates classification rules and output requirements from the data sections. Routing hints
cannot override the output contract or allowed IDs; individual transaction results may use other transactions
in the batch to identify a separately charged tax's related purchase.
Each input also carries allowed historical category IDs matching its normalized description and currency.
These are prompt evidence, not a server-side override; conflicting historical categories still require judgment.
Digital-service IVA batches bypass cache lookups so GPT sees the accompanying purchases, including those
already cached. IVA results themselves are not cached: description/amount/currency alone cannot identify
which purchase's account to use. Other results in the batch are still cached.

## Financial-entity file import

Bank statement files (Excel) are parsed per financial institution via
`Dictionary<FinancialEntityFile, Type>` → `IFinancialEntityFileReader` factory registered in
`RegisterFileReaders` (`Startup.cs`). Adding support for a new bank means adding a new
`IFinancialEntityFileReader` implementation and a corresponding `FinancialEntityFile` enum entry, then
registering it in that dictionary — follow `ScotiabankFileReader` as the template. Financial entity IDs
(e.g. Scotiabank = 6) are DB-seeded values referenced as named constants in code, not magic numbers.

## Configuration

`appsettings.json` holds the schema (empty secrets); real values come from `appsettings.local.json`
(gitignored) or environment variables in deployment. Key sections: `ConnectionStrings:DefaultConnection`
(SQL Server), `ConnectionStrings:MongoDB`, `authentication:secret` (JWT signing), `OpenAI:ApiKey`.

For local debugging in Visual Studio, select the `Local` launch profile. The default `IIS Express`
and `MyFinanceWebApiCore` profiles use `Development`, which does not load `appsettings.local.json`.
The runtime uses the environment-specific configuration file, not an unconditional local override.
If MongoDB is running but the API cannot connect, check the active environment and effective connection
string first; a successful shell ping alone does not verify the API's configuration.

## Exceptions

Controllers don't catch exceptions individually — `HttpResponseExceptionFilter`
(`MyFinanceWebApiCore/FilterAttributes`) centrally maps thrown `ServiceException`/custom exceptions
(`MyFinanceModel/Exceptions.cs`) to HTTP responses. Throw a typed exception from a service rather than
returning error codes or handling HTTP concerns in services.

## Legacy code

`MyFinanceBackend/Data` also holds ten non-EF repository classes (`AccountRepository`, `SpendsRepository`,
`AccountGroupRepository`, `AutomaticTaskRepository`, `LoanRepository`, `TransferRepository`, `UserRepository`,
`SpendTypeRepository`, `ResourceAccessRepository`, `AuthorizationDataRepository`) that call SQL Server
stored procedures. They predate the EF repositories, are not registered in `Startup.cs` and nothing
instantiates them, so they are dead code. Don't edit them to change behavior; edit the matching
`EF*Repository` in `EFDataAccess`. They still implement the repository interfaces, so adding a member to an
interface means adding a `throw new NotImplementedException()` stub to the legacy class so the project compiles.
The stored procedures themselves live in the database, not in this repo.

## CI / deployment

**Production is the only environment.** There is no dev or staging server for the API or the UI; a push to
`master` is a production release.

- Push to `develop`: `dotnet-build.yml` only builds.
- Push to `master`: `master_myfinancewebapicore_ef.yml` builds, runs `dotnet ef database update` against the
  Azure SQL database, then publishes and deploys (the deploy job waits for the build job, which includes the
  migration). So **migrations in a release are applied automatically before the new code is deployed.**
- `master_myfinancewebapicore.yml` also triggers on push to `master` and deploys without migrating.
  `EF_migrations.yml` is a manual (`workflow_dispatch`) migration job.

A local database (LocalDB, configured in the gitignored `appsettings.local.json`) is only for trying things
out; it is a copy and lags behind production's migrations.

## Defaults for new accounts

`GetAddAccountViewModelAsync` marks the `PeriodDefinition` with `IsDefault` as the selected period type, and
returns `suggestedAccountTypeIdForMainAccount` / `suggestedAccountTypeIdForSubAccount` from
`AccountTypeSuggestions` (null when that type isn't active). The UI applies them; see `docs/product.md`
("Defaults when creating an account").

Note: the code already treats `PeriodDefinitionId == 2` as the basic monthly period in a few places
(`IsBasicMontly` in `EFAccountRepository`, `PeriodTypeId == 2` in `PeriodCreatorHelper`,
`PeriodTypeViewModel.IsFriendlyMonthlyName`). The add form's default period does not depend on that: it uses
the `PeriodDefinition.IsDefault` flag.

## Migrations

Never write or edit a migration by hand. Change the model (entities, and the Fluent API in `MyFinanceContext`
for anything the migration must carry, such as defaults or constraints), then generate the migration with
the command (see `AGENTS.md`). First run `dotnet ef migrations has-pending-model-changes` so the generated
migration contains only your change. Applying it to production is the release workflow's job. To apply it to
a local database, set `ASPNETCORE_ENVIRONMENT=Local` so the design-time factory reads `appsettings.local.json`
(see `AGENTS.md`).

Gotcha with boolean flags that default to true (such as `AccountType.IsActive`): EF treats `false`, the CLR
default, as "not set" when it inserts a row, and stores the database default (true) instead. Rows of these lookup
tables aren't inserted through EF, so change such flags with SQL (`UPDATE ... SET IsActive = 0 ...`).
