using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFSpendTypeRepository : BaseEFRepository, ISpendTypeRepository
	{
		public EFSpendTypeRepository(MyFinanceContext context) : base(context)
		{
		}

		public async Task<IEnumerable<int>> AddEditSpendTypesAsync(string userId, ClientSpendType clientSpendType)
		{
			var currentSpendType = clientSpendType.SpendTypeId > 0
				? await Context.SpendType.FirstOrDefaultAsync(x => x.SpendTypeId == clientSpendType.SpendTypeId)
				: null;
			if (currentSpendType == null)
			{
				currentSpendType = new SpendType
				{
					Name = clientSpendType.SpendTypeName,
					Description = clientSpendType.SpendTypeDescription
				};

				await Context.SpendType.AddAsync(currentSpendType);
				if (clientSpendType.IsSelected)
				{
					var userSpendType = new UserSpendType
					{
						UserId = new Guid(userId),
						SpendType = currentSpendType
					};

					await Context.UserSpendType.AddAsync(userSpendType);
				}
			}
			else
			{
				currentSpendType.Name = clientSpendType.SpendTypeName;
				currentSpendType.Description = clientSpendType.SpendTypeDescription;
			}

			await Context.SaveChangesAsync();
			return new[] { currentSpendType.SpendTypeId };
		}

		public async Task<IEnumerable<int>> AddSpendTypeUserAsync(string userId, int spendTypeId)
		{
			var userGuid = new Guid(userId);
			var existsRecord = await Context.UserSpendType.AnyAsync(x => x.UserId == userGuid && x.SpendTypeId == spendTypeId);
			if (!existsRecord)
			{
				var userSpendType = new UserSpendType
				{
					SpendTypeId = spendTypeId,
					UserId = userGuid
				};
				await Context.UserSpendType.AddAsync(userSpendType);
				await Context.SaveChangesAsync();
			}

			return new[] { spendTypeId };
		}

		public async Task DeleteSpendTypeAsync(string userId, int spendTypeId)
		{
			var userSpendTypes = await Context.UserSpendType.Where(x => x.SpendTypeId == spendTypeId).ToListAsync();
			if (userSpendTypes.Any())
			{
				Context.RemoveRange(userSpendTypes);
			}

			var spendTypes = await Context.SpendType.Where(x => x.SpendTypeId == spendTypeId).ToListAsync();
			if (spendTypes.Any())
			{
				Context.SpendType.RemoveRange(spendTypes);
			}

			await Context.SaveChangesAsync();
		}

		public async Task<IEnumerable<int>> DeleteSpendTypeUserAsync(string userId, int spendTypeId)
		{
			var userSpendTypes = await Context.UserSpendType
				.Where(x => x.UserId == new Guid(userId) && x.SpendTypeId == spendTypeId)
				.ToListAsync();

			if (userSpendTypes.Any())
			{
				Context.RemoveRange(userSpendTypes);
				await Context.SaveChangesAsync();
			}

			return new[] { spendTypeId };
		}

		public async Task<IEnumerable<SpendTypeViewModel>> GetSpendTypeByAccountViewModelsAsync(string userId, int? accountId)
		{
			var userSpendTypes = await Context.UserSpendType.AsNoTracking()
				.Where(x => x.UserId == new Guid(userId))
				.Include(x => x.SpendType)
				.ToListAsync();
			int? accountSpendTypeId = null;
			if (accountId > 0)
			{
				accountSpendTypeId = (await Context.Account.Where(x => x.AccountId == accountId).FirstOrDefaultAsync())?.DefaultSpendTypeId;
			}

			var results = userSpendTypes.Select(x => new SpendTypeViewModel
			{
				SpendTypeId = x.SpendTypeId,
				Description = x.SpendType.Description,
				IsDefault = x.SpendTypeId == accountSpendTypeId,
				SpendTypeName = x.SpendType.Name
			});

			return results;
		}

		public async Task<IEnumerable<SpendTypeViewModel>> GetSpendTypesAsync(string userId, bool includeAll = true)
		{
			var userGuid = new Guid(userId);
			IReadOnlyCollection<LocalUserSpendType> spendTypes;
			if (includeAll)
			{
				spendTypes = await Context.SpendType.AsNoTracking()
					.Select(x => new LocalUserSpendType
					{
						UserId = null,
						SpendType = x,
					})
					.ToListAsync();
			}
			else
			{
				spendTypes = await Context.UserSpendType.AsNoTracking()
					.Include(x => x.SpendType)
					.Where(x => x.UserId == userGuid)
					.Select(x => new LocalUserSpendType
					{
						SpendType = x.SpendType,
						UserId = x.UserId
					})
					.ToListAsync();
			}

			var spendTypeViewModels = spendTypes
				.Select(x => new SpendTypeViewModel
				{
					Description = x.SpendType.Description,
					IsDefault = includeAll && userGuid == x.UserId,
					SpendTypeId = x.SpendType.SpendTypeId,
					SpendTypeName = x.SpendType.Name
				}).ToList();
			return spendTypeViewModels;

		}

		private class LocalUserSpendType
		{
			public Guid? UserId { get; set; }
			public SpendType SpendType { get; set; }
		}
	}
}
