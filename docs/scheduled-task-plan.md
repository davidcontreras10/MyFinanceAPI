# Reliable scheduled-task execution

Status: specification complete; execution-ledger foundation and a disabled v2 API
path implemented. The migration is generated but not applied to existing databases.
The current frontend and Function still use the old execution path/schedules.
See [the source audit](scheduled-task-execution.md) for current gaps.

## Agreed product rules

### Schedules and timezones

- Monthly schedules accept only day numbers 1 through 28. Validate both creation
  and editing server-side, including changes of frequency. No month-end fallback.
- Weekly schedules are independent of the day of the month. Preserve Sunday (0)
  through Saturday (6), matching the current .NET day-of-week convention.
- Manual-only tasks have no automatic occurrences.
- Users select a timezone saved on their profile. The client may suggest its
  device timezone, but the API validates the supplied identifier before saving.
- Existing users default to `America/Costa_Rica`. Do not infer timezone from IP,
  UTC offset, or every request, and do not silently change it when users travel.
- A user's local calendar date defines "today". Store execution instants in UTC
  separately from the scheduled local date and its timezone context.
- One hourly UTC dispatcher checks all users. A task becomes due at local midnight
  and is picked up on the first subsequent check, not necessarily at midnight
  exactly. Account for non-whole-hour offsets and daylight-saving transitions.

### Occurrences, retries, and manual execution

- An automatic occurrence is identified by `(TaskId, ScheduledLocalDate)`.
  It may have several attempts but only one committed set of financial effects.
- Failed occurrences remain eligible for retry; completed occurrences never
  create more transactions when called again, even after a lost HTTP response.
- A deliberate extra manual execution has its own client-generated request ID.
  Each new intent gets a new ID; retries of the same intent reuse its ID.
- Extra manual executions neither satisfy nor block automatic occurrences.
- When manually running a task due today and not completed, offer:
  1. **Run Today's Occurrence** (default): execute the same occurrence the automatic
     dispatcher would execute. Once completed, automatic calls return its result.
  2. **Run an Extra Execution**: execute independently, leaving the occurrence due.
- If today's occurrence already completed, warn that another execution creates
  an additional transaction. Manual-only/not-due tasks offer independent execution.
- Confirmation data is advisory. Execution rechecks authorization and state. If
  another caller completes today's occurrence while the dialog is open, return
  the existing completion; never silently convert that choice into an extra run.
- Expose an explicit in-progress or retryable outcome when an execution is busy;
  it must not be reported as newly completed before its transaction commits.

### Catch-up, rollout, and changes

- Catch up missed occurrences, oldest first per task, using their original local
  scheduled dates for financial posting/period resolution, not the retry date.
  A task due on the 28th may be recovered on the 29th or later.
- Use a persisted rollout boundary. Never generate years of occurrences from
  historical task definitions or guess completion from descriptions/history.
- Preserve already-due occurrences when a schedule is edited. Changes affect
  future scheduling only; they must not rewrite outstanding or completed dates.
- Preserve existing occurrences when a timezone changes; use the new timezone
  for future scheduling. Retain old timezone context on existing occurrences.
- Materialize due work before advancing scheduling progress. An interrupted
  discovery pass must neither skip occurrences nor generate duplicates.

## Technical design for step 2

These are design constraints, not a finalized SQL schema or wire contract.

### Persistence and transaction ownership

- Separate scheduled occurrences, execution attempts, and independent manual
  request identities. Existing `ExecutedTask` history has no reliable occurrence
  key; retain it without pretending it provides deduplication for old executions.
- Enforce occurrence and manual-request identities with SQL unique constraints,
  not only a read-before-write check or an in-memory lock. Scope manual identity
  to an authorized caller; reject reuse for a different task/action/payload.
- The financial writes (including transfer legs, included-account entries and
  transfer linkage) and completion/result record must commit in one SQL transaction.
  Reserve the identity before financial effects; serialize competing attempts.
- Retrying an ambiguous commit queries the persisted identity/result. Failure
  diagnostics must not change a committed success into a failure.
- Retain attempt diagnostics separately from rolled-back financial work. A process
  crash may leave an interrupted attempt, but must not permanently strand work.
- Refactor transfer transaction ownership to participate in a caller-owned
  transaction while preserving atomic behavior for ordinary transfer callers.
  Do not simply nest the existing `BeginTransaction` calls.
- Keep SQL/EF operations in repositories and transaction coordination behind the
  existing unit-of-work abstractions. Avoid new queues or external paid services.
- Use a controllable clock for deterministic scheduling tests. Resolve stored IANA
  timezone IDs consistently on Windows and other supported runtimes.
- Bound each catch-up batch and persist progress. One failed user's task must not
  block other users. Retry behavior needs visible diagnostics, not endless hidden
  "Success" logs. Preserve per-task ordering when older occurrences fail.

### Authorization, API, and UI

- Owners may inspect and execute their tasks and choose either manual action.
  Automated cross-user work requires a server-verified administrator permission.
  Do not trust a client `RequestType=Automatic` as authorization.
- Verify task/account/reference ownership at the service/repository boundaries.
  Apply the same policy to context, history, edits and deletion, not just execution.
- Define explicit request/result DTOs before updating the Angular client. Include
  occurrence date or manual request identity and distinguish completed, already
  completed, in progress, and failed outcomes.
- Validate occurrence dates against persisted due work; a client cannot satisfy a
  future or arbitrary occurrence by supplying an unchecked date.
- Preserve the current manual posting-date capability unless a reviewed contract
  change is needed. Keep posting date separate from request identity.
- Preserve a request ID across UI retries and double clicks; disable duplicate
  submission while a request is pending. Do not generate a new ID merely because
  a response timed out. Define safe behavior for old clients during rollout.

### Dispatcher and period readiness

- The API discovers due and missed occurrences; the Function triggers dispatch
  rather than calculating "today" once in UTC for all users.
- The Function reports failed work accurately, including API-returned task
  failures. Host-level success alone is not evidence of successful financial work.
- Scheduled execution must resolve the correct period for the occurrence date.
  If prerequisite period data is absent, return a retryable failure without
  financial effects. Do not depend on the existing global DailyRefresh timer
  running first in every user's timezone; do not change that timer in step 1.

## Acceptance test matrix

These are acceptance targets; the implemented subset is described below. Use NUnit in the current
test project for pure rules/service/controller checks. Use isolated local SQL Server
integration tests with separate connections/contexts for uniqueness, concurrency,
commit and rollback: mocks or EF's in-memory provider cannot prove these guarantees.
No test may invoke production financial jobs or require paid external calls.

The rule, execution-service/controller and LocalDB ledger tests now implement a
subset of this matrix. LocalDB financial writes are transactional probe rows, not
full spend/transfer workflows; complete financial and UI end-to-end coverage remains
a later requirement. See the implementation checkpoint below.

| ID | Scenario | Required outcome | Layer |
| --- | --- | --- | --- |
| S01 | Monthly days 1 and 28 | Accepted on create and edit | Rules/API |
| S02 | Monthly 0, 29, 30, 31, negatives | Rejected, including a weekly-to-monthly edit retaining invalid days | Rules/API |
| S03 | Weekly Sunday and days in the 29th-31st | Selected correctly; Sunday survives storage round-trip | Rules/SQL |
| S04 | Manual-only frequency | Never automatically discovered | Rules |
| T01 | Missing profile timezone; valid/invalid update | Costa Rica fallback; invalid IDs rejected | API/service |
| T02 | Device timezone changes after profile save | No silent profile/schedule change | UI/service |
| T03 | Same UTC instant, different user local dates | Only locally due work selected | Rules |
| T04 | Half-hour/quarter-hour zones; DST forward/back | First hourly check after local midnight; no duplicate local date | Rules |
| I01 | Sequential repeat of completed occurrence | Existing result; one financial effect set | Service/SQL |
| I02 | Two concurrent automatic callers | One committed execution | SQL |
| I03 | Automatic races Run Today's Occurrence | Same occurrence wins once; both callers see consistent result | SQL/API |
| I04 | Success committed, HTTP response lost | Retry returns completion without duplication | SQL/API |
| I05 | Same manual ID retried; different new manual IDs | Same ID executes once; new intents may execute repeatedly | SQL/API |
| I06 | Manual ID reused with changed task/action/payload | Conflict; no financial effects | SQL/API |
| I07 | Today + extra manual + automatic calls | One scheduled execution plus each distinct extra intent | SQL/API |
| F01 | Failure after financial writes but before completion | All financial writes rolled back; retry can succeed | SQL |
| F02 | Transfer fails after first leg/linkage write | No partial transfer or included-account postings survive | SQL |
| F03 | Process interrupted while executing | No stranded permanent claim or duplicate effects on recovery | SQL/integration |
| F04 | Commit acknowledgement lost or attempt logging fails | Resolve committed state; no false retry duplicates | SQL/integration |
| F05 | Validation/period/API task failure | Diagnostic retained; no false success; eligible for retry | Service/Function |
| C01 | Several missed scheduled dates | Catch up oldest first, once each, original posting dates | Rules/SQL |
| C02 | Missed monthly 28th recovered after 28th | Executes normally for the 28th occurrence | Rules/service |
| C03 | Failure in one task/user | Other tasks/users progress; no newer occurrence overtakes failed work for that task | Service |
| C04 | Rollout against years-old task history | Nothing before activation boundary replayed | Rules/SQL |
| C05 | Discovery interrupted before checkpoint save | Restart loses no due dates and creates no duplicates | SQL |
| E01 | Schedule edit with unmaterialized overdue dates | Old due occurrences preserved; new schedule affects future only | Rules/SQL |
| E02 | Timezone moves forward/back across date boundary | Existing occurrences unchanged; no duplicate task/date | Rules/SQL |
| A01 | Other user's context/execute/history/edit/delete | Denied without data leakage or effects | API/service/SQL |
| A02 | Ordinary caller labels request Automatic | Cross-user execution denied | API/service |
| A03 | Verified administrator dispatch | Authorized cross-user work executes for correct owner | API/service |
| U01 | Pending occurrence manual confirmation | Two choices; today's occurrence selected by default | UI |
| U02 | Completed occurrence extra execution | Explicit additional-transaction warning | UI |
| U03 | Automatic completion while confirmation open | Today's action returns existing result, never an extra | UI/API |
| U04 | Timeout/double click/context state refresh | Stable request ID; accurate state and error presentation | UI/API |

## Delivery checkpoints

1. **Specification (this step):** agree rules, map source gaps, define acceptance
   scenarios. No executable test claims, schema or behavior changes.
2. **API/database foundation:** implement occurrence identity, transaction safety,
   authorization and SQL-backed tests. Generate (never handwrite) the migration
   only after checking for unrelated pending model changes. Do not apply it to
   production. Keep the new scheduling path inactive until rollout is coordinated.
3. **Preferences/UI:** timezone selection, execution context and confirmation,
   stable manual IDs, cross-repository contract tests and UI verification.
4. **Function:** hourly dispatcher and bounded catch-up, reliable failure reporting.
   Verify locally with production jobs disabled and no financial side effects.
5. **Release:** additive API/schema first; compatible UI/Function changes next;
   reviewed activation boundary and history reconciliation; enable new dispatch
   only after draining/stopping the old dispatch path. A master push is production.

## Decisions to resolve during design/release

- Exact rollout instant and treatment of the activation day's already-run jobs:
  reconcile explicitly before enabling; do not infer from incomplete old history.
- Snapshot the execution inputs/revision of due work before edits so preserved
  occurrences do not accidentally use new amounts/accounts. Define deletion of
  tasks with outstanding work and invalid legacy monthly days before activation;
  do not silently execute, clamp, discard, or rewrite those records.
- Final retry/backoff, batch size, interrupted-claim recovery, and wire-level
  conflict/in-progress semantics. These are implementation decisions to test.
- Handling DST midnight gaps/ambiguities must produce one occurrence per local
  date. Use the first valid instant for a missing midnight and the first occurrence
  of an ambiguous midnight as the proposed rule; verify platform timezone support.

No commits, pushes, migration application or production changes are part of this
specification step. Review each delivery checkpoint before proceeding to the next.

## Implementation checkpoint: execution foundation

- Added occurrence snapshots and completion identities with SQL unique constraints.
  A transaction-owned SQL application lock serializes matching identities; timeout
  reports a retryable conflict. SQL constraints protect against bypassing the lock.
- Added `POST /api/ScheduledTasks/{taskId}/execution-v2`, disabled unless
  `ScheduledTasks:ExecutionV2Enabled` is explicitly true. Do not enable it during
  this checkpoint. Actor identity is taken from authentication, not request data.
- Requests supply either an existing due `occurrenceId`, or an independent
  `manualRequestId` plus `postingDate`, never both. Scheduled calls and manual
  fulfillment use the same occurrence ID. Extra requests are scoped to the caller.
- Repository checks current task ownership; cross-user occurrence execution also
  requires a persisted Admin role. Extra manual executions remain owner-only.
  The financial adapter additionally validates source/destination accounts and
  assigned category against the task owner's scope before any financial writes.
- Completed requests return their original execution ID. Reusing a manual identity
  with a different task or posting date is rejected. Scheduled work uses its stored
  snapshot/date and refuses future, unknown or out-of-order occurrences.
- Completion, financial effects and existing execution history share one SQL
  transaction. Transfers now use the unit-of-work transaction helper, joining an
  outer transaction or owning/disposal-managing one when called independently.
- Exceptions roll back and leave the identity retryable. Best-effort structured
  warning logs survive rollback; a durable attempt-history UI/table is not yet built.
- No occurrence discovery/checkpoint population, profile timezone, confirmation UI,
  hourly Function dispatch, or rollout/history reconciliation is implemented yet.
  Existing legacy routes have not been migrated to the v2 authorization policy;
  their audit findings remain open. Monthly/weekly validation helpers are tested
  but not wired into schedule writes until edit/version semantics are implemented.

Run all tests including SQL-backed execution tests in PowerShell:

```powershell
$env:MYFINANCE_RUN_SQL_TESTS = '1'
dotnet test MyFinanceWebApi.sln --configuration Release
Remove-Item Env:MYFINANCE_RUN_SQL_TESTS
```

The SQL fixture creates/deletes only a generated `MyFinance_ScheduledExecutionTests_*`
LocalDB database. It uses the full EF model, with a test-only correction to the
legacy TransferRecord primary-key clustering to allow EnsureCreated; production
model/migration clustering is unchanged. Placeholder task/account links in this
ledger-only fixture are not a substitute for financial integration tests.
