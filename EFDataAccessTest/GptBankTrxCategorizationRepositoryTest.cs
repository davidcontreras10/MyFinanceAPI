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

		private class Settings : IBackendSettings
		{
			public string CurrencyServiceUrl => "";
			public OpenAISettings OpenAISettings => new() { ApiKey = "test-key", ChatUrl = "https://api.openai.com/v1/chat/completions" };
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
	}
}
