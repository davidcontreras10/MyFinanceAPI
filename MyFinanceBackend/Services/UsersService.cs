using System;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.Utils;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using Serilog;

namespace MyFinanceBackend.Services
{
	public class UsersService : IUsersService
	{
		#region Attributes

		private readonly IUserRespository _userRespository;
		private readonly IEmailService _emailService;
		private readonly ILogger _logger;

		#endregion

		#region Constructor

		public UsersService(IUserRespository userRespository, IEmailService emailService, ILogger logger)
		{
			_userRespository = userRespository;
			_emailService = emailService;
			_logger = logger;
		}

		#endregion

		#region Public Methods

		public async Task<AppUser> GetUserAsync(string userId)
		{
			var user = await _userRespository.GetUserByUserIdAsync(userId);
			return user;
		}

		public async Task<LoginResult> AttemptToLoginAsync(string username, string password)
		{
			var encryptedPassword = PasswordUtils.EncryptText(password);
			var result = await _userRespository.AttemptToLoginAsync(username, encryptedPassword);
			return result;
		}

		public async Task<bool> SetPasswordAsync(string userId, string newPassword)
		{
			var encryptedPassword = PasswordUtils.EncryptText(newPassword);
			var result = await _userRespository.SetPasswordAsync(userId, encryptedPassword);
			return result;
		}

		public async Task<PostResetPasswordEmailResponse> SendResetPasswordEmailAsync(ClientResetPasswordEmailRequest request)
		{
			var userInfo = await _userRespository.GetUserByUsernameAsync(request.Username);
			if (userInfo == null)
			{
				throw new UnauthorizedAccessException();
			}

			var resetPasswordLink = CreateResetPasswordLink(request.HostUrl, userInfo.UserId.ToString());
			var body = GetResetPasswordEmailTemplate(resetPasswordLink, userInfo.Username);
			var result = await _emailService.SendEmailAsync(userInfo.PrimaryEmail, "My Finance Reset Password", body, true);
			return result ? PostResetPasswordEmailResponse.Ok : PostResetPasswordEmailResponse.Unknown;
		}

		public async Task<bool> ValidResetPasswordEmailRequestAsync(ClientResetPasswordEmailRequest request)
		{
			if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request.Email))
			{
				return false;
			}

			var user = await _userRespository.GetUserByUsernameAsync(request.Username);
			return !string.IsNullOrEmpty(user?.PrimaryEmail) &&
				   (user.Username == request.Username && user.PrimaryEmail == request.Email);
		}

		public async Task<ResetPasswordValidationResult> ValidateResetPasswordActionResultAsync(string actionLink)
		{
			if (string.IsNullOrEmpty(actionLink))
			{
				return new ResetPasswordValidationResult();
			}

			var token = CreateClientResetPasswordToken(actionLink);
			if (token == null)
			{
				return new ResetPasswordValidationResult();
			}

			if (token.DateTimeIssued.Add(token.ExpireTime) <= DateTime.Now)
			{
				return new ResetPasswordValidationResult(TokenActionValidationResult.Invalid);
			}

			var user = await _userRespository.GetUserByUserIdAsync(token.UserId);
			var newToken = new UserToken
			{
				ExpireTime = AppSettings.ResetPasswordTokenExpireTime,
				UserId = token.UserId,
				DateTimeIssued = DateTime.Now
			};

			return new ResetPasswordValidationResult(user)
			{
				Token = newToken.SerializeToken()
			};
		}

		public async Task<TokenActionValidationResult> UpdateUserPasswordAsync(ClientNewPasswordRequest passwordResetRequest)
		{
			UserToken token;
			token = TokenUtils.TryDeserializeToken(passwordResetRequest.Token, out token) ? token : null;
			if (token == null)
			{
				return TokenActionValidationResult.Unknown;
			}

			if (token.HasExpired())
			{
				return TokenActionValidationResult.Invalid;
			}

			var result = await SetPasswordAsync(token.UserId, passwordResetRequest.Password);
			return result ? TokenActionValidationResult.Ok : TokenActionValidationResult.Unknown;
		}

		public async Task<bool> UpdateUserAsync(string userId, ClientEditUser user)
		{
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}

			return await _userRespository.UpdateUserAsync(user);
		}

		public Task<bool> AddUserAsync(ClientAddUser user, string userId)
		{
			if (user == null)
			{
				throw new ArgumentNullException(nameof(user));
			}

			user.CreatedByUserId = userId;
			throw new NotImplementedException();
		}

		#endregion

		#region Private Methods



		private static UserToken CreateClientResetPasswordToken(string action)
		{
			UserToken token;
			token = TokenUtils.TryDeserializeToken(action, out token) ? token : null;
			return token;
		}

		private string CreateResetPasswordLink(string hostUrl, string userId)
		{
			var token = new UserToken
			{
				UserId = userId,
				DateTimeIssued = DateTime.Now,
				ExpireTime = TimeSpan.FromMinutes(5)
			};

			var encodedEncryptToken = token.SerializeToken();
			var result = $"{hostUrl}?actionLink={encodedEncryptToken}";
			return result;
		}

		private string GetResetPasswordEmailTemplate(string link, string user, int expireTimeHours = 2)
		{
			var fileContent = Properties.Resources.ResetPassword;
			fileContent = string.Format(fileContent, user, link, expireTimeHours);
			return fileContent;
		}

		//private bool CanUserUpdatePassword(string userId, string targetUser)
		//{
		//       var userGuid = new Guid(userId);
		//       var targetUserGuid = new Guid(targetUser);
		//       return userGuid == targetUserGuid;
		//}

		#endregion
	}
}