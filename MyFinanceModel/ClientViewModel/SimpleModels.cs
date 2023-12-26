using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace MyFinanceModel.ClientViewModel
{
    public class ClientLoanSpendViewModel : ClientBasicAddSpend
    {
        public int LoanRecordId { get; set; }
		public bool FullPayment { get; set; }

		public ClientLoanSpendViewModel GetCopy()
        {
			var copy = new ClientLoanSpendViewModel
			{
				Amount = Amount,
				AmountDenominator = AmountDenominator,
				AmountNumerator = AmountNumerator,
				AmountType = AmountType,
				AmountTypeId = AmountTypeId,
				CurrencyId = CurrencyId,
				Description = Description,
				IsPending = IsPending,
				LoanRecordId = LoanRecordId,
				SpendDate = SpendDate,
				SpendTypeId = SpendTypeId,
				UserId = UserId,
				FullPayment = FullPayment
			};

            return copy;
        }
    }

    public class ClientAddSpendAccountIncludeUpdate
    {
        #region Attributes

        public int AccountId { get; set; }
        public int AmountCurrencyId { get; set; }
        public int AccountIncludeId { get; set; }
        public int CurrencyConverterMethodId { get; set; }

        #endregion
    }

	public class ClientAccountPosition
	{
		public int AccountId { get; set; }
		public int Position { get; set; }
	}

    public class ClientAccountInclude
    {
        public int AccountId { get; set; }
        public int AccountIncludeId { get; set; }
        public int CurrencyConverterMethodId { get; set; }
    }

    public class ClientAccountFinanceViewModel
    {
        public int AccountPeriodId { get; set; }
        public bool PendingSpends { get; set; } = true;
        public bool LoanSpends { get; set; } = false;
        public int AmountTypeId { get; set; } = 0;
    }

    #region SpendType

    public class ClientSpendType
	{
		public virtual int SpendTypeId { get; set; }
        public virtual string SpendTypeName { get; set; }

		[DisplayFormat(ConvertEmptyStringToNull = false)]
        public virtual string SpendTypeDescription { get; set; }
        public virtual bool IsSelected { get; set; }
	}

    public class ClientAddSpendType : ClientSpendType
    {
        public override int SpendTypeId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(500)]
        public override string SpendTypeName { get; set; }

        [MaxLength(500)]
        public override string SpendTypeDescription { get; set; }
    }

    public class ClientEditSpendType : ClientSpendType
    {
        [Required]
        [Range(1,int.MaxValue)]
        public override int SpendTypeId { get; set; }

        [MinLength(1)]
        [MaxLength(500)]
        public override string SpendTypeName { get; set; }

        [MaxLength(500)]
        public override string SpendTypeDescription { get; set; }
    }

	public class ClientSpendTypeId
	{
		[Required]
		[Range(1, int.MaxValue)]
		public int SpendTypeId { get; set; }
	}

    #endregion

    #region Password

    public class ClientResetPasswordEmailRequest
    {
        #region Properties

        public string HostUrl { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        #endregion
    }

    public class ClientNewPasswordRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 5, ErrorMessage = "{0} field must have between {2} and {1} characters")]
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Confirm new Password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class SetPassword
    {
        [Required(AllowEmptyStrings = false)]
        public string NewPassword { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string UserId { get; set; }
    }

    #endregion

    #region user

    public enum UserProfile
    {
        Empty = 0,
        Regular = 1,
        RegularPrivileged = 2,
        Admin = 3,
        SuperUser = 4
    }

    public class ClientUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; } 
        public DateTime BirthDate { get; set; }
    }

    public class ClientAddUser : ClientUser
    {
        public string Password { get; set; }
        public string EncryptedPassword { get; set; }
        public string CreatedByUserId { get; set; }
        public string UserId { get; set; }
        public UserProfile Profile { get; set; }
    }

    public class ClientEditUser : ClientUser
    {
        [JsonIgnore]
        public string UserId { get; set; }
    }

    #endregion
}