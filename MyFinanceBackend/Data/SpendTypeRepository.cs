using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using DataAccess;
using DContre.MyFinance.StUtilities;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public class SpendTypeRepository : SqlServerBaseService, ISpendTypeRepository
	{
		
        #region Constructor

		public SpendTypeRepository(IConnectionConfig config)
            : base(config)
        {
        }

        #endregion

		#region methods

		public Task<IEnumerable<int>> DeleteSpendTypeUserAsync(string userId, int spendTypeId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, spendTypeId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_USER_DELETE, parameters);
			if (dataSet == null || dataSet.Tables.Count < 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				IEnumerable<int> emptyResult = Array.Empty<int>();
				return Task.FromResult(emptyResult);
			}

			var result =
				dataSet.Tables[0].Rows.Cast<DataRow>().Select(dataRow => dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID));
			return Task.FromResult(result);
		}

		public Task<IEnumerable<int>> AddSpendTypeUserAsync(string userId, int spendTypeId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, spendTypeId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_USER_ADD, parameters);
			if (dataSet == null || dataSet.Tables.Count < 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				IEnumerable<int> emptyResult = Array.Empty<int>();
				return Task.FromResult(emptyResult);
			}

			var result =
				dataSet.Tables[0].Rows.Cast<DataRow>().Select(dataRow => dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID));
			return Task.FromResult(result);
		}

		public Task<IEnumerable<int>> AddEditSpendTypesAsync(string userId, ClientSpendType clientSpendType)
		{
			if (clientSpendType == null)
			{
				throw new ArgumentNullException("clientSpendType");
			}

			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, clientSpendType.SpendTypeId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_NAME, clientSpendType.SpendTypeName),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_DESCRIPTION, clientSpendType.SpendTypeDescription),
				new SqlParameter(DatabaseConstants.PAR_IS_SELECTED, clientSpendType.IsSelected)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_ADD_EDIT, parameters);
			if (dataSet == null || dataSet.Tables.Count < 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				IEnumerable<int> emptyResult = Array.Empty<int>();
				return Task.FromResult(emptyResult);
			}

			var result =
				dataSet.Tables[0].Rows.Cast<DataRow>().Select(dataRow => dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID));
			return Task.FromResult(result);
		}

		public Task<IEnumerable<SpendTypeViewModel>> GetSpendTypeByAccountViewModelsAsync(string userId, int? accountId)
		{
			var parameters = new[]
            {
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
                new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId)
            };

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_BY_ACCOUNT_LIST, parameters);
			if(dataSet == null)
			{
				IEnumerable<SpendTypeViewModel> emptyResult = Array.Empty<SpendTypeViewModel>(); 
				return Task.FromResult(emptyResult);
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateSpendTypeViewModel);
			return Task.FromResult(result);
		}

		public Task<IEnumerable<SpendTypeViewModel>> GetSpendTypesAsync(string userId, bool includeAll = true)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_INCLUDE_ALL, includeAll),
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_LIST, parameters);
			if(dataSet == null)
			{
				IEnumerable<SpendTypeViewModel> empty = Array.Empty<SpendTypeViewModel>();
				return Task.FromResult(empty);
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateSpendTypeViewModel);
			return Task.FromResult(result);
		}

        public Task DeleteSpendTypeAsync(string userId, int spendTypeId)
        {
            var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, spendTypeId),
			};

            ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_TYPE_DELETE, parameters);
			return Task.CompletedTask;
        }

		#endregion
	}
}
