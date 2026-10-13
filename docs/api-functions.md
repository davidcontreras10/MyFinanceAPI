# ApiFunctions

`ApiFunctions/ApiFunctions.sln` is an independently deployed .NET 10 isolated-worker app.
It is outside `MyFinanceWebApi.sln`, whose projects remain on .NET 8, and has no references
to API assemblies. `ScheduledTasks` calls the configured API over HTTP; `DailyRefresh`
uses the existing SQL stored procedure. Schedules and business behavior are unchanged.

The source was imported from MyFinance commit `f40a1cc`. The production release includes
only this project, its manual Azure Pipeline, and API workflow isolation changes. API
features and EF migrations pending on `develop` are not included.

From the repository root:

```powershell
dotnet publish ApiFunctions/ApiFunctions.csproj -c Release
```

Real Functions local settings and Visual Studio publish profiles are gitignored. The
settings example disables both jobs; leave them disabled during local build verification.
Do not copy the legacy repository's tracked `local.settings.json` into source control.

`ApiFunctions/azure-pipelines.yml` is manually triggered. It builds/packages by default;
production deployment is opt-in and limited to `master`, using the Azure service connection
`myfinance-functions-azure` and environment `myfinance-functions-production`. Hosted image
`windows-2022` has been verified in Azure DevOps. The pipeline installs .NET 10 explicitly.
Environment approval must name a human approver, with self-approval enabled for a solo operator.

Existing API GitHub Actions remain separate, skip Functions-only pushes, and explicitly
build the API solution and publish its host project. A push changing the workflows or other
API-relevant files still triggers the existing production API deployment/migration jobs.

See [Functions setup](../ApiFunctions/AZURE-PIPELINES.md) for deployment instructions.
Keep the old MyFinance source copy until the first deployment from this repository is verified.
