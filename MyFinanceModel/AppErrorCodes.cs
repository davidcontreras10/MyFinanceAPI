using System.Net;

namespace MyFinanceModel
{
	public record class AppErrorCode(int Code, string Message, HttpStatusCode StatusCode = HttpStatusCode.InternalServerError);

	public class AppErrorCodes
	{
		public static AppErrorCode DeleteTrxWithBankTrx = new(2001, "Unable to delete transaction with bank transactions associated", HttpStatusCode.BadRequest);
		public static AppErrorCode DeleteTrxWithErrors = new(2000, "Unable to delete transaction with errors", HttpStatusCode.BadRequest);
	}
}
