# AI prompt comparison

One-time live diagnostic on 2026-10-04 comparing the two prompt improvements. This is not an
automated integration test or a classification-accuracy evaluation.

## Method

- Historical repository implementations were read from Git and compiled in the gitignored diagnostic
  project with only their class names changed. No branch checkout or application-code replacement.
- One local database snapshot supplied the same 10 recorded Scotiabank transactions, 18 categories,
  6 hinted accounts and 21 cleaned historical examples to all three implementations. Sampling followed
  the existing model-comparison endpoint's selection logic; the snapshot was loaded once before calls.
- Three sequential paid Chat Completions calls, one per revision, with no retries.
- All requested and returned `gpt-6-luna`, standard service tier, `reasoning_effort: none`, temperature
  0.2, JSON mode and a 4096-token completion limit. Reported reasoning tokens were zero.
- Repository calls bypassed classification-cache lookup/writes; no SQL or MongoDB data was changed.
- Every response passed the revision's existing parser/ID/confidence validation and produced 10 full
  application results. Accuracy and agreement between classifications were not scored.

## Results

Each row represents one request containing the same 10 transactions, not one transaction.

| Version | Revision | Input tokens | Output tokens | Cache-write tokens | Estimated USD | Uncached-equivalent USD | Elapsed seconds |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Original prompt | `f1708ae` | 2262 | 503 | 2259 | 0.000534175 | 0.0004777 | 4.423 |
| Wording cleanup | `1462b3c` | 2276 | 503 | 2273 | 0.000535925 | 0.0004791 | 3.203 |
| Compact response | `5fe4db4` | 2251 | 296 | 2248 | 0.000429300 | 0.0003731 | 2.854 |

Cached input reads were zero for every call. Estimated USD uses the reported cache-write usage;
uncached-equivalent USD treats all input as ordinary uncached input to separate prompt size from
cache behavior. Neither is an invoice. Elapsed time covers the repository call, including parsing
and local diagnostic capture, not just server inference time.

[Official Luna pricing](https://developers.openai.com/api/docs/models/gpt-6-luna), checked 2026-10-04,
was USD 0.10 per million uncached input tokens, 0.01 per million cached input tokens, 0.125 per million
cache-write tokens and 0.50 per million output tokens. Cache writes are a subset of input usage here;
they are charged separately, not also as ordinary input.

## Interpretation

- Wording cleanup improved organization but did not reduce token usage in this sample: 14 more input
  tokens and unchanged output usage. It should not be described as a measured cost optimization.
- Latest versus original: 41.2% fewer output tokens, 11 fewer input tokens, 19.6% lower estimated cost,
  and 21.9% lower uncached-equivalent cost. The compact response accounts for the observed savings.
- Total estimated cost for all three calls was USD 0.0014994.
- Latency was lower for the latest version in this run, but one call per revision is not a benchmark.
  Future usage, cache state, response formatting and classification decisions can change these figures.

Raw snapshots, requests, responses and the machine-readable report are local-only under the gitignored
`artifacts/classification-check/` directory; financial payloads are not included in this document.
The current NUnit suite also passed all 116 tests after the diagnostic.

## Structured Outputs follow-up

After adding strict schema support and usage logging, four further calls on 2026-10-04 reused the exact
saved snapshot above (verified by SHA-256), without reloading the database or accessing MongoDB.
For each format, the existing comparison repository called GPT-4o mini then Luna. Messages, inputs and
model settings were identical across formats; only `response_format` changed. JSON mode was rerun to
provide a contemporaneous baseline, rather than relying solely on earlier latency/cache observations.

| Model | Format | Input tokens | Output tokens | Cache-write tokens | Estimated USD | Uncached-equivalent USD | HTTP seconds |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| GPT-4o mini | JSON mode | 2252 | 484 | 0 | 0.00062820 | 0.00062820 | 6.116 |
| GPT-4o mini | Strict schema | 2344 | 290 | 0 | 0.00052560 | 0.00052560 | 3.969 |
| Luna | JSON mode | 2251 | 296 | 2248 | 0.00042930 | 0.00037310 | 2.419 |
| Luna | Strict schema | 2341 | 298 | 2338 | 0.00044155 | 0.00038310 | 2.916 |

Every call returned HTTP 200, standard service tier, zero reasoning tokens and 10 results passing
existing validation. Cached input reads were zero. Both models accepted the same fixed strict schema.
GPT-4o mini used its documented USD 0.15 input / 0.60 output per million token rates;
[official model pricing](https://developers.openai.com/api/docs/models/gpt-4o-mini). Luna rates are as above.

- Luna: schema added 90 input tokens and 2 output tokens. Estimated cost increased 2.85%
  (USD 0.00001225 per 10-transaction request); uncached-equivalent cost increased 2.68%.
- GPT-4o mini: schema added 92 input tokens but output fell by 194 tokens, lowering estimated cost
  16.3% in this run. Output formatting and token usage can vary, so this is not a guaranteed saving.
- Luna with schema still used 40.8% fewer output tokens and had 17.3% lower estimated cost than the
  original pre-cleanup prompt measured earlier on the same snapshot.
- Total estimated cost for the four follow-up calls was USD 0.00202465. Latency results are individual
  observations, not benchmarks. Classification accuracy and agreement were not scored.

The machine-readable follow-up is local-only at `artifacts/classification-check/schema-comparison.json`.
No application data was written. All 129 NUnit tests passed, including schema/configuration, existing
validation, legacy responses and usage-log safety checks.
