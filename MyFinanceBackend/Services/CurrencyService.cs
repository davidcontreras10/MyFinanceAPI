using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MyFinanceBackend.Models;
using MyFinanceModel;
using MyFinanceModel.Records;
using MyFinanceModel.WebMethodsModel;
using WebApiBaseConsumer;

namespace MyFinanceBackend.Services
{
	public interface ICurrencyService
	{
		Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isPurchase);
		Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds);
		Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<MethodParam> methodParams);
	}

	public class CurrencyService(IHttpClientFactory httpClientFactory, IBackendSettings backendSettings) : WebApiBaseService(httpClientFactory), ICurrencyService
	{
		#region Attributes

		private readonly string _serviceUrl = backendSettings.CurrencyServiceUrl;
		private const string CURRENCY_SERVICE_NAME = "Convert";
		private const string CONVERT_METHOD_BY_LIST_NAME = "ExchangeRateResultByMethodIds";
		private const string CONVERT_METHOD_NAME = "ExchangeRateResultByMethodId";

		protected override string ControllerName => CURRENCY_SERVICE_NAME;

		#endregion

		#region Public Methods

		public async Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isPurchase)
		{
			return await GetExchangeRateResultServiceAsync(methodId, dateTime, isPurchase);
		}

		public async Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<MethodParam> methodParams)
		{
			var methodIds = methodParams.Select(x => new ExchangeRateResultModel.MethodParam
			{
				Id = x.Id,
				IsPurchase = x.IsPurchase,
				DateTime = x.DateTime
			});
			return methodIds == null || !methodIds.Any()
				? []
				: await GetExchangeRateResultServiceAsync(methodIds);
		}

		public async Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds)
		{
			return methodIds == null || !methodIds.Any()
				? Array.Empty<ExchangeRateResult>()
				: await GetExchangeRateResultServiceAsync(methodIds);
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

		private async Task<IEnumerable<ExchangeRateResult>> GetExchangeRateResultServiceAsync(IEnumerable<ExchangeRateResultModel.MethodParam> methodIds)
		{
			var methodUrl = CreateMethodUrl(CONVERT_METHOD_BY_LIST_NAME);
			var exchangeRateResultModel = new ExchangeRateResultModel
			{
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
