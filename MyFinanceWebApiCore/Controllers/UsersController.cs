using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Attributes;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : BaseApiController
	{
		private readonly IUsersService _usersService;

		public UsersController(IUsersService usersService)
		{
			_usersService = usersService;
		}

		#region Web Methods

		[ResourceActionRequired(ApplicationResources.Users, ResourceActionNames.View)]
		[HttpGet]
		public async Task<AppUser> GetUserById([FromQuery] string targetUserId)
		{
			var appUser = await _usersService.GetUserAsync(targetUserId);
			return appUser;
		}

		[ResourceActionRequired(ApplicationResources.Users, ResourceActionNames.Edit)]
		[HttpPatch]
		public async Task<bool> UpdateUser([FromQuery] string targetUserId, [FromBody] ClientEditUser editUser)
		{
			if (string.IsNullOrEmpty(targetUserId))
			{
				throw new ArgumentNullException(nameof(targetUserId));
			}

			editUser.UserId = targetUserId;
			var userId = GetUserId();
			var result = await _usersService.UpdateUserAsync(userId, editUser);
			return result;
		}

		[AllowAnonymous]
		[HttpPut]
		[Route("ResetPassword")]
		public async Task<TokenActionValidationResult> UpdateUserPassword(ClientNewPasswordRequest passwordResetRequest)
		{
			var result = await _usersService.UpdateUserPasswordAsync(passwordResetRequest);
			return result;
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("ResetPassword")]
		public async Task<ResetPasswordValidationResult> GetResetPasswordValidationResult(string actionLink)
		{
			var result = await _usersService.ValidateResetPasswordActionResultAsync(actionLink);
			return result;
		}

		[AllowAnonymous]
		[HttpPost]
		[Route("ResetPasswordEmail")]
		public async Task<PostResetPasswordEmailResponse> SendResetPasswordEmail([FromBody] ClientResetPasswordEmailRequest request)
		{
			var valid = await _usersService.ValidResetPasswordEmailRequestAsync(request);
			if (!valid)
			{
				return PostResetPasswordEmailResponse.Invalid;
			}

			var result = await _usersService.SendResetPasswordEmailAsync(request);
			return result;
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("InSession")]
		public bool InSession()
		{
			return Request.HttpContext.User.Identity.IsAuthenticated;
		}

		[AllowAnonymous]
		[HttpGet]
		[Route("ResultLoginAttempt")]
		public async Task<LoginResult> ResultLoginAttempt(string username, string password)
		{
			return await _usersService.AttemptToLoginAsync(username, password);
		}

		[ResourceActionRequired(ApplicationResources.Users, ResourceActionNames.EditSensitive)]
		[Route("Password")]
		[HttpPatch]
		public async Task<bool> SetUserPassword([FromBody] SetPassword setPassword)
		{
			return await _usersService.SetPasswordAsync(setPassword.UserId, setPassword.NewPassword);
		}

		#endregion
	}
}
