# Scheduled-task activation runbook

Status: operator tooling, guarded local-copy tests, the basic manual UI flow,
hosted Function/API basic-spend dispatch and the 59-task local activation rehearsal
are verified. Remaining acceptance scenarios and production cutover approval are
still required. This
document does not authorize a deployment, flag change, migration or activation.

Use the [production cutover checklist](scheduled-task-production-checklist.md) for
the setting matrix, approval gates, ordered release steps and monitoring/stop checks.

## Safety boundaries

- Work in the official API checkout. Rehearse against the approved restorable local
  database copy first. Never point a local Function host at production SQL or API.
- A push to `master` runs production migrations and deploys. Treat it as a separate,
  explicitly approved release, not a harmless way to run a build.
- Migrations stay EF-generated from Fluent API. The activation tool does not apply
  migrations, change app settings, run discovery, execute tasks or call external APIs.
- Deploying the schema or enabling flags does not activate existing tasks. New tasks
  created with history enabled are registered automatically at their creation instant.
- Freeze task edits, ownership changes and timezone preferences throughout preview,
  approval and apply. Stop/drain the legacy dispatcher before reconciling its final
  work. Snapshot preflight is not a replacement for this maintenance window.

## Package verification

The Function pipeline runs `ApiFunctions/scripts/Test-PublishPackage.Tests.ps1`, then
validates the actual publish output and ZIP. All three functions must be present with
their exact schedules: DailyRefresh (`0 1 6 * * *`), ScheduledTasks (`0 10 6 * * *`),
and ScheduledTasksV2 (`0 0 * * * *`). The new timer remains opt-in. A package passing
verification is not proof that its host, credentials or target API are correct.

## Review the cutoff

1. Capture a database backup and current API/Function settings. Confirm all generated
   migrations are present before running this API/tool build. Do not migrate here.
2. Stop both dispatch paths and drain in-flight requests. In production, disable the
   legacy timer with `AzureWebJobs.ScheduledTasks.Disabled=true` and keep
   `scheduledTasksV2Enabled=false`. Freeze API writes during activation. For local
   rehearsal also disable DailyRefresh so startup cannot run its stored procedure.
3. Review each selected task's last legacy results against its actual financial rows.
   Do not infer reliable occurrence completion from an old success label, description
   or incomplete history. Resolve ambiguous final legacy work before proceeding.
4. Choose an explicit UTC boundary after the work already handled by the old path.
   A due instant equal to the boundary is included; earlier due instants are excluded.
   A midday boundary usually excludes that local day's midnight, but different user
   timezones can produce different first dates. Review every previewed first date.
   Use separate plans when users/tasks require different boundaries. Do not backdate
   years to task creation or assume the tool reconciles old transactions.
5. Select explicit task IDs and expected owner IDs. Retired tasks, invalid schedules
   and owners without valid saved timezones require review; do not silently repair
   or clamp them. Existing activation boundaries cannot be reset.

## Preview, approve and apply

`tools/ScheduledTaskActivation` is an offline .NET 8 operator command. It reads only
`MYFINANCE_SCHEDULE_ACTIVATION_CONNECTION`, never appsettings or Function credentials.
The connection's server and database must match the manifest. The administrator ID
must have a persisted Admin role in that database. Keep plans/output outside source
control, for example under the ignored repository `artifacts/` directory.

The current login grants the Admin JWT claim to the `AzureAdmin` username without
requiring a stored role membership. That claim alone does not satisfy v2 activation
or dispatch authorization. Verify the chosen actor's persisted Admin membership
before cutover; provisioning a missing membership requires explicit approval for
that database. It is an operator data step, not an EF schema migration. Do not
grant administrator privileges to ordinary task owners just to enable manual runs.

Create a selection JSON, replacing every placeholder and choosing the cutoff yourself:

```json
{
  "expectedServer": "(localdb)\\MSSQLLocalDB",
  "expectedDatabase": "MYFNDB-2026-10-18-00-21",
  "administratorId": "PERSISTED-ADMINISTRATOR-GUID",
  "activationUtc": "REVIEWED-UTC-INSTANT-WITH-Z",
  "reviewed": false,
  "approvalNote": "",
  "tasks": [
    { "taskId": "SELECTED-TASK-GUID", "ownerId": "EXPECTED-OWNER-GUID" }
  ]
}
```

Supply the environment variable through your local secret-management mechanism.
Do not put its value in a plan, committed file, command transcript or screenshot.
From the API repository root:

```powershell
dotnet build tools/ScheduledTaskActivation/ScheduledTaskActivation.csproj -c Release
dotnet run --no-build -c Release --project tools/ScheduledTaskActivation -- --plan artifacts/selection.json |
    Out-File -Encoding utf8 artifacts/reviewed-plan.json
if ($LASTEXITCODE -ne 0) { throw 'Preview failed; do not approve its output.' }
```

Preview is the default and writes no database rows. It returns saved timezones,
definition fingerprints bound to the target, administrator and cutoff, descriptions
and the first eligible local date within the
calendar scan window (null for a manual-only task). It always returns `reviewed=false`.
Review all output. In that output file, set `reviewed=true` and provide an approval
note confirming reconciliation, the chosen boundary, stopped/drained dispatchers and
the frozen edits/preferences window. Preserve the generated fingerprints/timezones.
Neither a fingerprint nor this attestation automatically stops a running host.

Only after approval, while the maintenance window remains in effect:

```powershell
dotnet run --no-build -c Release --project tools/ScheduledTaskActivation -- --plan artifacts/reviewed-plan.json --apply
if ($LASTEXITCODE -ne 0) { throw 'Activation stopped; inspect partial progress before resuming.' }
```

Apply preflights the complete allowlist and then rechecks each task before calling
the existing repository activation method. Changed owners, definitions, saved
timezones and conflicting existing boundaries are rejected. The repository retains
its own authorization, reference checks and owner/task locks. The tool's internal
history option permits only these explicit history writes; it changes no host flag.

Each task commits in its own transaction, not one batch transaction. A failure can
leave earlier tasks activated. Keep the reviewed plan and per-task confirmation
output; inspect state and rerun the same plan to resume. An identical activation is
idempotent and does not reset discovery progress. Do not delete schedule/revision rows
or move the cutoff to work around an error. If definitions change, stop and review
again before continuing. The tool never expands the allowlist on its own.

## Validation and production handoff

- Before production activation, complete local UI/API and Function/API end-to-end
  validation using only the database copy and temporary test tasks. Confirm the
  pending-today default, independent extra runs, same-ID retry, history, failed-task
  reporting, and no duplicate work after a repeated dispatch.
- Enable flags only as part of that reviewed local validation, not to finish missing
  implementation. Local API history, execution and dispatch settings must agree with
  the local Function target and `scheduledTasksV2Enabled`; the legacy timer stays off.
- During approved production cutover, enable `ScheduledTasks:HistoryEnabled` before
  reopening any task/profile writes after activation. Then coordinate
  `ScheduledTasks:ExecutionV2Enabled`, `ScheduledTasks:DispatcherEnabled` and the
  Function's `scheduledTasksV2Enabled`. Keep the old ScheduledTasks timer disabled.
  Do not turn off DailyRefresh in production as part of this feature by default.
- Observe actual posting dates and completion/attempt receipts, not only host success.
  Missing periods must retain failed pending work without financial effects. Batches
  process only the oldest occurrence per task, so backlog can take several hourly runs.
- To pause, disable the v2 Function and dispatch/execution as needed, then drain
  in-flight calls. Keep history enabled so edits preserve the scheduling timeline.
  Disabling history or reverting to an old API/legacy dispatcher is not a safe
  automatic rollback: freeze writes and reconcile existing v2 effects first. Never
  restore an old database/package over newly committed financial work casually.

## Verification recorded

Guarded local-copy tests cover command-line JSON/read-only preview, explicit allowlist activation,
persisted administrator/owner checks, reviewed snapshot requirements, stale-plan
rejection before batch writes, invalid legacy monthly day 29, cutoff preservation and idempotent resume after
discovery. They activate only temporary test tasks, invoke no financial execution
for activation cases, and remove/verify removal of their temporary rows.

### Hosted local validation on 2026-10-19

The user ran the real hourly Function host against the local API and restored
database copy, with v2 flags enabled and both legacy timers disabled locally.
Authentication and dispatch returned HTTP 200. An 11:00 UTC-6 invocation found no
due work. After preparing a labeled pending $1 basic expense for `dcontre10` in
`CUENTA BAC`, the 12:00 invocation scanned three schedules, completed one execution
and reported zero failures. The user confirmed the successful attempt and matching
October 19 expense in the UI. The 13:00 and 14:00 invocations each completed zero
executions with zero failures, verifying that later hourly checks did not rerun it.

These results are based on user-supplied console logs and UI screenshots. They
validate the hosted basic-spend path, not every UI/transfer/failure scenario. See
[the implementation plan](scheduled-task-plan.md) for separate automated coverage.
The recurring Monday fixture remains in the local copy; stop local dispatch or
retire it before unwanted future test postings. No production activation is implied.
Production still requires explicit task selection, legacy-work reconciliation,
reviewed cutoffs and approval of migration/deployment order before any master push.

### Restored-copy activation rehearsal on 2026-11-02

A fresh production backup was reviewed using SELECT-only queries before local
schema changes. It contained 60 monthly task definitions across two owners. For
59 supported definitions, the latest successful legacy history matched one
automatic financial candidate by timestamp, description, source account/owner,
current amount, currency and category. Transaction direction and the configured
destination linkage for all 14 transfers matched. Their latest execution dates
also matched their most recent scheduled monthly dates as of November 2. One
definition lacking both a basic and transfer subtype was excluded, not repaired.
These are latest-occurrence checks, not an exhaustive historical reconciliation;
legacy history has no immutable link to financial transaction IDs.

The user selected all 59 supported tasks (25 for one owner, 34 for the other) and
approved `America/Costa_Rica` for both owners. With explicit local-only approval,
all five existing EF-generated scheduler migrations were applied to the copy;
no migrations were edited and none remained pending. The existing Admin role was
granted to `AzureAdmin` in that copy, with both task owners' memberships verified
unchanged. The production role remains a separate approval/provisioning step.

The read-only preview used the rehearsal boundary `2026-11-02T12:00:00Z` (06:00
Costa Rica time), after the already-handled November 2 occurrences. First eligible
local dates were November 3 (5 tasks), November 5 (1), November 6 (14), November 20
(34), November 25 (1), and December 2 (4). The four November 2 occurrences were not
selected for replay. This boundary is not an approved production cutoff.

After the user confirmed that no local projects were running, the reviewed plan
was applied through the activation tool. Verification found exactly 59 schedules
and 59 initial revisions with the approved owners, boundary, scan cursors and
Costa Rica timezone; the excluded task remained unactivated. Occurrences,
completions and attempts stayed at zero, and the financial transaction count was
unchanged. No dispatch or financial work was invoked. Starting a local host with
dispatch enabled can subsequently execute due work; the rehearsal does not imply
that those local flags were disabled.

Detailed evidence, manifests and the local helper remain in ignored `artifacts/`
files because they contain private financial metadata. They must not be copied
to production without a new target-specific preview and review. Production
continues changing while legacy scheduling runs: stop/drain it, reconcile final
work and approve the final cutoff at cutover. No production migrations, roles,
activation, flags or deployment were changed by this rehearsal.

### Deployment order review

The live GitHub Actions check found the non-migrating API deployment workflow
manually disabled. The active `master_myfinancewebapicore_ef.yml` workflow runs
migrations before deployment; recent production runs confirmed that order.
Keep the old workflow disabled rather than assuming both YAML files execute.
Reconfirm live workflow state before production release.

Release the API schema/code first with v2 activation flags off, verify API health,
then release the UI. Functions require a separate manual pipeline run from
`master`; deploy with v2 dispatch disabled. Activation and coordinated flag
enablement follow their own reviewed maintenance window. A push to `develop`
builds but does not perform this production cutover.
