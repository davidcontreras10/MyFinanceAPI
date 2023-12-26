using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceBackend.Attributes;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
	public class AuthorizationService : IAuthorizationService
	{
		#region Attributes

		private readonly IUserAuthorizeService _userAuthorizeService;

		#endregion

		#region Constructor

		public AuthorizationService(IUserAuthorizeService userAuthorizeService)
		{
			_userAuthorizeService = userAuthorizeService;
		}

		#endregion

		public async Task<bool> IsAuthorizedAsync(string authenticatedUserId, IEnumerable<string> targetUserIds,
			ResourceActionRequiredAttribute resourceActionRequiredAttribute)
		{
			if (string.IsNullOrEmpty(authenticatedUserId))
			{
				throw new ArgumentNullException(nameof(authenticatedUserId));
			}

			if (resourceActionRequiredAttribute == null)
			{
				return true;
			}

			if (resourceActionRequiredAttribute.Resource == ApplicationResources.Users)
			{
				return await _userAuthorizeService.IsAuthorizedAsync(authenticatedUserId, targetUserIds,
					resourceActionRequiredAttribute.Actions);
			}

			throw new NotImplementedException();
		}
	}
}
