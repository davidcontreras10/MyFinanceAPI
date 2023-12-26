using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using DContre.MyFinance.StUtilities;
using MyFinanceModel.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyFinanceModel
{
	[Serializable]
    public class ServiceException : Exception
    {
	    private const string DEFAULT_MESSAGE = "Unknown error";
		private const int DEFAULT_ERROR_CODE = 1;

		private int _errorCode = DEFAULT_ERROR_CODE;

	    public ServiceException() : base(DEFAULT_MESSAGE)
        {
            StatusCode = HttpStatusCode.InternalServerError;
		    if (ErrorCode == 0)
			    ErrorCode = DEFAULT_ERROR_CODE;
        }

        public ServiceException(string message) : base(message)
        {
			if (ErrorCode == 0)
				ErrorCode = DEFAULT_ERROR_CODE;
            StatusCode = HttpStatusCode.InternalServerError;
        }

		public ServiceException(string message, int errorCode = DEFAULT_ERROR_CODE, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

		public ServiceException(SerializationInfo info, StreamingContext context)
			: base(info.GetString("Message"), ExtractException(info))
		{
			if (!info.ExistsKey("ErrorCode", true) || !info.ExistsKey("StatusCode", true))
			{
				throw new InvalidCastException("Missing ServiceException members");
			}

			var errorCodeEntry = info.GetSerializationEntry("ErrorCode", true);
			if (errorCodeEntry == null)
			{
				throw new ArgumentNullException("info");
			}

			ErrorCode = StringUtilities.GetInt(errorCodeEntry.Value.Value);
			var statusCodeEntry = info.GetSerializationEntry("StatusCode", true);
			if (statusCodeEntry == null)
			{
				throw new ArgumentNullException("info");
			}

			StatusCode = (HttpStatusCode) StringUtilities.GetInt(statusCodeEntry.Value.Value);
		}

		public int ErrorCode
		{
			get => _errorCode;
		    protected set
			{
				if (!IsErrorCodeValid(value))
				{
					throw new ArgumentException("Invalid error code");
				}

				_errorCode = value;
			}
		}

		public HttpStatusCode StatusCode { get; protected set; }

		private static Exception ExtractException(SerializationInfo info)
		{
			if (info == null || !info.ExistsKey("Exception", true))
			{
				return null;
			}

			var exceptionEntry = info.GetSerializationEntry("Exception");
			var exception = exceptionEntry.HasValue
				? (Exception)info.GetValue(exceptionEntry.Value.Name, typeof(Exception))
				: null;
			return exception;
		}

		public virtual bool IsErrorCodeValid(int errorCode)
		{
			return IsServiceExceptionErrorCodeValid(errorCode);
		}
		
		public static bool IsServiceExceptionErrorCodeValid(int errorCode)
		{
			return errorCode != 0;
		}
    }

	[Serializable]
    public class ModelStateException : ServiceException
    {   
        public object ModelStateErrorsObject { get; }

		public ModelStateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		    if (!info.ExistsKey("ModelStateErrorsObject") && !info.ExistsKey("ModelState"))
		    {
                throw new InvalidCastException("Missing ModelStateException members");
		    }

		    var item = info.GetSerializationEntry("ModelStateErrorsObject");
		    item = item ?? info.GetSerializationEntry("ModelState");
		    if (item == null)
		    {
                throw new InvalidCastException("Missing ModelStateErrorsObject members");
		    }

            var jObject = (JObject)item.Value.Value;
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jObject.ToString());
		    ModelStateErrorsObject = dictionary;
		}

        public ModelStateException(object modelStateErrorsObject, string message = "Model validation error",
            int errorCode = 5000, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(message, errorCode, statusCode)
        {
            ModelStateErrorsObject = modelStateErrorsObject;
        }

		public override bool IsErrorCodeValid(int errorCode)
		{
			return IsModelStateErrorCodeValid(errorCode);
		}

		public static bool IsModelStateErrorCodeValid(int errorCode)
		{
			return errorCode >= 5000 && errorCode < 10000;
		}
	}
}