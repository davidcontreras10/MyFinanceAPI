# Azure Pipelines setup

Azure Pipelines is the build/deployment service inside Azure DevOps. It deploys to the existing
Azure Function App; it does not replace the app or require moving the source from GitHub.

Target:

- Azure DevOps organization: `https://dev.azure.com/dcontre10/`
- GitHub repository: `davidcontreras10/MyFinanceAPI`
- Pipeline file: `ApiFunctions/azure-pipelines.yml`
- Azure subscription: `Default Free` (`ebc21e19-be0b-44f4-87e4-5b4aafa9a724`)
- Resource group: `MyFinance`
- Existing Windows Function App: `MyFinanceFunctionApp`
- App URL: `https://myfinancefunctionapp.azurewebsites.net`

## Behavior

The pipeline is manually triggered: both `trigger` and `pr` are `none`.
Its default run builds only ApiFunctions with .NET 10, checks the worker and timer metadata,
and publishes a ZIP artifact named `apifunctions`. `host.json` is at the ZIP root and
`.azurefunctions` host extensions are included. Local settings and publish profiles are excluded.
It does not start the functions or run API/database migrations.

The `agentImage` parameter chooses a Microsoft-hosted Windows image for both jobs and defaults
to `windows-2022`. The pipeline installs .NET 10 explicitly, independently of the image's bundled
SDKs. Other supported images can be selected in **Run pipeline** to diagnose image-specific
agent assignment failures without editing YAML. These images use the normal Microsoft-hosted
allocation, not the separately billed GitHub-hosted agent option.

The production deployment stage is included only when `deployProduction` is selected.
It runs only from `master`, downloads the same run's artifact, checks the existing Windows
.NET 10 isolated configuration, then deploys with `AzureFunctionApp@2`.
The pipeline does not replace existing SQL/API credentials, schedules, timezone or disabled settings.
GitHub Actions continue to deploy the API independently.

## One-time Azure DevOps setup

1. Open the organization above. If it has no project, create a private project named `MyFinance`.
   An organization (`dev.azure.com/dcontre10`) contains projects; a project contains pipelines.
   Use an existing suitable project instead if one is already available.
2. Under **Project settings > Service connections**, create an **Azure Resource Manager** connection.
   Prefer **App registration (automatic)** with **workload identity federation**. Select the
   subscription above and scope it to resource group `MyFinance`. Name it
   `myfinance-functions-azure`. Do not grant access to all pipelines; authorize this pipeline
   when requested. This connection avoids storing a client secret or publish profile in YAML.
3. Under **Pipelines > Environments**, create `myfinance-functions-production` with resource type
   **None**. Add an approval under **Approvals and checks** if you want a confirmation before
   deployment. Keep self-approval allowed if you are the sole operator and require an approval.
4. The migration and pipeline files must be committed and pushed to GitHub before DevOps can read
   them. Start with the `codex/move-apifunctions` feature branch; the repository's commit/push
   approval policy still applies. A production deployment requires the changes on `master`.
5. Under **Pipelines > New pipeline**, choose **GitHub**, authorize access to this repository,
   and select `davidcontreras10/MyFinanceAPI`. Choose **Existing Azure Pipelines YAML file**,
   select the published branch and `/ApiFunctions/azure-pipelines.yml`, then save the pipeline.
   Do not choose a template that creates another GitHub Actions workflow or deploys the API.
6. Enable Microsoft-hosted capacity at the organization level. If the run reports no hosted
   parallelism, go to **Organization settings > Billing** and link a valid Azure subscription.
   Then check **Organization settings > Pipelines > Parallel jobs** for the free Microsoft-hosted
   job before retrying. Current Microsoft guidance enables the private-project free tier through
   subscription linking; a paid parallel-job purchase is not required for the free allocation.
   A self-hosted agent is another option if hosted capacity cannot be enabled.

## First run: build only

Select **Run pipeline**, choose the published migration branch, and leave
**Deploy to the existing production Function App** unchecked. The default service-connection
name does not need to resolve for this run because the deployment stage is omitted.

Verify the Build stage succeeds and download the `apifunctions` artifact if desired.
There should be no Deploy stage and no changes to the running Azure app.

If assignment fails with `No agent found` before checkout, verify hosted parallelism and the
pool's `isHosted` property first. A hosted pool can have an empty Agents page; do not assume
that a self-hosted agent must be installed. Start a new run with another `agentImage` value.
Changing images is a diagnostic step, not proof of the cause; if all supported images fail,
investigate organization/pool provisioning rather than buying additional capacity.

## First production deployment

After the migration is reviewed and merged to `master`, run the pipeline on `master`, select
the deployment checkbox, and use `myfinance-functions-azure` (or enter your actual connection name).
Authorize the connection/environment if requested and approve the deployment if an approval is set.

Before deployment the pipeline checks the existing app is Windows, its worker runtime is
`dotnet-isolated`, and its Windows framework setting is `v10.0`. Fix any mismatch in the Azure
portal before retrying; the pipeline deliberately does not perform another runtime migration.

After deployment confirm both functions are listed, the worker starts without a new runtime
mismatch warning, and the next scheduled executions produce the expected database/API results.
Avoid manually executing financial jobs merely to check deployment. Retire the old MyFinance
source copy only after this deployment is verified. Keep the previous known-good deployment ZIP
for recovery; do not assume changing a repository branch rolls Azure back.

## Local package verification

From the MyFinanceAPI repository root:

```powershell
dotnet publish ApiFunctions/ApiFunctions.csproj -c Release -o ApiFunctions/artifacts/publish
./ApiFunctions/scripts/Test-PublishPackage.ps1 -PublishDirectory ApiFunctions/artifacts/publish
```

These commands build and inspect output without executing either timer job.

## CLI access

The Azure CLI and its `azure-devops` extension are installed on the current workstation,
but setup checks encountered expired/missing authentication. Sign in with `az login` before
using CLI commands. Do not paste access tokens, publish profiles or local settings into chat.

```powershell
az devops project list --organization https://dev.azure.com/dcontre10/ --output table
```

## References

- https://learn.microsoft.com/azure/azure-functions/functions-how-to-azure-devops
- https://learn.microsoft.com/azure/devops/pipelines/library/connect-to-azure
- https://learn.microsoft.com/azure/devops/pipelines/tasks/reference/azure-function-app-v2
- https://learn.microsoft.com/azure/devops/pipelines/licensing/concurrent-jobs
