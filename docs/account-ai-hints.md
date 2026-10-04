# Account AI Classification Hints

`GET /api/Accounts/{accountId}/ai-classification-hint`

Returns HTTP 200 with `accountId`, `accountName`, and the current `aiClassificationHint`, using the same
response shape as PUT below. Accounts with no hint are still returned, with a null hint after clearing;
they are not treated as missing accounts. This read-only endpoint requires the authenticated owner.
Nonexistent accounts and accounts owned by another user both return 404. It does not write data or call OpenAI.

`PUT /api/Accounts/{accountId}/ai-classification-hint`

Requires the existing bearer authentication. The account must belong to the authenticated user;
the request does not accept a user ID. Only `Account.AiClassificationHint` is updated.

Set or replace a hint:

```json
{
  "aiClassificationHint": "AI subscriptions such as OpenAI, ChatGPT and Claude. Include the associated digital-service IVA."
}
```

Clear it:

```json
{
  "aiClassificationHint": null
}
```

The field must be present; an empty object or missing body is not a clear command. Empty or whitespace-only
text also clears the hint. Nonblank text is trimmed at both ends, with internal formatting preserved.
The maximum submitted length is 4000 characters, limiting the hint contribution to classification prompts.

A successful request returns HTTP 200 with the persisted account ID, name and hint:

```json
{
  "accountId": 4016,
  "accountName": "Ingresos Ahorros",
  "aiClassificationHint": "AI subscriptions such as OpenAI, ChatGPT and Claude. Include the associated digital-service IVA."
}
```

Invalid input returns 400. A nonexistent account or one owned by another user returns the same 404 response.
The existing authentication requirement applies; unauthorized requests cannot update hints.

Only accounts with nonblank hints are offered as candidates for fresh AI classification. No additional
enable/disable flag is needed. Clearing a hint excludes that account from fresh AI calls; setting it makes
it available again. Hint updates do not rewrite transactions or invalidate existing classification-cache
entries, so cached results can still reflect an earlier hint or a now-cleared account. The model-comparison
test endpoint bypasses that cache when testing current hints. This endpoint itself makes no OpenAI calls.

The existing database field is reused; no migration is required.

## Knowing which accounts have a hint

The hint text is only returned by the GET above. To let a screen show which accounts have one without a request
per account, the account list (`GET /api/Accounts/{accountGroupId}`), the edit view model and the period list
return `hasAiClassificationHint` (true for a non-blank hint). Each entry of `subAccounts` carries the same flag.
It is read from the account, so it is current after a PUT.
