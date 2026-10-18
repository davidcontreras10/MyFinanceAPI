# Reliable scheduled-task execution

Status: specification, execution ledger, bounded occurrence discovery, versioned
schedule history, profile timezones, new-task registration and backend execution workflows implemented.
The v2 execution path remains disabled.
Ledger/history, timezone, retirement and execution-attempt migrations are generated; production migration application remains pending.
The frontend contains the manual-run choice flow and falls back while history is disabled.
The Function retains its legacy daily path; the new hourly dispatcher remains opt-in.
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
- Deletion retires a task and cancels all incomplete automatic work, including undiscovered missed
  dates. Preserve task definitions, occurrences, legacy history and completed receipts. Retired tasks
  cannot be edited, reactivated or executed with a new identity. Already committed retries return
  their original receipt. An execution already holding the owner lock may commit before retirement;
  retirement waits for that outcome rather than interrupting a financial transaction halfway through.
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
- Immutable effective-dated revisions now preserve the old transaction inputs and
  timezone even if due work is discovered after an edit. Retirement now cancels outstanding work
  and preserves history. Invalid legacy monthly days block
  activation/revision; they are not silently clamped or rewritten.
- Final retry/backoff, batch size, interrupted-claim recovery, and wire-level
  conflict/in-progress semantics. These are implementation decisions to test.
- DST midnight gaps use the first valid instant of that local date; ambiguities
  use the earliest UTC instant. A civil date skipped entirely has no occurrence.
  System timezone data and IANA/Windows support are exercised in calendar tests.

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
  warning logs survive rollback; durable attempt records are described below.
- Confirmation UI and hourly Function dispatch are implemented in later checkpoints.
  Rollout/history reconciliation and full financial workflow tests remain pending.
  Existing execution/history/delete routes now use domain authorization and approved scopes. Monthly/weekly
  validation is used by activation/revisions and the validated creation path described below.

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

## Implementation checkpoint: discovery and schedule history

- `ScheduledTaskSchedule` persists an explicit activation boundary and a monotonic
  next-scan UTC cursor. Deploying its schema does not activate anything. Activation
  is administrator-only, idempotent for the same boundary/initial timezone, and
  refuses resetting an existing boundary. There is no public activation endpoint.
- `ScheduledTaskRevision` stores an immutable transaction definition and timezone
  for a half-open UTC interval. SQL permits only one open revision per task and
  rejects nonpositive intervals. Old revisions are retained for catch-up.
- Discovery scans at most 31 UTC days per task/call, intersects each revision's
  interval, and adds each `(task, local date)` only once. Dates and cursor advancement
  commit together under a task-scoped SQL lock. Interrupted discovery can be retried
  without skipped dates; larger gaps return `HasMore` for another bounded pass.
- Edits obtain an owner/task scope from the dedicated scheduled-task authorization service, including
  approval of replacement spend categories. Both legacy and history-enabled writes enforce that scope
  transactionally; body IDs cannot override the approved task. Denials use 403 (missing identity: 401).
- Owner-authorized edits use this boundary when history maintenance is enabled. The original edit
  and new revision share a transaction; a failed edit or snapshot validation rolls
  both back. Due work at the exact edit instant still uses the old revision. A
  timezone change affects future scheduling and never rewrites existing dates.
- The new calendar enforces monthly days 1-28, supports weekly Sunday = 0 and all
  month dates, and handles fractional-hour offsets and midnight DST changes using
  .NET timezone APIs. Scheduled local dates remain date values; due instants remain
  UTC. This does not change the legacy UTC daily-list implementation or its Sunday
  parsing issue; the new discovery path reads raw stored schedule days correctly.
- Pending-work queries return only the earliest incomplete occurrence per task,
  with owner filtering or verified administrator access. Cross-user scan listings
  are administrator-only and bounded. The Function is not yet calling these APIs.
- Monthly/weekly validation is enforced during activation, revisions and API creation/editing,
  including when history maintenance is disabled. Weekly Sunday is preserved in DTO parsing.
- Revision changes are serialized with discovery. They do not need to materialize
  an unbounded overdue backlog during an interactive edit: later discovery uses
  the retained older revisions to reconstruct the original dates and inputs.
- DELETE now retires tasks without removing history. Its cancellation and concurrency policy is
  implemented in the backend workflow checkpoint below.

Release safety: do not enable either setting until full financial workflow tests and client/dispatcher
adoption are complete. Both default
to false. `ScheduledTasks:HistoryEnabled` protects schedule activation/discovery and
revision-aware edits independently of `ScheduledTasks:ExecutionV2Enabled`. After
activation, keep history maintenance enabled even when pausing v2 execution. Turning
history maintenance off requires freezing edits and an explicit data/rollback plan;
it is not the execution pause switch. No activation or flag change has been performed.

## Implementation checkpoint: preferences and registration

- The profile stores an explicit, validated IANA timezone in `AppUser.SchedulingTimeZoneId`.
  Its required-column/default configuration lives in Fluent API, not handwritten migration code.
  Existing/new users receive `America/Costa_Rica` through the database default. EF generated the additive
  `20261015173512_ScheduledUserTimeZone` migration without manual edits; apply it before deploying this API build.
- `GET`/`PUT /api/Users/scheduling-preferences?targetUserId=<user ID>` use the existing user
  View/Edit authorization policy. PUT accepts a `timeZoneId`; both return the saved `timeZoneId`.
  Missing identity remains 401, denial is 403, invalid timezone is 400, and disabled history is 503.
  These endpoints do not depend on the unfinished general profile-update repository method.
- An atomic profile change appends future timezone revisions for all activated tasks. Existing
  occurrences and unmaterialized due work keep their original snapshots, including a midnight due
  exactly at the change instant. Repeating the same preference creates no new revisions.
- History operations lock owner first, task second. Registration, activation, discovery, schedule
  edits and preference changes therefore cannot leave a newly created task on a stale timezone.
  A failure rolls back the profile and every revision changed in that request.
- Both creation endpoints authorize source/destination accounts and category, forward an approved
  owner scope, validate monthly 1-28/weekly 0-6/manual schedules, and recheck references during writes.
  With history enabled, task/definition/initial revision commit together, using the saved profile zone
  and creation instant as the activation boundary. No earlier dates or old tasks are auto-activated.
  With history disabled, creation remains validated/owner-scoped but writes no schedule-history rows.
- Timezone selection UI, execution confirmation,
  hourly dispatch and reviewed rollout activation remain later checkpoints. Neither feature flag has
  been enabled and no production database has been migrated or activated by this work.

## Implementation checkpoint: execution context and retirement

- Owner-authorized execution context supplies today's shared occurrence, earliest pending work,
  discovery completeness, execution enablement, default action and extra-execution warning. One read
  scans at most 31 UTC days; repeated reads advance bounded catch-up. Incomplete discovery never
  silently defaults to an extra run, and older pending dates still block newer scheduled work.
- Manual choices use the existing `execution-v2` contract: send `occurrenceId` for the displayed due
  occurrence, or a fresh `manualRequestId` plus `postingDate` for a deliberate extra. Retries preserve
  that identity. A stale confirmation returns an existing completion and never becomes an extra run.
  `DefaultAction` is 1 for today's occurrence, 2 for extra, or null when a default is unsafe/unavailable.
  Occurrence status is 1 pending, 2 completed, 3 canceled. The Angular UI uses this context in the
  manual execution-choice checkpoint below.
- Owner-scoped history lists successful receipts and materialized canceled occurrences with bounded
  pagination. Old `ExecutedTask` history remains available through the owner-authorized legacy route;
  it is not inferred into v2 completions. Owner-facing failed/interrupted-attempt history remains future work.
- DELETE stores `AutomaticTask.RetiredAtUtc` instead of physically deleting anything. It cancels all
  incomplete automatic work and hides the task from normal lists/dispatch. Undiscovered canceled dates
  are covered by the task tombstone, not expanded into an unbounded set of artificial history rows.
  No restore endpoint exists; clearing the tombstone by hand is not a supported recovery procedure.
- Execution and retirement serialize under an owner-first transaction lock. A completed identity
  remains idempotent after retirement; pending or new manual identities cannot create financial effects.
  An in-flight rollback followed by retirement leaves no financial effects and cancels the retry.
- Dedicated authorization covers context, history, retirement and both execution routes. Cross-user
  scheduled execution requires a persisted administrator grant; manual extras remain owner-only.
  The legacy execution route is blocked once history is enabled so old clients cannot bypass identities.
- EF generated the nullable retirement-column migration from Fluent API without manual edits.
  It has not been applied to a database. Apply it before running/deploying this API build, even with
  both flags disabled, because ordinary task queries now include the column. Production is unchanged.

## Implementation checkpoint: timezone editor

- The Angular Scheduled Tasks screen now loads profile preferences on demand and
  offers an explicit timezone choice, with confirmation before PUT. The saved
  preference always wins over device detection; detected device timezone is only
  an unsaved suggestion. API 404/503 leaves preferences unavailable without
  changing task execution or writing a client-invented default.
- Authenticated `GET`/`PUT /api/Users/@current/scheduling-preferences` resolve the
  owner from the authenticated identity and authorize only that user's own
  preference through the self-preference policy. The original administrator/
  target-user routes retain their existing View/Edit policy. No token decoding
  or client-supplied owner ID is needed by the editor.
- This checkpoint does not adopt execution-v2, change Function schedules, enable
  either flag, apply migrations or activate historical tasks. Execution-choice
  confirmation and stable manual identities are covered by the later UI checkpoint.

## Implementation checkpoint: manual execution choice UI

- The Scheduled Tasks Run now action reads the owner-authorized execution context.
  When today's occurrence is pending, the dialog defaults to that occurrence and
  lets the user choose an independent extra run. It warns when extra work leaves
  an automatic occurrence pending.
- Confirmed v2 requests persist their occurrence or manual request identity and
  posting date in browser storage before submission. Failures retain the same
  identity for retry; success clears it. Repeated clicks are guarded while the
  request is active.
- When history is off, the explicit history-disabled response preserves the
  existing legacy confirmation and route. A missing context route also supports
  older API versions. Other failures do not fall back, and a context reporting
  v2 disabled never invokes legacy execution.
- The existing execution-history panel still reads legacy records. A separate
  owner-facing attempt-history API/UI is implemented; full financial transaction
  tests remain required before enabling either feature flag.

## Implementation checkpoint: hourly dispatcher

- `POST /api/ScheduledTasks/dispatch-v2` verifies the caller through persisted
  administrator checks, scans at most 50 activated schedules, and executes at
  most 50 due occurrences per request. Each task contributes only its oldest
  pending occurrence, so a failed older occurrence blocks newer ones for that task.
- Discovery and execution failures are returned per task while other tasks
  continue. Failed occurrences stay pending for a later batch; `HasMoreDiscovery`
  and `HasMoreDueWork` indicate bounded work remains.
- The additive `ScheduledTasksV2` timer runs hourly but is a no-op unless
  `scheduledTasksV2Enabled` is explicitly true. The API also requires
  `ScheduledTasks:HistoryEnabled`, `ExecutionV2Enabled` and `DispatcherEnabled`.
  All remain off by default; the existing daily Function is unchanged and must
  be stopped before any coordinated production activation.
- No migration was applied, no flag was enabled and nothing was deployed.

## Implementation checkpoint: durable execution attempts

- `ScheduledTaskExecutionAttempt` stores a numbered attempt for each occurrence or
  manual request identity, including owner, actor, posting date, UTC start/end,
  outcome, failure stage/type and a bounded safe failure summary.
- A start row commits before financial work. On success, the attempt's succeeded
  state and execution completion commit in the same financial transaction. On
  failure, financial work rolls back and the attempt is marked failed separately.
  If a later retry finds an unfinished attempt, it records it as interrupted first.
- A completion remains authoritative after an ambiguous response. The repository
  checks the persisted identity before recording failure; diagnostic-write errors
  never turn a committed financial result into a reported execution failure.
- SQL tests cover durable callback and completion-write failures, safe retry with
  incremented attempt numbers, and recovery of an unfinished attempt. Full spend/
  transfer integration coverage remains outstanding.
- EF generated `20261018033003_ScheduledTaskExecutionAttempts` from Fluent API and
  it was applied to the local database on 2026-10-17. Production migration application
  remains pending. The owner-facing attempt-history API/UI is implemented, with
  focused UI coverage for loading, pagination and the feature-disabled response.
