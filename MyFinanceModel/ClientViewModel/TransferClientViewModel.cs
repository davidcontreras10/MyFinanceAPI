using System;

namespace MyFinanceModel.ClientViewModel
{
    public class TransferClientViewModel : ClientBasicAddSpend
    {
        public int AccountPeriodId { get; set; }
        public int DestinationAccount { get; set; }
        public BalanceTypes BalanceType { get; set; }

        public TransferClientViewModel GetCopy()
        {
            return new TransferClientViewModel
            {
                UserId = UserId,
                AccountPeriodId = AccountPeriodId,
                Amount = Amount,
                CurrencyId = CurrencyId,
                BalanceType = BalanceType,
                SpendDate = SpendDate,
                DestinationAccount = DestinationAccount,
                Description = Description,
                SpendTypeId = SpendTypeId
            };
        }
    }

    public enum BalanceTypes
    {
        Invalid = 0, Custom = 1, AccountPeriodBalance = 2, AccountOverallBalance = 3
    }
}
