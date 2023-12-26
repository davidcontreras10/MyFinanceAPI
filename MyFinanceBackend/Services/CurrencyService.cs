using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MyFinanceBackend.Models;
using MyFinanceModel;
using MyFinanceModel.WebMethodsModel;
using WebApiBaseConsumer;

namespace MyFinanceBackend.Services
{
	public interface ICurrencyService
	{
		Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isPurchase);
		Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds, DateTime dateTime);
	}

	public class CurrencyService : WebApiBaseService, ICurrencyService
	{
		#region Attributes

		private readonly string _serviceUrl;
		private const string CURRENCY_SERVICE_NAME = "Convert";
		private const string CONVERT_METHOD_BY_LIST_NAME = "ExchangeRateResultByMethodIds";
		private const string CONVERT_METHOD_NAME = "ExchangeRateResultByMethodId";

		protected override string ControllerName => CURRENCY_SERVICE_NAME;

		#endregion

		#region Constructors

		public CurrencyService(IHttpClientFactory httpClientFactory, IBackendSettings backendSettings) : base(httpClientFactory)
		{
			_serviceUrl = backendSettings.CurrencyServiceUrl;
		}

		#endregion

		#region Public Methods

		public async Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isPurchase)
		{
			return await GetExchangeRateResultServiceAsync(methodId, dateTime, isPurchase);
		}

		public async Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds, DateTime dateTime)
		{
			return methodIds == null || !methodIds.Any()
				? new List<ExchangeRateResult>()
				: await GetExchangeRateResultServiceAsync(methodIds, dateTime);
		}

		#endregion

		#region Private Methods

		private async Task<ExchangeRateResult> GetExchangeRateResultServiceAsync(int methodId, DateTime dateTime, bool isPurchase)
		{
			var requestDateTime = dateTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
			var parameters = new Dictionary<string, object>
			{
				{ "methodId", methodId },
				{ "dateTime", requestDateTime },
				{ "isPurchase", isPurchase}
			};
			var methodUrl = CreateMethodUrl(CONVERT_METHOD_NAME, parameters);
			var request = new WebApiRequest(methodUrl, HttpMethod.Get);
			return await GetResponseAsAsync<ExchangeRateResult>(request);
		}

		private async Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultServiceAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds, DateTime dateTime)
		{
			var methodUrl = CreateMethodUrl(CONVERT_METHOD_BY_LIST_NAME);
			var exchangeRateResultModel = new ExchangeRateResultModel
			{
				DateTime = dateTime,
				MethodIds = methodIds
			};
			var request = new WebApiRequest(methodUrl, HttpMethod.Post)
			{
				Model = exchangeRateResultModel
			};

			return await GetResponseAsAsync<IEnumerable<ExchangeRateResult>>(request);
		}

		protected override string GetApiBaseDomain()
		{
			return _serviceUrl;
		}


		#endregion
	}
}
