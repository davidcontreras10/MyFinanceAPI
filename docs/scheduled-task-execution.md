# Scheduled-task execution audit

## Implemented protections and remaining rollout

Both execution routes, history and retirement now use dedicated domain authorization and approved
scopes. Manual extras are owner-only; cross-user occurrence dispatch requires a persisted administrator.
Legacy financial/history writes now share a transaction, returned failures roll back, and the legacy
execution route is rejected when history maintenance is enabled because it lacks retry identities.
Weekly Sunday parsing is fixed. V2 identity/transaction guards, execution context, bounded history,
manual execution-choice UI and hourly Function dispatch are implemented; deletion retires tasks and
cancels incomplete work without deleting history. Execution and retirement share owner-first locks,
preserving committed receipts and rejecting new work after retirement. Failed/interrupted attempts are
now persisted separately from financial transactions; the owner-facing attempt-history API is
implemented together with its UI. Local-copy financial tests cover manual and
automatic spend/transfer posting, rollback, catch-up and retry recovery. Packaging
guards and a preview-first activation command are implemented; local UI/Function
end-to-end validation and the reviewed cutover remain pending. See the
[activation runbook](scheduled-task-rollout.md).

## Durable attempt records

`ScheduledTaskExecutionAttempt` persists a numbered start before financial work. The success status is
committed atomically with the completion receipt and financial effects. On rollback, the attempt is
marked failed in a separate context; if a later retry finds an unfinished attempt, it is marked
interrupted. Stored failure details are stage/type plus a bounded summary that avoids raw database
exception messages. The generated migration was applied to the local database on 2026-10-17;
production migration application remains pending.

## Original behavior (historical)

- `ScheduledTasksService.GetTodayScheduledTaskAsync` selects weekly/monthly tasks using
  `DateTime.UtcNow`; it does not exclude successful executions for that date.
- `ExecuteScheduledTaskAsync` creates a spend or transfer on every request. Execution
  history is recorded afterward; there is no occurrence key or duplicate guard.
- The legacy task service has no encompassing transaction. `TransferService` commits
  its financial transaction before the task service writes execution history.
  An execution-history failure can therefore leave committed financial effects.
- Returned validation failures reach execution history, but thrown exceptions bypass
  that recording step. History is not a complete attempt log.
- `ExecutedTask` has no modeled unique constraint on task plus scheduled occurrence.
- The execution endpoint passes the caller ID for history, but does not check task
  ownership or require administrator access for cross-user execution. The `today`
  listing is administrator-only; execution must enforce its own authorization.
- Weekly selection uses Sunday = 0, but repository day parsing drops zero. Sunday
  tasks need coverage and a consistent day encoding.

## Original design direction

Automatic execution should identify an occurrence by task ID and scheduled business
date. Enforce uniqueness in SQL and persist the occurrence completion and all
financial effects in the same transaction. Duplicate or concurrent requests must
not create additional transactions; a retry after a lost response must return the
existing completion. Rollback must permit a safe retry without partial effects.

Coordinate transaction ownership with the existing transfer service rather than
nesting its current transaction inside a new one. Retain failure diagnostics
separately from the rolled-back financial work. Filtering completed tasks from the
daily list is an optimization, not the duplicate-execution guarantee.

The agreed scheduling, timezone, catch-up and manual-execution rules are recorded
in [the implementation plan](scheduled-task-plan.md), together with the acceptance
test matrix and staged rollout. The implementation is behind default-off feature gates;
the original gap list above is historical, not the current endpoint authorization/transaction policy.
