using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
	public enum LoanQueryCriteria
	{
		Invalid = 0,
		UserId = 1,
		AccountId = 2,
		AccountPeriodId = 3
	}

	public enum PostResetPasswordEmailResponse
	{
		Unknown = 0,
		Ok = 1,
		Invalid = 2
	}

	public enum TokenActionValidationResult
	{
		Unknown = 0,
		Ok = 1,
		Invalid = 2
	}

	public class ResetPasswordValidationResult
	{
		public ResetPasswordValidationResult()
		{

		}

		public ResetPasswordValidationResult(
			TokenActionValidationResult actionValidationResult = TokenActionValidationResult.Unknown)
		{
			ActionValidationResult = actionValidationResult;
		}

		public ResetPasswordValidationResult(AppUser user) : this(TokenActionValidationResult.Ok)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
		}

		public TokenActionValidationResult ActionValidationResult { get; set; }
		public AppUser User { get; set; }
		public string Token { get; set; }
	}

	public class SpendActionAttributes
	{
		public enum ActionResult
		{
			Unknown = 0,
			Valid = 1,
			InvalidIsLoan = 2,
			InvalidIsTransfer = 3
		}

		public int SpendId { get; set; }
		public bool IsLoan { get; set; }
		public bool IsTransfer { get; set; }
		public IEnumerable<ResourceAccesLevels> AccessLevels => GetResourceAccessLevel();

		private IEnumerable<ResourceAccesLevels> GetResourceAccessLevel()
		{
			var levels = new List<ResourceAccesLevels>();
			if (!IsLoan && !IsTransfer)
			{
				levels.Add(ResourceAccesLevels.AnyOther);
			}

			if (IsLoan)
			{
				levels.Add(ResourceAccesLevels.LoanRelated);
			}

			if (IsTransfer)
			{
				levels.Add(ResourceAccesLevels.TransferRelated);
			}

			levels.Add(ResourceAccesLevels.Any);

			return levels;
		}
	}

	public class SpendActionResult : SpendActionAttributes
	{
		public ResourceActionNames Action { get; set; }
		public ActionResult Result { get; set; }
		public string ResultName => Result.ToString();
	}

	public class CurrencyAmount
	{
		public float Amount { get; set; }
		public string CurrencySymbol { get; set; }
        public string AsString => $"{CurrencySymbol}{Math.Round(Amount, 2):N}";
    }

	public class BankAccountSummary : AccountBasicInfo
	{
		public CurrencyAmount Balance { get; set; }
        public int? FinancialEntityId { get; set; }
        public string FinancialEntityName { get; set; }
    }

	public class AccountNotes
	{
		public string NoteTitle { get; set; }

		public string NoteContent { get; set; }
	}
}