using System;
using System.Net;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MyFinanceBackend.Services
{
    public static class ScheduledTaskDefinitionCodec
    {
        private const int BasicTaskType = 1;
        private const int TransferTaskType = 2;
        private const string TaskTypePropertyName = "taskType";

        public static string Write(BaseScheduledTaskVm definition) => JsonConvert.SerializeObject(definition,
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

        public static BaseScheduledTaskVm Read(string json)
        {
            try
            {
                var document = JObject.Parse(json);
                var type = document[TaskTypePropertyName];
                if (type?.Type != JTokenType.Integer || (type.Value<long>() != BasicTaskType && type.Value<long>() != TransferTaskType))
                    throw new JsonSerializationException("Unsupported task type.");
                return document.ToObject<BaseScheduledTaskVm>();
            }
            catch (Exception error) when (error is JsonException or ArgumentException or OverflowException)
            { throw new ServiceException("The stored task definition is invalid.", HttpStatusCode.Conflict); }
        }
    }
}
