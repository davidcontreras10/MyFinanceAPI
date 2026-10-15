using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientAccountAiClassificationHint
	{
		public const int MaxHintLength = 4000;

		[JsonProperty(Required = Required.AllowNull)]
		[StringLength(MaxHintLength)]
		public string AiClassificationHint { get; set; }
	}
}
