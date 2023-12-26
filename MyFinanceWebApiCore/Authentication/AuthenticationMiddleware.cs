using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyFinanceWebApiCore.Config;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System;
using MyFinanceBackend.Services;
using System.Linq;

namespace MyFinanceWebApiCore.Authentication
{
	public class AuthenticationMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly AppSettings.AuthConfig _authConfig;

		public AuthenticationMiddleware(RequestDelegate next, IOptions<AppSettings.AuthConfig> authConfig)
		{
			_next = next;
			_authConfig = authConfig.Value;
		}

		public async Task Invoke(HttpContext context, IUsersService userService)
		{
			var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
			if (token != null)
			{
				AttachUserToContext(context, userService, token);
			}

			await _next(context);
		}

		private void AttachUserToContext(HttpContext context, IUsersService userService, string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				if (_authConfig?.Secret == null)
				{
					throw new Exception($"Expected: {nameof(_authConfig.Secret)}");
				}

				var key = Encoding.ASCII.GetBytes(_authConfig.Secret);
				var pricipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					// set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
					ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);
								
				context.User = pricipal;
			}
			catch
			{
				throw new UnauthorizedAccessException();
			}
		}
	}
}
