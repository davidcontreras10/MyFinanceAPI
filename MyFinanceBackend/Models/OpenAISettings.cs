namespace MyFinanceBackend.Models
{
	public class OpenAISettings
	{
		public string ApiKey { get; set; }
		public string ChatUrl { get; set; }
		public string Model { get; set; } = "gpt-6-luna";
		public bool UseStructuredOutputs { get; set; } = true;
	}
}
