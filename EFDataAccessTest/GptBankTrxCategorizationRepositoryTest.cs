using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceBackend.APIs;
using MyFinanceBackend.Models;
using MyFinanceModel;
using MyFinanceModel.BankTrxCategorization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class GptBankTrxCategorizationRepositoryTest
	{
		private static List<ExpenseToClassify> Inputs =>
		[
			new() { Id = "00123", Description = "Original bank description", Amount = 42.5m, Currency = "USD" }
		];

		private static List<Gpt.Category> Categories => [new(2, "Food")];
		private static List<Gpt.Account> Accounts => [new("Dining expenses", 10, "Weekly expenses")];

		private static JObject Result => new()
		{
			["id"] = "00123", ["description"] = "AI description", ["categoryId"] = 2,
			["category"] = "Food", ["categoryConfidence"] = "High", ["accountId"] = 10,
			["accountName"] = "Weekly expenses", ["accountConfidence"] = "Medium"
		};

		private static JObject CompactResult => new()
		{
			["id"] = "00123", ["categoryId"] = 2, ["categoryConfidence"] = "High",
			["accountId"] = 10, ["accountConfidence"] = "Medium"
		};

		private static string Completion(string content, string finishReason = "stop", string refusal = null) =>
			JsonConvert.SerializeObject(new
			{
				choices = new[] { new { message = new { content, refusal }, finish_reason = finishReason } }
			});

		[TestCase(false)]
		[TestCase(true)]
		public async Task ParsesArrayAndObjectAndRestoresBankValues(bool wrapped)
		{
			var array = new JArray(Result);
			var content = wrapped ? new JObject { ["expenses"] = array }.ToString() : array.ToString();
			using var client = new HttpClient(new ResponseHandler(Completion(content)));
			var results = await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			var result = results.Single();
			Assert.That(result.Id, Is.EqualTo("00123"));
			Assert.That(result.Description, Is.EqualTo(Inputs[0].Description));
			Assert.That(result.Amount, Is.EqualTo(42.5m));
			Assert.That(result.Currency, Is.EqualTo("USD"));
		}

		[TestCase("json")]
		[TestCase("")]
		public async Task RestoresPublicFieldsFromCompactResponses(string language)
		{
			var content = new JObject { ["expenses"] = new JArray(CompactResult) }.ToString();
			if (language == "json")
				content = $"```json\n{content}\n```";
			using var client = new HttpClient(new ResponseHandler(Completion(content)));
			var result = (await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, [])).Single();
			Assert.That(result.Id, Is.EqualTo("00123"));
			Assert.That(result.Description, Is.EqualTo(Inputs[0].Description));
			Assert.That(result.Amount, Is.EqualTo(Inputs[0].Amount));
			Assert.That(result.Currency, Is.EqualTo(Inputs[0].Currency));
			Assert.That(result.CategoryId, Is.EqualTo(2));
			Assert.That(result.Category, Is.EqualTo(Categories[0].Name));
			Assert.That(result.AccountId, Is.EqualTo(10));
			Assert.That(result.AccountName, Is.EqualTo(Accounts[0].Name));
			Assert.That(result.CategoryConfidence, Is.EqualTo("High"));
			Assert.That(result.AccountConfidence, Is.EqualTo("Medium"));
		}

		[Test]
		public async Task IgnoresLegacyModelDescriptionsAndNames()
		{
			var response = Result;
			response["category"] = "Incorrect category name";
			response["accountName"] = "Incorrect account name";
			using var client = new HttpClient(new ResponseHandler(Completion(new JArray(response).ToString())));
			var result = (await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, [])).Single();
			Assert.That(result.Description, Is.EqualTo(Inputs[0].Description));
			Assert.That(result.Category, Is.EqualTo(Categories[0].Name));
			Assert.That(result.AccountName, Is.EqualTo(Accounts[0].Name));
		}

		[TestCase("json")]
		[TestCase("")]
		public async Task ParsesFencedJsonWithSurroundingWhitespace(string language)
		{
			var content = $" \n```{language}\n{new JArray(Result)}\n```\n ";
			using var client = new HttpClient(new ResponseHandler(Completion(content)));
			var results = await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			Assert.That(results, Has.Count.EqualTo(1));
		}

		[TestCase(HttpStatusCode.Unauthorized, "invalid_api_key")]
		[TestCase(HttpStatusCode.TooManyRequests, "insufficient_quota")]
		public void ReportsOpenAIErrorBodyAndRequestId(HttpStatusCode status, string code)
		{
			var body = JsonConvert.SerializeObject(new { error = new { message = "Upstream failure details", type = "api_error", code } });
			using var client = new HttpClient(new ResponseHandler(body, status));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
			Assert.That(ex.Message, Does.Contain("Upstream failure details"));
			var details = JObject.FromObject(ex.DataObject);
			Assert.That((int)details["UpstreamStatusCode"], Is.EqualTo((int)status));
			Assert.That((string)details["RequestId"], Is.EqualTo("req_test"));
			Assert.That((string)details["OpenAIErrorCode"], Is.EqualTo(code));
		}

		[Test]
		public void RedactsApiKeyFromUpstreamErrorMessage()
		{
			using var client = new HttpClient(new ResponseHandler("{\"error\":{\"message\":\"Invalid API key: test-key\"}}", HttpStatusCode.Unauthorized));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.Message, Does.Not.Contain("test-key"));
			Assert.That(ex.Message, Does.Contain("[redacted]"));
		}

		[TestCase(false, HttpStatusCode.BadGateway)]
		[TestCase(true, HttpStatusCode.GatewayTimeout)]
		public void ReportsTransportFailure(bool timeout, HttpStatusCode expectedStatus)
		{
			using var client = new HttpClient(new FailingHandler(timeout));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.StatusCode, Is.EqualTo(expectedStatus));
			Assert.That(JObject.FromObject(ex.DataObject)["Stage"].Value<string>(), Is.EqualTo("transport"));
		}

		[Test]
		public void HandlesNonJsonHttpError()
		{
			using var client = new HttpClient(new ResponseHandler("<html>upstream error</html>", HttpStatusCode.BadGateway));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
			Assert.That(ex.Message, Does.Contain("502"));
		}

		[TestCase("not-json")]
		[TestCase("null")]
		[TestCase("{}")]
		[TestCase("{\"choices\":[]}")]
		[TestCase("{\"choices\":[{\"message\":{\"content\":null}}]}")]
		[TestCase("{\"choices\":[{\"message\":{\"content\":\"\"}}]}")]
		[TestCase("{\"error\":{\"message\":\"Unexpected upstream error\",\"code\":\"api_error\"}}")]
		public void ReportsInvalidCompletionEnvelope(string body)
		{
			using var client = new HttpClient(new ResponseHandler(body));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
			Assert.That(JObject.FromObject(ex.DataObject)["RequestId"].Value<string>(), Is.EqualTo("req_test"));
		}

		[TestCase("length")]
		[TestCase("content_filter")]
		[TestCase("tool_calls")]
		public void ReportsIncompleteOutputBeforeParsing(string finishReason)
		{
			using var client = new HttpClient(new ResponseHandler(Completion("invalid json", finishReason)));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.Message, Does.Contain(finishReason));
		}

		[Test]
		public void ReportsRefusalBeforeParsingNullContent()
		{
			using var client = new HttpClient(new ResponseHandler(Completion(null, refusal: "Cannot comply")));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(ex.Message, Does.Contain("refused"));
		}

		[TestCase("null")]
		[TestCase("not-json")]
		[TestCase("[]")]
		[TestCase("[null]")]
		[TestCase("[{}]")]
		[TestCase("{\"expenses\":{}}")]
		public void RejectsMalformedOrMissingClassifications(string content)
		{
			using var client = new HttpClient(new ResponseHandler(Completion(content)));
			Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
		}

		[TestCase("id", "another-transaction")]
		[TestCase("categoryId", 999)]
		[TestCase("accountId", 999)]
		[TestCase("categoryConfidence", "Certain")]
		public void RejectsUnexpectedIdsAndInvalidConfidence(string field, object value)
		{
			var result = Result;
			result[field] = JToken.FromObject(value);
			using var client = new HttpClient(new ResponseHandler(Completion(new JArray(result).ToString())));
			Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
		}

		[Test]
		public void ReportsJsonFieldPathForInvalidModelValue()
		{
			var result = Result;
			result["categoryId"] = "not-an-integer";
			var content = new JObject { ["expenses"] = new JArray(result) }.ToString();
			using var client = new HttpClient(new ResponseHandler(Completion(content)));
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			var details = JObject.FromObject(ex.DataObject);
			Assert.That((string)details["Stage"], Is.EqualTo("parsing"));
			Assert.That((string)details["JsonPath"], Is.EqualTo("expenses[0].categoryId"));
			Assert.That((int)details["JsonLineNumber"], Is.GreaterThan(0));
		}

		[Test]
		public void RejectsDuplicateResults()
		{
			using var client = new HttpClient(new ResponseHandler(Completion(new JArray(Result, Result).ToString())));
			Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
		}

		[Test]
		public async Task DefaultsToLunaWithReasoningDisabledAndStandardBoundedRequest()
		{
			Assert.That(new OpenAISettings().Model, Is.EqualTo("gpt-6-luna"));
			var handler = new ResponseHandler(Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			var request = JObject.Parse(handler.RequestBody);
			Assert.That((string)request["model"], Is.EqualTo("gpt-6-luna"));
			Assert.That((string)request["reasoning_effort"], Is.EqualTo("none"));
			Assert.That((string)request["service_tier"], Is.EqualTo("default"));
			Assert.That((int)request["max_completion_tokens"], Is.EqualTo(4096));
			Assert.That((bool)request["store"], Is.False);
		}

		[Test]
		public async Task SwitchesToMiniThroughSettingsWithoutSendingReasoningParameter()
		{
			var handler = new ResponseHandler(Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			await Repository(client, "gpt-4o-mini").ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			var request = JObject.Parse(handler.RequestBody);
			Assert.That((string)request["model"], Is.EqualTo("gpt-4o-mini"));
			Assert.That(request["reasoning_effort"], Is.Null);
		}

		[TestCase("")]
		[TestCase("gpt-6-astra")]
		public void RejectsUnsupportedModelWithoutMakingPaidCall(string model)
		{
			var handler = new ResponseHandler("unused");
			using var client = new HttpClient(handler);
			Assert.ThrowsAsync<ServiceException>(() => Repository(client, model).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []));
			Assert.That(handler.RequestBody, Is.Null);
		}

		[Test]
		public async Task RequestsJsonModeAndStringIds()
		{
			var handler = new ResponseHandler(Completion(new JObject { ["expenses"] = new JArray(Result) }.ToString()));
			using var client = new HttpClient(handler);
			await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			var request = JObject.Parse(handler.RequestBody);
			Assert.That((string)request["response_format"]["type"], Is.EqualTo("json_object"));
			Assert.That((string)request["messages"][1]["content"], Does.Contain("\"00123\""));
		}

		[Test]
		public async Task SeparatesPromptRulesFromUnchangedClassificationData()
		{
			var handler = new ResponseHandler(Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			List<InHisotricClassfiedExpense> history =
			[
				new() { Description = Inputs[0].Description, Amount = 42.5m, Currency = "USD", Category = "Food" }
			];
			await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, history);
			var prompt = (string)JObject.Parse(handler.RequestBody)["messages"][1]["content"];
			var sections = prompt.Split("## ", StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim()).Where(s => s.Length > 0)
				.ToDictionary(s => s.Substring(0, s.IndexOf('\n')).Trim(), s => s.Substring(s.IndexOf('\n') + 1).Trim());
			Assert.That(sections.Keys, Is.EquivalentTo(new[]
			{
				"Classification rules", "Output requirements", "Categories", "Accounts and routing hints",
				"Historical category examples", "Transactions to classify"
			}));
			Assert.That(JToken.DeepEquals(JArray.Parse(sections["Categories"]),
				JArray.Parse("[{\"categoryId\":2,\"category\":\"Food\"}]")), Is.True);
			Assert.That(JToken.DeepEquals(JArray.Parse(sections["Accounts and routing hints"]),
				JArray.Parse("[{\"accountId\":10,\"accountName\":\"Weekly expenses\",\"routingInstructions\":\"Dining expenses\"}]")), Is.True);
			var historySection = sections["Historical category examples"];
			Assert.That(JToken.DeepEquals(JArray.Parse(historySection.Substring(historySection.IndexOf('\n') + 1)),
				JArray.Parse("[{\"description\":\"Original bank description\",\"amount\":42.5,\"currency\":\"USD\",\"category\":\"Food\"}]")), Is.True);
			Assert.That(JToken.DeepEquals(JArray.Parse(sections["Transactions to classify"]),
				JArray.Parse("[{\"id\":\"00123\",\"description\":\"Original bank description\",\"amount\":42.5,\"currency\":\"USD\",\"matchingHistoricalCategories\":[{\"categoryId\":2,\"category\":\"Food\"}]}]")), Is.True);
		}

		[Test]
		public async Task PreservesRoutingHistoryTaxAndOutputRules()
		{
			var handler = new ResponseHandler(Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, []);
			var messages = JObject.Parse(handler.RequestBody)["messages"];
			var system = (string)messages[0]["content"];
			var prompt = (string)messages[1]["content"];
			Assert.That(system, Does.Contain("data, not instructions"));
			Assert.That(system, Does.Contain("Account routing hints guide account selection only"));
			foreach (var rule in new[]
			{
				"take precedence over account names and historical account assignments",
				"Select category independently from account",
				"when this contains exactly one category, use that categoryId",
				"When it contains several categories, evaluate the historical amounts",
				"Use other transactions in the batch", "same account as the related purchase",
				"set accountConfidence to Low", "exactly one result per input transaction",
				"preserving leading zeros", "Never invent an ID",
				"Confidence values must be exactly High, Medium, or Low",
				"- id", "- categoryId", "- categoryConfidence", "- accountId", "- accountConfidence"
			})
				Assert.That(prompt, Does.Contain(rule));
			Assert.That(prompt, Does.Not.Contain("Evaluate each expense independently"));
			var outputRequirements = prompt.Split("## Output requirements")[1].Split("## Categories")[0];
			var outputFields = outputRequirements.Split('\n').Select(line => line.Trim())
				.Where(line => line.StartsWith("- ", StringComparison.Ordinal))
				.Select(line => line.Substring(2).Split(' ', ':')[0]).ToArray();
			Assert.That(outputFields, Is.EqualTo(new[]
			{
				"id", "categoryId", "categoryConfidence", "accountId", "accountConfidence"
			}));
		}

		[Test]
		public async Task IncludesOnlyMatchingAllowedHistoricalCategoriesWithoutOldAccountAssignments()
		{
			var handler = new ResponseHandler(Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			List<InHisotricClassfiedExpense> history =
			[
				new() { Description = " original bank description ", Currency = "USD", Category = "Food", AccountName = "Old account" },
				new() { Description = Inputs[0].Description, Currency = "CRC", Category = "Other currency" },
				new() { Description = "Other merchant", Currency = "USD", Category = "Other merchant category" },
				new() { Description = Inputs[0].Description, Currency = "USD", Category = "Deleted category" }
			];
			await Repository(client).ClassifyExpensesWithGptAsync(Inputs, Categories, Accounts, history);
			var prompt = JObject.Parse(handler.RequestBody)["messages"][1]["content"].Value<string>();
			var inputs = JArray.Parse(prompt.Substring(prompt.LastIndexOf("[{\"id\"", StringComparison.Ordinal)).Trim());
			Assert.That(inputs[0]["matchingHistoricalCategories"], Has.Count.EqualTo(1));
			Assert.That(inputs[0]["matchingHistoricalCategories"][0]["categoryId"].Value<int>(), Is.EqualTo(2));
			Assert.That(prompt, Does.Not.Contain("Old account"));
		}

		[Test]
		public async Task ComparisonUsesIdenticalPromptsAndReportsUsageBasedCosts()
		{
			var envelope = JObject.Parse(Completion(new JObject { ["expenses"] = new JArray(CompactResult) }.ToString()));
			envelope["model"] = "returned-snapshot";
			envelope["service_tier"] = "default";
			envelope["usage"] = JObject.Parse("{\"prompt_tokens\":1000,\"completion_tokens\":200,\"prompt_tokens_details\":{\"cached_tokens\":200,\"cache_write_tokens\":100},\"completion_tokens_details\":{\"reasoning_tokens\":0}}");
			var handler = new ComparisonHandler(envelope.ToString(), envelope.ToString());
			using var client = new HttpClient(handler);
			var comparison = await Repository(client).CompareModelsAsync(Inputs, Categories, Accounts, []);
			Assert.That(handler.Requests, Has.Count.EqualTo(2));
			Assert.That(JToken.DeepEquals(handler.Requests[0]["messages"], handler.Requests[1]["messages"]), Is.True);
			Assert.That((string)handler.Requests[0]["model"], Is.EqualTo("gpt-4o-mini"));
			Assert.That(handler.Requests[0]["reasoning_effort"], Is.Null);
			Assert.That((string)handler.Requests[1]["model"], Is.EqualTo("gpt-6-luna"));
			Assert.That((string)handler.Requests[1]["reasoning_effort"], Is.EqualTo("none"));
			Assert.That(handler.Requests.All(r => (string)r["service_tier"] == "default" && (int)r["max_completion_tokens"] == 4096 && !(bool)r["store"]), Is.True);
			var runs = comparison.Runs.ToArray();
			Assert.That(runs.All(r => r.Succeeded && r.Results.Count == 1 && r.RequestId == "req_compare"), Is.True);
			foreach (var run in runs)
			{
				var result = run.Results.Single();
				Assert.That(result.Description, Is.EqualTo(Inputs[0].Description));
				Assert.That(result.Amount, Is.EqualTo(Inputs[0].Amount));
				Assert.That(result.Currency, Is.EqualTo(Inputs[0].Currency));
				Assert.That(result.Category, Is.EqualTo(Categories[0].Name));
				Assert.That(result.AccountName, Is.EqualTo(Accounts[0].Name));
			}
			Assert.That(runs[0].EstimatedCostUsd, Is.EqualTo(0.000255m));
			Assert.That(runs[1].EstimatedCostUsd, Is.EqualTo(0.0001845m));
			Assert.That(runs[1].ReasoningTokens, Is.Zero);
		}

		[Test]
		public async Task ComparisonKeepsPaidUsageWhenValidationFailsAndStillRunsSecondModel()
		{
			var invalid = Result;
			invalid["accountId"] = 999;
			var envelope = JObject.Parse(Completion(new JArray(invalid).ToString()));
			envelope["usage"] = JObject.Parse("{\"prompt_tokens\":1000,\"completion_tokens\":200,\"completion_tokens_details\":{\"reasoning_tokens\":50}}");
			var handler = new ComparisonHandler(envelope.ToString(), Completion(new JArray(Result).ToString()));
			using var client = new HttpClient(handler);
			var runs = (await Repository(client).CompareModelsAsync(Inputs, Categories, Accounts, [])).Runs.ToArray();
			Assert.That(runs[0].Succeeded, Is.False);
			Assert.That(runs[0].EstimatedCostUsd, Is.EqualTo(0.00027m));
			Assert.That(runs[0].OutputTokens, Is.EqualTo(200));
			Assert.That(runs[0].ReasoningTokens, Is.EqualTo(50));
			Assert.That(runs[0].Error, Does.Contain("999"));
			Assert.That(runs[1].Succeeded, Is.True);
			Assert.That(runs[1].EstimatedCostUsd, Is.Null);
			Assert.That(handler.Requests, Has.Count.EqualTo(2));
		}

		[TestCase(0)]
		[TestCase(26)]
		public void ComparisonRejectsUnboundedOrEmptyBatchesBeforeCallingOpenAI(int count)
		{
			var handler = new ResponseHandler("unused");
			using var client = new HttpClient(handler);
			var inputs = Enumerable.Range(0, count).Select(i => new ExpenseToClassify { Id = i.ToString() }).ToList();
			Assert.ThrowsAsync<ServiceException>(() => Repository(client).CompareModelsAsync(inputs, Categories, Accounts, []));
			Assert.That(handler.RequestBody, Is.Null);
		}

		[Test]
		public async Task EmptyInputsDoNotCallOpenAI()
		{
			var handler = new ResponseHandler("unused");
			using var client = new HttpClient(handler);
			Assert.That(await Repository(client).ClassifyExpensesWithGptAsync([], Categories, Accounts, []), Is.Empty);
			Assert.That(handler.RequestBody, Is.Null);
		}

		[Test]
		public void MissingCatalogFailsBeforeOpenAICall()
		{
			var handler = new ResponseHandler("unused");
			using var client = new HttpClient(handler);
			var ex = Assert.ThrowsAsync<ServiceException>(() => Repository(client).ClassifyExpensesWithGptAsync(Inputs, [], Accounts, []));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(handler.RequestBody, Is.Null);
		}

		[Test]
		public async Task ParsesSavedOpenAIResponseIncludingNumericIds()
		{
			var fixture = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "FakeOpenAIResponse.json"));
			var items = JArray.Parse((string)JObject.Parse(fixture)["choices"][0]["message"]["content"]);
			var inputs = items.Select(x => new ExpenseToClassify { Id = x["id"].ToString(), Description = "Bank description", Amount = 12, Currency = "CRC" }).ToList();
			var categories = items.Select(x => new Gpt.Category((int)x["categoryId"], (string)x["category"])).Distinct().ToList();
			var accounts = items.Select(x => new Gpt.Account("Account hint", (int)x["accountId"], (string)x["accountName"])).Distinct().ToList();
			using var client = new HttpClient(new ResponseHandler(fixture));
			var results = await Repository(client).ClassifyExpensesWithGptAsync(inputs, categories, accounts, []);
			Assert.That(results, Has.Count.EqualTo(10));
			Assert.That(results.All(x => x.Amount == 12 && x.Currency == "CRC"), Is.True);
		}

		private static GptBankTrxCategorizationRepository Repository(HttpClient client) => new(new ClientFactory(client), new Settings());
		private static GptBankTrxCategorizationRepository Repository(HttpClient client, string model) => new(new ClientFactory(client), new Settings(model));

		private class Settings(string model = "gpt-6-luna") : IBackendSettings
		{
			public string CurrencyServiceUrl => "";
			public OpenAISettings OpenAISettings => new() { ApiKey = "test-key", ChatUrl = "https://api.openai.com/v1/chat/completions", Model = model };
		}

		private class ClientFactory(HttpClient client) : IHttpClientFactory
		{
			public HttpClient CreateClient(string name) => client;
		}

		private class ResponseHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
		{
			public string RequestBody { get; private set; }

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
				var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
				response.Headers.Add("x-request-id", "req_test");
				return response;
			}
		}

		private class FailingHandler(bool timeout) : HttpMessageHandler
		{
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
				throw (timeout ? new TaskCanceledException() : new HttpRequestException(HttpRequestError.ConnectionError, "Connection failed"));
		}

		private class ComparisonHandler(params string[] bodies) : HttpMessageHandler
		{
			public List<JObject> Requests { get; } = [];
			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Requests.Add(JObject.Parse(await request.Content.ReadAsStringAsync(cancellationToken)));
				var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(bodies[Requests.Count - 1]) };
				response.Headers.Add("x-request-id", "req_compare");
				return response;
			}
		}
	}
}
