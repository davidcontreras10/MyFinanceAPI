using MyFinanceBackend.Data;
using MyFinanceModel.BankTrxCategorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MyFinanceBackend.APIs.BankTrxCategorizationRepository;

namespace MyFinanceBackend.APIs
{
	public class BankTrxCategorizationRepository(IHttpClientFactory httpClientFactory) : IBankTrxCategorizationRepository
	{
		public enum GptModel
		{
			Gpt35Turbo,
			Gpt4Turbo
		}

		private const int MaxHistoricalExamples = 100;

		public async Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExpensesWithGptAsync(
			List<ExpenseToClassify> inputExpenses,
			List<Gpt.Category> categories,
			List<Gpt.Account> accountDescriptions,
			List<InHisotricClassfiedExpense> historicalExamples
		)
		{
			var model = GptModel.Gpt35Turbo;
			var categoriesStr = string.Join(", ", categories.Select(c => $"{c.Id} - {c.Name}"));
			var accountsStr = string.Join(", ", accountDescriptions.Select(a => $"{a.Name} (ID: {a.Id}) - {a.Description}"));
			var examplesStr = string.Join("\n", historicalExamples.Take(MaxHistoricalExamples).Select(e =>
				$"- \"{e.Description}\" ({e.Currency} {e.Amount}) → Category: {e.Category}, Account: {e.AccountName}"));

			var inputsStr = string.Join("\n", inputExpenses.Select(e =>
				$"- ID: {e.Id}, \"{e.Description}\" ({e.Currency} {e.Amount})"));

			var fullPrompt = $@"
You are a financial assistant that classifies expenses based on their description, amount, and currency.

Each expense must be classified with:
1. A category from this list: [{categoriesStr}]
2. An internal account from this list: [{accountsStr}]

Use historical examples as guidance. Be consistent with past classification, including subtle details (vendor name, price, currency):
{examplesStr}

For each one, return a JSON object with:
- id (copied from the input)
- description
- category (from the list above)
- categoryId (integer ID of the matched category)
- categoryConfidence: High / Medium / Low
- accountName (from the list above)
- accountId (integer ID of the matched account)
- accountConfidence: High / Medium / Low

Important: Return only the JSON array, and do not include markdown formatting (no ```json).
{inputsStr}
";

			var requestBody = new
			{
				model = model.GetModelName(),
				messages = new[]
				{
					new { role = "system", content = "You are a helpful financial assistant." },
					new { role = "user", content = fullPrompt }
				},
				temperature = 0.2
			};

			var requestJson = JsonSerializer.Serialize(requestBody);
			var responseString = await CallOpenAIAsync(requestJson);
			//var responseString = await openAICaller.FakeCallOpenAIAsync(requestJson);
			using var doc = JsonDocument.Parse(responseString);
			var rawContent = doc.RootElement
				.GetProperty("choices")[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString();

			rawContent = CleanGptJson(rawContent);
			var classifiedExpenses = JsonSerializer.Deserialize<IReadOnlyCollection<OutGptClassifiedExpense>>(rawContent);
			CompleteClassifiedExpenseResults(inputExpenses, classifiedExpenses);
			return classifiedExpenses ?? [];
		}

		private static string CleanGptJson(string rawContent)
		{
			if (rawContent.StartsWith("```json"))
			{
				rawContent = rawContent.Substring(7);
			}
			else if (rawContent.StartsWith("```"))
			{
				rawContent = rawContent.Substring(3);
			}

			if (rawContent.EndsWith("```"))
			{
				rawContent = rawContent.Substring(0, rawContent.Length - 3);
			}

			return rawContent.Trim();
		}

		private static void CompleteClassifiedExpenseResults(List<ExpenseToClassify> inputExpenses, IReadOnlyCollection<OutGptClassifiedExpense> results)
		{
			var inputDict = inputExpenses.ToDictionary(e => e.Id);
			foreach (var result in results)
			{
				if (inputDict.TryGetValue(result.Id, out var inputExpense))
				{
					result.Description = inputExpense.Description;
					result.Amount = inputExpense.Amount;
					result.Currency = inputExpense.Currency;
				}
			}
		}

		private async Task<string> CallOpenAIAsync(string requestJson)
		{
			var openAIAPIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
			var httpClient = httpClientFactory.CreateClient("OpenAI");
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIAPIKey);
			var response = await httpClient.PostAsync(
				"https://api.openai.com/v1/chat/completions",
				new StringContent(requestJson, Encoding.UTF8, "application/json"));
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			return responseString;
		}
	}

	public static class GptModelExtensions
	{
		public static string GetModelName(this GptModel model) => model switch
		{
			GptModel.Gpt35Turbo => "gpt-3.5-turbo",
			GptModel.Gpt4Turbo => "gpt-4-turbo",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}
