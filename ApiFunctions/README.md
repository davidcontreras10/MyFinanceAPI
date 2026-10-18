# ApiFunctions

Azure Functions v4, .NET 10 isolated worker, hosted on Windows.

This is the independently deployed scheduled-job app for MyFinanceAPI. Its source was imported
from `davidcontreras10/MyFinance` commit `f40a1cc`; runtime behavior is unchanged.
Open `ApiFunctions/ApiFunctions.sln` in Visual Studio. It is intentionally separate from the
API solution, with no API project references.

The legacy timer jobs retain their existing schedules and behavior:

- `DailyRefresh`: `0 1 6 * * *`, runs `SpUpdateCurrentPeriods` for all users.
- `ScheduledTasks`: `0 10 6 * * *`, authenticates with the API and executes today's tasks.

`ScheduledTasksV2` is an additive hourly dispatcher (`0 0 * * * *`). It is a no-op unless
`scheduledTasksV2Enabled=true`. The API must also have `ScheduledTasks:HistoryEnabled`,
`ScheduledTasks:ExecutionV2Enabled`, and `ScheduledTasks:DispatcherEnabled` enabled. Before
activating v2 in production, disable the legacy `ScheduledTasks` timer to avoid overlapping
execution paths. All v2 flags remain off by default; the legacy timer is unchanged.

Schedules default to UTC unless the Azure app has `WEBSITE_TIME_ZONE` configured.
Preserve that setting during migration.

## Build and local development

Install the .NET 10 SDK and Azure Functions Core Tools v4. For Visual Studio development
and publishing, use an updated Visual Studio 2026 with the Azure development workload.
Run these commands from the MyFinanceAPI repository root:

```powershell
dotnet build .\ApiFunctions\ApiFunctions.csproj
dotnet publish .\ApiFunctions\ApiFunctions.csproj -c Release
```

The project uses `Microsoft.NET.Sdk` with `Microsoft.Azure.Functions.Worker.Sdk`.
Keep this setup for the existing Visual Studio Zip Deploy workflow: using
`Azure.Functions.Sdk/1.0.0` with the installed publishing tools caused a ZipDeploy
task parameter mismatch (`UseBlobContainerDeploy`). The worker model remains isolated.

Visual Studio's existing Zip Deploy profile can still target the same Function App.
Reload the project after changing the SDK and build before publishing. Azure runtime
settings must match the isolated worker as described below.

Existing local publish profiles remain gitignored. The code relocation does not deploy anything
or change Azure resources. [Azure Pipelines setup](AZURE-PIPELINES.md) describes the manual-only
build/package pipeline and optional production deployment in `azure-pipelines.yml`.
Keep the old repository's copy until publishing from this location is verified, then retire it.

Use `ApiFunctions/local.settings.example.json` as the schema for the gitignored
`ApiFunctions/local.settings.json`. Set `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`
in any existing local settings. Keep real credentials outside source control.
The example disables both jobs to prevent local startup from running financial operations.
Enable them only with intentional test database/API settings and local Azure Storage (Azurite).
The legacy repository tracks a real `local.settings.json`; that file is deliberately excluded
from this import. Initialize local configuration from the example instead.

From the `ApiFunctions` project directory, `func start --port 7296` starts the host.
Do not start the host with production credentials for migration verification.

## Deploying the isolated-worker migration

The code migration alone does not change the deployed Azure app. Coordinate the
configuration and deployment in a maintenance window; a mixed worker setting and
deployment package can prevent function discovery.

1. Record the existing app settings and runtime stack for rollback.
2. Stop the Function App before changing its runtime and deployment package.
3. Set `FUNCTIONS_EXTENSION_VERSION=~4` and `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`.
4. Set the Windows runtime stack to .NET 10 isolated (ARM `siteConfig.netFrameworkVersion=v10.0`).
5. Remove `FUNCTIONS_INPROC_NET8_ENABLED` if present. Preserve storage, SQL/API credentials,
   timezone, Application Insights and existing function-disabled settings.
6. Deploy the complete Release publish output, including generated metadata and worker files.
7. Start the app and confirm that both function names appear and the worker starts successfully.
8. Observe the next scheduled executions in Application Insights and verify database/API results.
   Do not manually invoke financial jobs solely to test the migration.

To roll back, stop the app, restore the old package and matching runtime settings,
then restart it. The old .NET 6 runtime is already out of support; rollback is only
a temporary recovery measure.

Worker logs use Application Insights directly. `host.json` controls host logging;
worker logging is configured in `Program.cs`.

References:
- https://learn.microsoft.com/azure/azure-functions/migrate-dotnet-to-isolated-model
- https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide
