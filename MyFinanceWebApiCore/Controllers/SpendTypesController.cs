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

		[Obsolete]
		[HttpDelete]
		public async Task DeleteSpendType([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			await _spendTypeService.DeleteSpendTypeAsync(userId, clientSpendTypeId.SpendTypeId);
		}

		[Route("{spendTypeId}")]
		[HttpDelete]
		public async Task DeleteSpendType([FromRoute]int spendTypeId)
		{
			var userId = GetUserId();
			await _spendTypeService.DeleteSpendTypeAsync(userId, spendTypeId);
		}

		[HttpPost]
		public async Task<ActionResult> AddSpendType([FromBody]ClientAddSpendType spendType, [FromQuery]bool entireResponse = false)
		{
			if (spendType == null)
			{
				throw new ArgumentNullException(nameof(spendType));
			}

			var userId = GetUserId();
			spendType.SpendTypeId = 0;
			var result = await _spendTypeService.AddEditSpendTypesAsync(userId, spendType);
			if (entireResponse)
			{
				return Ok(result);
			}
			else
			{
				var response = new[]
				{
					result.SpendTypeId
				};
				return Ok(response);
			}
		}

		[HttpPatch]
		public async Task<ActionResult> EditSpendType(ClientEditSpendType spendType, bool entireResponse = false)
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
			var result = await _spendTypeService.AddEditSpendTypesAsync(userId, spendType);
			if (entireResponse)
			{
				return Ok(result);
			}
			else
			{
				var response = new[]
				{
					result.SpendTypeId
				};
				return Ok(response);
			}
		}

		[Obsolete]
		[Route("user")]
		[HttpPost]
		public async Task<IEnumerable<int>> AddSpendTypeUser([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.AddSpendTypeUserAsync(userId, clientSpendTypeId.SpendTypeId);
			return result;
		}

		[Obsolete]
		[Route("user")]
		[HttpDelete]
		public async Task<IEnumerable<int>> DeleteSpendTypeUser([FromBody] ClientSpendTypeId clientSpendTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.DeleteSpendTypeUserAsync(userId, clientSpendTypeId.SpendTypeId);
			return result;
		}

		[Route("{trxTypeId}/user")]
		[HttpDelete]
		public async Task<IEnumerable<int>> DeleteSpendTypeUser([FromRoute] int trxTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.DeleteSpendTypeUserAsync(userId, trxTypeId);
			return result;
		}


		[Route("{trxTypeId}/user")]
		[HttpPost]
		public async Task<IEnumerable<int>> AddSpendTypeUser([FromRoute] int trxTypeId)
		{
			var userId = GetUserId();
			var result = await _spendTypeService.AddSpendTypeUserAsync(userId, trxTypeId);
			return result;
		}
		#endregion
	}
}
