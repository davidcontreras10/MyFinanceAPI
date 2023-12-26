using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Data
{
    public interface IUserRespository : ITransactional
    {
        Task<AppUser> GetUserByUserIdAsync(string userId);
		Task<IEnumerable<AppUser>> GetOwendUsersByUserIdAsync(string userId);
        Task<AppUser> GetUserByUsernameAsync(string username);
        Task<LoginResult> AttemptToLoginAsync(string username, string encryptedPassword);
		Task<bool> SetPasswordAsync(string userId, string encryptedPassword);
        Task<bool> UpdateUserAsync(ClientEditUser user);
        Task<string> AddUserAsync(ClientAddUser user);
    }
}
