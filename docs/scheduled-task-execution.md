# Scheduled-task execution audit

## Current behavior

- `ScheduledTasksService.GetTodayScheduledTaskAsync` selects weekly/monthly tasks using
  `DateTime.UtcNow`; it does not exclude successful executions for that date.
- `ExecuteScheduledTaskAsync` creates a spend or transfer on every request. Execution
  history is recorded afterward; there is no occurrence key or duplicate guard.
- The task service's transaction calls are commented out. `TransferService` commits
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

## Proposed direction (not implemented)

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
test matrix and staged rollout. Its v2 ledger/execution foundation is implemented
behind a default-off feature gate; this audit still describes the legacy path.
