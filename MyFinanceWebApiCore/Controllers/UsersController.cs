using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Attributes;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
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
		private readonly IUserAuthorizeService _userAuthorizationService;

		public UsersController(IUsersService usersService, IUserAuthorizeService userAuthorizationService)
		{
			_usersService = usersService;
			_userAuthorizationService = userAuthorizationService;
		}

		#region Web Methods

		[ResourceActionRequired(ApplicationResources.Users, ResourceActionNames.View)]
		[HttpGet]
		public async Task<ActionResult<AppUser>> GetUserById([FromQuery] string targetUserId)
		{
			if (!await _userAuthorizationService.IsAuthorizedAsync(GetUserId(), new[] { targetUserId }, new[] { ResourceActionNames.View }))
				return StatusCode(403);
			var appUser = await _usersService.GetUserAsync(targetUserId);
			return appUser;
		}

		[ResourceActionRequired(ApplicationResources.Users, ResourceActionNames.Edit)]
		[HttpPatch]
		public async Task<ActionResult<bool>> UpdateUser([FromQuery] string targetUserId, [FromBody] ClientEditUser editUser)
		{
			var userId = GetUserId();
			if (!await _userAuthorizationService.IsAuthorizedAsync(userId, new[] { targetUserId }, new[] { ResourceActionNames.Edit }))
				return StatusCode(403);
			if (editUser == null)
				return BadRequest();

			editUser.UserId = targetUserId;
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
		public async Task<ActionResult<bool>> SetUserPassword([FromBody] SetPassword setPassword)
		{
			if (!await _userAuthorizationService.IsAuthorizedAsync(GetUserId(), new[] { setPassword?.UserId }, new[] { ResourceActionNames.EditSensitive }))
				return StatusCode(403);
			return await _usersService.SetPasswordAsync(setPassword.UserId, setPassword.NewPassword);
		}

		#endregion
	}
}
