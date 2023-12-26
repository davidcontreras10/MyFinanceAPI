using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
    public interface IUsersService
    {
        Task<AppUser> GetUserAsync(string userId);

        Task<LoginResult> AttemptToLoginAsync(string username, string password);

        Task<bool> SetPasswordAsync(string userId, string newPassword);
        Task<PostResetPasswordEmailResponse> SendResetPasswordEmailAsync(ClientResetPasswordEmailRequest request);
        Task<bool> ValidResetPasswordEmailRequestAsync(ClientResetPasswordEmailRequest request);
        Task<ResetPasswordValidationResult> ValidateResetPasswordActionResultAsync(string actionLink);
        Task<TokenActionValidationResult> UpdateUserPasswordAsync(ClientNewPasswordRequest passwordResetRequest);
        Task<bool> UpdateUserAsync(string userId, ClientEditUser user);
        Task<bool> AddUserAsync(ClientAddUser user, string userId);
    }
}