using MyFinanceBackend.Services;
using System.Security.Claims;
using System.Text;
using System;
using MyFinanceModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using MyFinanceWebApiCore.Models;
using Microsoft.Extensions.Options;
using MyFinanceWebApiCore.Config;

namespace MyFinanceWebApiCore.Services
{
	public interface IAuthenticationService
	{
		Task<AuthToken> AuthenticateAsync(AuthenticateRequest request);
	}

	public class AuthenticationService : IAuthenticationService
	{
		private readonly IUsersService _usersService;
		private readonly AppSettings.AuthConfig _authConfig;

		public AuthenticationService(IUsersService usersService, IOptions<AppSettings.AuthConfig> authConfigSettings)
		{
			_usersService = usersService;
			_authConfig = authConfigSettings.Value;
		}

		public async Task<AuthToken> AuthenticateAsync(AuthenticateRequest request)
		{
			var appUser = await _usersService.AttemptToLoginAsync(request.Username, request.Password);
			if (appUser.User == null)
			{
				throw new UnauthorizedAccessException(appUser.ResultMessage);
			}

			return GenerateJwtToken(appUser);
		}

		private AuthToken GenerateJwtToken(LoginResult user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			if (_authConfig.Secret == null)
			{
				throw new Exception($"Expected: {nameof(_authConfig.Secret)}");
			}

			var role = user.User.Username == "AzureAdmin" ? "Admin" : string.Empty;
			var key = Encoding.ASCII.GetBytes(_authConfig.Secret);
			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.User.UserId.ToString()),
				new Claim(ClaimTypes.Name, user.User.Username),
				new Claim(ClaimTypes.Role, role),
				new Claim(ClaimTypes.UserData,user.User.UserId.ToString())
			};
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.Add(_authConfig.TokenExpiresIn),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			var stringToken = tokenHandler.WriteToken(token);
			return new AuthToken
			{
				AccessToken = stringToken,
				ExpiresIn = (int)_authConfig.TokenExpiresIn.TotalSeconds - 1,
				RefreshToken = string.Empty,
				TokenType = "bearer"
			};
		}
	}
}
