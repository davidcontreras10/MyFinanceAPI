# Scheduled-task production cutover checklist

Status: preparation only. No production deployment, role change, activation, cutoff
or flag change is authorized by this checklist. Use the detailed
[activation runbook](scheduled-task-rollout.md) for preview/apply commands.

## Approved scope and remaining gates

- The selected scope is the 59 supported monthly tasks across both owners, using
  `America/Costa_Rica`. The incomplete legacy definition remains excluded.
- The local restored-copy rehearsal passed without financial execution. Its cutoff,
  credentials, LocalDB pipe, fingerprints and reviewed manifest are not valid for
  production. Create a fresh production-targeted preview during maintenance.
- Confirm remaining acceptance gaps recorded in the runbook before release approval.
- Obtain explicit approval of the maintenance window, API/UI master promotions,
  manual Function deployment, any missing production Admin membership, final task
  allowlist/cutoff, and enabling financial execution. A local approval is not a
  production approval. Do not publish connection strings, settings exports or plans.

## Settings matrix

Use these names in Azure App Settings; double underscores map to API configuration
sections. Local JSON files are ignored and are not deployed as production settings.

| Application setting | Maintenance / deployment | After activation, before reopening writes | Running |
| --- | --- | --- | --- |
| API: `ScheduledTasks__HistoryEnabled` | `false` for initial deployment | `true` | `true` |
| API: `ScheduledTasks__ExecutionV2Enabled` | `false` | `false` | `true` |
| API: `ScheduledTasks__DispatcherEnabled` | `false` | `false` | `true` |
| Function: `scheduledTasksV2Enabled` | `false` | `false` | `true` |
| Function: `AzureWebJobs.ScheduledTasksV2.Disabled` | `true` | `true` | `false` |
| Function: `AzureWebJobs.ScheduledTasks.Disabled` | `true` | `true` | `true` |

The initial history-off state assumes production has never activated v2. If it
already maintains schedule history, stop and review; do not disable that history.
Flags do not enforce the required user-write freeze. Freeze scheduled-task writes,
timezone changes and manual task executions separately and drain in-flight work.
API options are captured at startup: restart/recycle and verify effective settings
after each approved transition. Function settings also require host reload.

Preserve production DailyRefresh behavior; do not copy its local test disablement.
Verify `useCoreVersion=true`, `webApiCoreBaseApi=<production-core-api-url>/`, and
`azureAppUser=AzureAdmin`, with credentials supplied through existing secret settings.
Confirm the API database and DailyRefresh database point to the intended production
target. Never point the local Function host at production SQL or API.

## Execute only after production approval

1. Record release commit IDs, take/verify a recoverable database backup, and securely
   capture current API/Function settings. Verify the old non-migrating API workflow
   remains disabled and the migration-enabled workflow remains active. Check its EF
   tooling/runtime prerequisites; the current dotnet-ef install is not version-pinned.
2. Establish the write freeze. Disable both dispatch paths using the maintenance
   settings above and wait for in-flight calls to finish. Reconcile final legacy
   history against actual financial rows, including work since the restored backup.
3. After explicit release approval, promote API develop to master. Observe successful
   application of all five generated scheduler migrations before the new API deploys.
   Verify migration history and basic API/authentication health. Do not edit migrations.
4. Promote UI develop to master after the API is healthy. Verify the deployed UI
   loads against that API while v2 execution remains unavailable.
5. Run the ApiFunctions production pipeline manually from the approved master commit
   with production deployment selected. Verify package guards, dotnet-isolated/.NET 10
   runtime checks, correct production target and both scheduled dispatch timers disabled.
6. Verify saved Costa Rica timezones and AzureAdmin's persisted Admin membership.
   If missing, obtain separate production permission before provisioning only that
   existing role. Do not grant Admin to task owners or rely solely on the JWT claim.
7. Build an explicit 59-task/owner selection against the production target. Recheck
   definitions, exclusions and final legacy work. Choose a nonfuture UTC boundary
   that includes genuinely pending work but excludes work already handled. Review
   every first eligible local date. Do not reuse the November 2 rehearsal cutoff.
8. Generate and approve the target-specific preview. Apply while the freeze holds,
   preserving per-task confirmations. Verify exactly the reviewed schedules/revisions,
   boundaries and timezones, with no unexpected financial/attempt/completion changes.
   On partial failure, keep dispatch off; inspect and resume the same reviewed plan.
9. Enable history and restart/verify the API before reopening any task/profile writes.
   Coordinate execution/dispatch enablement only after its separate approval. Keep
   the Function disabled until the API is ready, then enable the v2 Function last.
   Leave legacy ScheduledTasks disabled. Reopen writes only after checks pass.

## Observe and pause

- Verify the first hourly invocation authenticates, dispatches and logs failures
  accurately. Inspect occurrence posting dates, completion receipts, attempts and
  actual source/destination financial rows, not just Function success status.
- Scan and execution batches are capped at 50; discovery uses bounded windows and
  execution takes only the oldest pending occurrence per task per dispatch. With
  59 schedules, one hourly scan is not evidence that every schedule was examined.
  Check remaining schedules/backlog over subsequent runs. Do not assume completing
  zero executions proves every task has no outstanding work.
- Repeated checks of a completed occurrence must not add another receipt, attempt
  or transaction. Independent user-requested extra runs remain separate intents.
- Missing periods or other financial failures must retain pending work and failed
  attempts without partial financial effects. Pause and investigate unexpected dates,
  duplicate effects, unresolved failures, wrong targets or authorization errors.
- To pause, disable v2 Function dispatch and API dispatcher/execution, drain calls
  and preserve history. Keep the old scheduler disabled; do not automatically switch
  back to legacy, reset cutoffs, remove ledger rows, downgrade schema or restore a
  backup over newly committed transactions. Reconcile before deciding recovery.
