# Scheduled Task Edit — API Contract

`PATCH /api/ScheduledTasks/{taskId}`

Partial-update endpoint for an existing scheduled task. Only the fields listed in `modifyList`
are applied — everything else in the payload is ignored server-side.

## Auth

Bearer token, same as every other endpoint. Ownership is enforced server-side: the task must
belong to the calling user.

`taskId` goes in the URL path (the `id` GUID from the scheduled-task list responses), **not** in
the body. If `taskId` is included in the JSON body it's ignored/overwritten.

## Request body

```ts
{
  amount?: number;          // float
  spendTypeId?: number;
  isPending?: boolean;
  description?: string;
  frequencyType?: number;   // ScheduledTaskFrequencyType — see enum below
  days?: number[];
  modifyList: number[];     // REQUIRED — ScheduledTaskField values, see enum below
}
```

Send just the fields you're changing plus `modifyList`; no need to round-trip the rest of the
task's current values.

## Enums

**`frequencyType`** (`ScheduledTaskFrequencyType`):

| Value | Meaning |
|---|---|
| 0 | Invalid — never send |
| 1 | Monthly |
| 2 | Weekly |
| 3 | Manual |

**`modifyList` entries** (`ScheduledTaskField` — which fields to actually write):

| Value | Field |
|---|---|
| 0 | Invalid — never send |
| 1 | Amount |
| 2 | SpendTypeId |
| 3 | IsPending |
| 4 | Description |
| 5 | FrequencyType |
| 6 | Days |

## The FrequencyType/Days rule

- **Modifying `frequencyType` to `Monthly` (1) or `Weekly` (2) requires `days` in the same
  request** — i.e. if `modifyList` contains `5` and `frequencyType` is `1` or `2`, `modifyList`
  must also contain `6`, with a non-empty `days` array.
- **Modifying `frequencyType` to `Manual` (3) does *not* require `days`** — a manual task only
  runs on demand from a UI button, so there's no day schedule to provide. You can still include
  `days` alongside it if you want (e.g. to preserve/change the value for if it's ever switched back
  to Monthly/Weekly later), but it's optional in this case.
- **`days` can be modified alone** (`modifyList: [6]`), without touching `frequencyType` — e.g. a
  weekly task just changing which days of the week fire.

## `days` value semantics

Unchanged from create/GET — documenting here since `days` is now editable independently.

- **Weekly:** `.NET DayOfWeek` ordinals — `0`=Sunday, `1`=Monday, `2`=Tuesday, `3`=Wednesday,
  `4`=Thursday, `5`=Friday, `6`=Saturday.
- **Monthly:** day-of-month, `1`-`31`.
- **Manual:** not used for scheduling (manual tasks never auto-fire), and — unlike Monthly/Weekly —
  not required when switching `frequencyType` to `Manual`.

⚠️ **The backend does not validate that day values are in a sane range for the frequency type** —
nothing stops you from sending `days: [45]` for Monthly or `days: [10]` for Weekly. That validation
needs to live client-side for now.

## Response

`200 OK`, **empty body** on success. Re-fetch (`GET /api/ScheduledTasks/@current`) if you need the
updated task back — this endpoint doesn't return a representation.

## Error shapes

| Status | When | Body |
|---|---|---|
| `401` | Missing/invalid token, **or** task doesn't belong to the caller, **or** `taskId` doesn't exist at all (ownership check runs first, so a bad ID looks like "not yours," not "not found") | `{ message }` |
| `400` | `frequencyType` set to Monthly/Weekly in `modifyList` without `days` → `"Days must be provided when modifying FrequencyType to Monthly or Weekly"`. `days` in `modifyList` but null/empty → `"Days cannot be empty"` | `{ message, errorCode, dataObject }` |
| `500` | ⚠️ Malformed payload: `modifyList` missing/empty, or contains `0`/an unrecognized value | `{ message }` |

That last row is a known rough edge: those malformed-payload cases are guarded with a plain
`ArgumentException` in the repository, which the global exception filter maps to `500` rather than
`400` (only `ServiceException` and `UnauthorizedAccessException` get mapped to a specific status —
anything else falls through to `500`). Doesn't matter if the frontend always sends a well-formed
`modifyList`, but a defensive UI shouldn't expect a clean `400` here if that guard is ever hit by a
bug — it'll come back as a `500` with a generic message.

## Examples

Update only the description:

```json
PATCH /api/ScheduledTasks/3fa85f64-5717-4562-b3fc-2c963f66afa6
{ "description": "Netflix subscription", "modifyList": [4] }
```

Switch to weekly, Mon/Wed/Fri:

```json
{ "frequencyType": 2, "days": [1, 3, 5], "modifyList": [5, 6] }
```

Just change which days (still weekly):

```json
{ "days": [0, 6], "modifyList": [6] }
```

Switch to manual — no `days` needed:

```json
{ "frequencyType": 3, "modifyList": [5] }
```

Invalid — will 400 (`"Days must be provided when modifying FrequencyType to Monthly or Weekly"`):

```json
{ "frequencyType": 1, "modifyList": [5] }
```
