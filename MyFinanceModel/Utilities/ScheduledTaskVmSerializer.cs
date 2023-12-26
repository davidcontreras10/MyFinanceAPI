using System;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyFinanceModel.Utilities
{
	public class ScheduledTaskVmSerializer : JsonConverter<BaseScheduledTaskVm>
	{
		public override bool CanRead => true;
		public override bool CanWrite => false;

		public override void WriteJson(JsonWriter writer, BaseScheduledTaskVm value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override BaseScheduledTaskVm ReadJson(
			JsonReader reader,
			Type objectType,
			BaseScheduledTaskVm existingValue,
			bool hasExistingValue,
			JsonSerializer serializer
		)
		{
			var token = JToken.ReadFrom(reader);
			var jObject = JObject.Load(token.CreateReader());
			var type = (ScheduledTaskType) jObject["taskType"].ToObject<int>();
			BaseScheduledTaskVm result;
			switch (type)
			{
				case ScheduledTaskType.Invalid:
					throw new ArgumentOutOfRangeException();
				case ScheduledTaskType.Basic:
					result = new BasicScheduledTaskVm();
					serializer.Populate(token.CreateReader(), result);
					break;
				case ScheduledTaskType.Transfer:
					result = new TransferScheduledTaskVm();
					serializer.Populate(token.CreateReader(), result);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}
	}
}