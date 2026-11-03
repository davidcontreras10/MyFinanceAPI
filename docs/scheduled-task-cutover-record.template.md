# Scheduled-task cutover record template

Template only, not an approval or an executable activation plan. Copy into a
private ignored artifacts folder for the actual cutover. Do not commit the filled
record, financial evidence, settings exports or manifests. Never record credentials.
Follow the [production checklist](scheduled-task-production-checklist.md).

## Release and target

- Operator and approver: NOT RECORDED
- Maintenance start/end in Costa Rica time and UTC: NOT APPROVED
- API, UI and Function release commit IDs: NOT SELECTED
- Production API URL and SQL server/database identifiers (no connection string): NOT VERIFIED
- Backup identifier, completion time and recovery verification: NOT RECORDED
- Current workflow states and last checks: NOT VERIFIED
- API/Function settings captured securely; evidence reference: NOT RECORDED
- Remaining acceptance gaps reviewed and accepted: NOT APPROVED
- Release/deployment permission and evidence reference: NOT APPROVED

## Final legacy reconciliation

- Legacy and v2 dispatch disabled; effective time: NOT VERIFIED
- In-flight calls drained and task/manual/timezone writes frozen; time: NOT VERIFIED
- Final evidence source captured after drain: NOT RECORDED
- Previously reviewed task baseline: 59 supported monthly tasks, two owners
- Explicit task/owner allowlist artifact and checksum: NOT REVIEWED
- Added/removed/edited tasks since baseline and decisions: NOT REVIEWED
- Excluded incomplete definition verified and decision: NOT REVIEWED
- Latest successful legacy work matched to financial effects: NOT REVIEWED
- Failures, missing occurrences, manual runs and ambiguous effects resolved: NOT REVIEWED
- Unhandled work before each proposed cutoff and catch-up decision: NOT REVIEWED
- Both saved timezones verified as America/Costa_Rica: NOT VERIFIED

## Migration and deployment gates

- API schema migration result and migration-history evidence: NOT VERIFIED
- API health/authentication result with execution/dispatch off: NOT VERIFIED
- UI deployment/health result: NOT VERIFIED
- Function package/runtime/target verification with dispatch off: NOT VERIFIED
- AzureAdmin persisted Admin membership verified: NOT VERIFIED
- If provisioning is required, separate production role approval/result: NOT APPROVED

## Activation approval

- Target-specific selection and preview artifacts/checksums: NOT GENERATED
- Explicit nonfuture activation UTC boundary per plan: NOT APPROVED
- First eligible Costa Rica dates and task counts reviewed: NOT REVIEWED
- Completed legacy occurrences excluded from replay: NOT VERIFIED
- Target, administrator, allowlist, snapshots and timezones match: NOT VERIFIED
- Maintenance freeze still in effect at approval/apply: NOT VERIFIED
- Explicit activation approval, approver and time: NOT APPROVED
- Apply output/per-task confirmations and partial-progress record: NOT APPLIED
- Schedules/revisions/owners/cursors/timezones/exclusions verified: NOT VERIFIED
- Financial rows, attempts and completions unchanged by activation: NOT VERIFIED

## Enablement and monitoring

- History enabled/reloaded before writes resume: NOT VERIFIED
- Explicit financial execution/dispatcher enablement approval: NOT APPROVED
- API ready before v2 Function enabled; effective times: NOT ENABLED
- Legacy timer remains disabled; DailyRefresh behavior preserved: NOT VERIFIED
- First invocation authentication, dispatch and failure counts: NOT OBSERVED
- Actual posting dates, receipts, attempts and transfer legs verified: NOT OBSERVED
- Remaining scan batches/backlog checked beyond first 50 schedules: NOT OBSERVED
- Later invocation does not repeat completed occurrences: NOT OBSERVED
- Write freeze lifted, approver and time: NOT APPROVED
- Pause needed/result and reconciliation reference: NOT RECORDED

## Postponement and partial progress

If cutover is postponed before any activation commits, discard approval of the old
preview, refresh final evidence and approve a new cutoff/preview. If legacy scheduling
is intentionally resumed, first confirm no v2 effects committed and obtain approval;
the final reconciliation must include its additional work when cutover resumes.

If activation partially committed, keep dispatch off, preserve the reviewed plan and
per-task results, and inspect/resume that plan. Do not reset existing boundaries or
delete ledger rows. Never restore an older backup over new financial work as an
automatic rollback. History must remain enabled once history maintenance is active.
