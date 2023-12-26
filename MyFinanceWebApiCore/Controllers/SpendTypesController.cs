using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SpendTypesController : BaseApiController
	{
		#region Attributes

		private readonly ISpendTypeService _spendTypeService;

		#endregion

		#region Constructor

		public SpendTypesController(ISpendTypeService spendTypeService)
		{
			_spendTypeService = spendTypeService;
		}

		#endregion

		#region Routes

		[HttpGet]
		public Task<IEnumerable<SpendTypeViewModel>> GetSpendTypes(bool includeAll = true)
		{
			var userId = GetUserId();
			var result = _spendTypeService.GeSpendTypesAsync(userId, includeAll);
			return result;
		}

		[HttpDelete]
		public async Task DeleteSpendType([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			await _spendTypeService.DeleteSpendTypeAsync(userId, clientSpendTypeId.SpendTypeId);
		}

		[HttpPost]
		public async Task<IEnumerable<int>> AddSpendType(ClientAddSpendType spendType)
		{
			if (spendType == null)
			{
				throw new ArgumentNullException("spendType");
			}

			var userId = GetUserId();
			spendType.SpendTypeId = 0;
			var result = await _spendTypeService.AddEditSpendTypesAsync(userId, spendType);
			return result;
		}

		[HttpPatch]
		public async Task<IEnumerable<int>> EditSpendType(ClientEditSpendType spendType)
		{
			if (spendType == null)
			{
				throw new ArgumentNullException("spendType");
			}

			if (spendType.SpendTypeId < 1)
			{
				throw new ArgumentException("Id cannot be zero or less", "spendType");
			}

			var userId = GetUserId();
			var result =  await _spendTypeService.AddEditSpendTypesAsync(userId, spendType);
			return result;
		}

		[Route("user")]
		[HttpPost]
		public async Task<IEnumerable<int>> AddSpendTypeUser([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.AddSpendTypeUserAsync(userId, clientSpendTypeId.SpendTypeId);
			return result;
		}

		[Route("user")]
		[HttpDelete]
		public async Task<IEnumerable<int>> DeleteSpendTypeUser([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.DeleteSpendTypeUserAsync(userId, clientSpendTypeId.SpendTypeId);
			return result;
		}

		#endregion
	}
}
