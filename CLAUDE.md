# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

MyFinanceAPI — a .NET 8 backend for a personal finance application (accounts, spends/expenses, bank
transaction imports, transfers, loans, debt requests, currency conversion, AI-assisted expense
classification). There is no frontend in this repo; a separate Azure Static Web App
(`icy-sea-0f0fb8a10.2.azurestaticapps.net`) consumes this API and is allow-listed in CORS
(`MyFinanceWebApiCore/Startup.cs`), along with `localhost:4350` for local frontend dev.

## Commands

Solution file: `MyFinanceWebApi.sln`. All projects target `net8.0`.

```bash
# Build
dotnet build

# Run the API (from MyFinanceWebApiCore) — serves Swagger UI at /swagger
dotnet run --project MyFinanceWebApiCore

# Run all tests
dotnet test

# Run a single test (NUnit, by fully-qualified name)
dotnet test --filter "FullyQualifiedName~PeriodCreatorHelperTest"

# Apply EF Core migrations locally (context lives in EFDataAccess.Models)
dotnet ef database update --project MyFinanceWebApiCore --context EFDataAccess.Models.MyFinanceContext

# Add a new migration
dotnet ef migrations add <Name> --project EFDataAccess --startup-project MyFinanceWebApiCore --context EFDataAccess.Models.MyFinanceContext
```

The only test project is `EFDataAccessTest` (NUnit), and it currently covers just the EF data-access
helper layer — there is no test coverage for `MyFinanceBackend` (services) or `MyFinanceWebApiCore`
(controllers). Don't assume behavior is pinned by tests; check call sites when changing service logic.

CI (`.github/workflows/`) builds on push to `develop` and deploys to Azure Web Apps on `master`
(`dotnet-build.yml`, `master_myfinancewebapicore*.yml`). `EF_migrations.yml` is a manual
(`workflow_dispatch`) job that runs `dotnet ef database update` against the Azure SQL instance —
migrations aren't applied automatically on every push.

## Architecture

### Project layout and dependency direction

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

### Unit of Work + Repository pattern

`IUnitOfWork` (`MyFinanceBackend/Data/IUnitOfWork.cs`), implemented by `EFUnityOfWork`, aggregates all EF
repositories and exposes explicit transaction control: `StartTransactionAsync` / `CommitTransactionAsync`
/ `RollbackAsync` / `SaveAsync`. Services that need atomicity across multiple repositories manage this
manually (see `DebtRequestService` for the reference pattern: start transaction → mutate through multiple
repos → commit, with rollback on failure). Simpler services just call a repository and `SaveAsync`.

### Sub-services

Cross-cutting logic that's reused by more than one top-level service lives in a "sub-service"
(`I*SubService` / `*SubService` in `MyFinanceBackend/Services`), e.g. `IAppTransactionsSubService`
(creating/confirming app transactions by account, used by both transfers and debt requests) and
`IExpensesClassificationSubService` (GPT-based expense classification, used by bank transaction import
flows). Prefer extending an existing sub-service over duplicating logic across services.

### AI-assisted expense classification

`ExpensesClassificationSubService` classifies imported bank transactions into spend categories via GPT,
behind `IBankTrxCategorizationRepository` (impl: `GptBankTrxCategorizationRepository`, calls OpenAI's
chat completions API — configured under `OpenAI` in `appsettings.json`). Classification results are
cached in MongoDB (`IGptClassifiedExpensesCacheRepository`) keyed by normalized description/amount/
currency/account ownership, so previously-seen expenses skip the GPT call on subsequent imports. When
touching this flow, be aware there are two distinct outputs: cached lookups vs. fresh GPT classification,
merged before being returned to the caller.

### Financial-entity file import

Bank statement files (Excel) are parsed per financial institution via
`Dictionary<FinancialEntityFile, Type>` → `IFinancialEntityFileReader` factory registered in
`RegisterFileReaders` (`Startup.cs`). Adding support for a new bank means adding a new
`IFinancialEntityFileReader` implementation and a corresponding `FinancialEntityFile` enum entry, then
registering it in that dictionary — follow `ScotiabankFileReader` as the template. Financial entity IDs
(e.g. Scotiabank = 6) are DB-seeded values referenced as named constants in code, not magic numbers.

### Configuration

`appsettings.json` holds the schema (empty secrets); real values come from `appsettings.local.json`
(gitignored) or environment variables in deployment. Key sections: `ConnectionStrings:DefaultConnection`
(SQL Server), `ConnectionStrings:MongoDB`, `authentication:secret` (JWT signing), `OpenAI:ApiKey`.

### Exceptions

Controllers don't catch exceptions individually — `HttpResponseExceptionFilter`
(`MyFinanceWebApiCore/FilterAttributes`) centrally maps thrown `ServiceException`/custom exceptions
(`MyFinanceBackend/Exceptions`) to HTTP responses. Throw a typed exception from a service rather than
returning error codes or handling HTTP concerns in services.
