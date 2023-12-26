using EFDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFUserRepository : BaseEFRepository, IUserRespository
	{
		public EFUserRepository(MyFinanceContext context) : base(context)
		{
		}

		public Task<string> AddUserAsync(ClientAddUser user)
		{
			throw new NotImplementedException();
		}

		public async Task<LoginResult> AttemptToLoginAsync(string username, string encryptedPassword)
		{
			var appUser = await Context.AppUser.AsNoTracking()
				.Where(x => x.Username == username)
				.SingleAsync();
			if (appUser == null)
			{
				return new LoginResult
				{
					IsAuthenticated = false,
					ResetPassword = false,
					ResultCode = "3",
					ResultMessage = "User Incorrect"
				};
			}

			if (appUser.Password == encryptedPassword)
			{
				return new LoginResult
				{
					IsAuthenticated = true,
					ResetPassword = false,
					ResultCode = "1",
					ResultMessage = "Login Succesfully",
					User = ToAppUser(appUser)
				};
			}

			return new LoginResult
			{
				IsAuthenticated = false,
				ResetPassword = false,
				ResultCode = "2",
				ResultMessage = "Password Incorrect"
			};
		}

		public void BeginTransaction()
		{
			throw new NotImplementedException();
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public async Task<IEnumerable<MyFinanceModel.AppUser>> GetOwendUsersByUserIdAsync(string userId)
		{
			var userGuid = new Guid(userId);
			var results = await Context.AppUserOwner.AsNoTracking()
				.Include(x => x.User)
				.Where(x => x.OwnerUserId == userGuid)
				.Select(x => ToAppUser(x.User))
				.ToListAsync();
			return results;
		}

		public async Task<MyFinanceModel.AppUser> GetUserByUserIdAsync(string userId)
		{
			return await Context.AppUser.AsNoTracking()
				.Where(x => x.UserId == new Guid(userId))
				.Select(x => ToAppUser(x))
				.FirstOrDefaultAsync();
		}

		public async Task<MyFinanceModel.AppUser> GetUserByUsernameAsync(string username)
		{
			return await Context.AppUser.AsNoTracking()
				.Where(x => x.Username == username)
				.Select(x => ToAppUser(x))
				.FirstOrDefaultAsync();
		}

		public void RollbackTransaction()
		{
			throw new NotImplementedException();
		}

		public Task<bool> SetPasswordAsync(string userId, string encryptedPassword)
		{
			throw new NotImplementedException();
		}

		public Task<bool> UpdateUserAsync(ClientEditUser user)
		{
			throw new NotImplementedException();
		}

		private static MyFinanceModel.AppUser ToAppUser(Models.AppUser appUser)
		{
			return new MyFinanceModel.AppUser
			{
				Name = appUser.Name,
				PrimaryEmail = appUser.PrimaryEmail,
				UserId = appUser.UserId,
				Username = appUser.Username
			};
		}
	}
}
