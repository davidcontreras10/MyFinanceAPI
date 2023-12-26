using Newtonsoft.Json;

namespace MyFinanceWebApiCore.Models
{
	public class AuthToken
	{
		public string AccessToken { get; set; }
		public int ExpiresIn { get; set; }
		public string RefreshToken { get; set; }
		public string TokenType { get; set; }
	}
}
