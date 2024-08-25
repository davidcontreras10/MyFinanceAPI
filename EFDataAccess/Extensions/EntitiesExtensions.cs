using EFDataAccess.Models;
using MyFinanceModel.Records;

namespace EFDataAccess.Extensions
{
	public static class EntitiesExtensions
    {
        public static BankTrxId GetId(this EFBankTransaction entity)
		{
			return new BankTrxId(entity.FinancialEntityId, entity.BankTransactionId);
		}

	}
}
