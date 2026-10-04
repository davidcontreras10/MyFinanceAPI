# AGENTS.md

Instructions for AI coding tools (Claude Code, Codex, Copilot, …) working in this repository. This is the
canonical file; tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`) only point here.

## Keeping this documentation up to date

If you learn something a future session would need (a new feature, a domain rule, a convention, a gotcha,
a changed command), record it in the repository docs — this file or a file under `docs/` — not in a
tool-specific file (`CLAUDE.md`, `.github/copilot-instructions.md`, a tool's memory, etc.). Those files
only point here. Keep additions short and durable; skip anything obvious from the code or likely to go
stale. Put product/domain facts in `docs/product.md` and structure/technical facts in
`docs/architecture.md`. The same applies to the sibling repository's docs when the fact belongs there.

## What this is

MyFinanceAPI — a .NET 8 backend for a personal finance app (accounts and sub-accounts reconciled against
one real bank account, transactions, bank statement import, transfers, loans, debt requests, currency
conversion, AI-assisted expense classification). Solo, non-commercial project.

## Read first

- [docs/product.md](docs/product.md) — what the app is for, features, glossary (note: "Spend" means any
  transaction, expense or income).
- [docs/architecture.md](docs/architecture.md) — project layout, UoW/repository pattern, sub-services,
  GPT classification, file import, configuration, exceptions, CI.
- Other files in [docs/](docs/) are specs for individual features.

## Related repository

The frontend is a separate repo: `myfinance-ui` (Angular 16), deployed as an Azure Static Web App
(`icy-sea-0f0fb8a10.2.azurestaticapps.net`). It is allow-listed in CORS in `MyFinanceWebApiCore/Startup.cs`,
along with `localhost:4350` for local frontend dev. Changing an API contract (routes, DTOs in
`MyFinanceModel`) usually means a matching UI change. API JSON responses are camelCase.

## Commands

Solution: `MyFinanceWebApi.sln`. All projects target `net8.0`.

```bash
dotnet build
dotnet run --project MyFinanceWebApiCore            # Swagger UI at /swagger
dotnet test
dotnet test --filter "FullyQualifiedName~PeriodCreatorHelperTest"   # single NUnit test

# Apply EF Core migrations to the local database (needs ASPNETCORE_ENVIRONMENT=Local, see below)
ASPNETCORE_ENVIRONMENT=Local dotnet ef database update --project EFDataAccess --startup-project MyFinanceWebApiCore --context EFDataAccess.Models.MyFinanceContext

# Check first that the model has no unrelated pending changes, then add a migration
dotnet ef migrations has-pending-model-changes --project EFDataAccess --startup-project MyFinanceWebApiCore --context EFDataAccess.Models.MyFinanceContext
dotnet ef migrations add <Name> --project EFDataAccess --startup-project MyFinanceWebApiCore --context EFDataAccess.Models.MyFinanceContext
```

**Migrations: never write or edit one by hand.** Generate it with the command above, and put anything it must
carry (defaults, constraints) in the Fluent API in `MyFinanceContext`. If the API is running (Visual Studio or
IIS Express locks the Debug DLLs), add `--configuration Release` to the `dotnet ef` commands. Don't create a
migration unless asked.

**Commands that connect to the database need `ASPNETCORE_ENVIRONMENT=Local`** (`database update`,
`migrations list`). The design-time factory (`MyFinanceWebApiCore/Models/MyFinanceContextFactory.cs`) reads only
`appsettings.json` (secrets empty), `appsettings.{ASPNETCORE_ENVIRONMENT}.json` and environment variables — not
`appsettings.local.json` on its own — so without the variable you get "The ConnectionString property has not
been initialized". On Windows `Local` matches `appsettings.local.json`. In PowerShell:
`$env:ASPNETCORE_ENVIRONMENT = "Local"` (clear it afterwards with `Remove-Item Env:ASPNETCORE_ENVIRONMENT`).
`migrations add` and `has-pending-model-changes` don't connect, so they don't need it.

The only test project is `EFDataAccessTest` (NUnit). It covers the EF period helpers, account
hierarchy rules (`AccountHierarchyValidator`, `AccountLinkRules`), GPT classification response/error handling,
classification cache behavior for digital-service IVA, and model-comparison request/usage/cost handling.
GPT tests also cover strict output schemas, the JSON-mode switch, and payload-free, failure-safe usage logging.
It also covers account AI-hint request validation and service updates.
Other services and controllers have no tests — don't assume their behavior is pinned; check call sites when
changing service logic.

## Conventions

- Services in `MyFinanceBackend` use repository interfaces and `IUnitOfWork` only — never EF or Mongo types.
- Register new services/repositories in `MyFinanceWebApiCore/Startup.cs` (`Scoped`).
- Throw typed exceptions (`ServiceException` in `MyFinanceModel/Exceptions.cs`, with an HTTP status) from
  services; controllers don't catch — a filter maps them to HTTP responses.
- The non-EF repositories in `MyFinanceBackend/Data` (`AccountRepository`, `SpendsRepository`, …) are unused
  legacy code. The real implementations are the `EF*Repository` classes in `EFDataAccess`.
- Prefer extending an existing sub-service over duplicating logic.
- Scope every query over user-owned data (accounts, account groups, spends, …) to the current user (`UserId`), and
  never trust an id sent by the client: check it belongs to the user. Loading by id alone lets one user read or
  change another's data.
- Real config values live in `appsettings.local.json` (gitignored) or environment variables; never commit
  secrets.
- Never commit automatically after completing work. An explicit user request to commit authorizes that
  commit without another confirmation. Ask before a push or pull request, stating the scope and branch;
  permission to commit does not authorize either.
- Branching: work on `develop` or a feature branch (for example, `feature/ImproveAIConnectivity`). Continue on
  the current feature branch when it is relevant to the task; do not switch to `develop` just to start a session.
  Base new feature branches and worktrees on `develop` unless the user specifies another base.
  **`master` is production**, and **production is the only environment** (no dev or staging): a push to `master`
  runs the EF migrations on the Azure database and then deploys. See `docs/architecture.md` ("CI / deployment").
