using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.GptClassifiedExpenseCache;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;

namespace MyFinanceModel.Mappers
{
	public static class BankTrxCategorizationMapper
	{
		public static InGptClassifiedExpenseCache ToInGptClassifiedExpenseCache(OutGptClassifiedExpense outGptClassifiedExpense)
		{
			return new InGptClassifiedExpenseCache
			{
				Description = outGptClassifiedExpense.Description,
				Amount = outGptClassifiedExpense.Amount,
				Currency = outGptClassifiedExpense.Currency,
				Category = outGptClassifiedExpense.Category,
				CategoryConfidence = outGptClassifiedExpense.CategoryConfidence,
				AccountName = outGptClassifiedExpense.AccountName,
				AccountConfidence = outGptClassifiedExpense.AccountConfidence,
				CategoryId = outGptClassifiedExpense.CategoryId,
				AccountId = outGptClassifiedExpense.AccountId

			};
		}

		public static InHisotricClassfiedExpense ToInHisotricClassfiedExpense(
			ClassifiedBankTrx classifiedBankTrx)
		{
			return new InHisotricClassfiedExpense
			{
				Description = classifiedBankTrx.trxDescription,
				Category = classifiedBankTrx.TrxTypeName,
				AccountName = classifiedBankTrx.AccountName,
				Amount = classifiedBankTrx.OriginalAmount,
				Currency = classifiedBankTrx.CurrencyCode
			};
		}

		public static ExpenseToClassify ToExpenseToClassify(ToClassifyBankTrx toClassifyBankTrx)
		{
			return new ExpenseToClassify
			{
				Id = toClassifyBankTrx.BankTrxId.TransactionId,
				Description = toClassifyBankTrx.Description,
				Amount = toClassifyBankTrx.OriginalAmount,
				Currency = toClassifyBankTrx.CurrencyCode
			};
		}

		public static Gpt.Category ToGptCategory(SpendTypeViewModel spendTypeViewModel)
		{
			return new Gpt.Category(spendTypeViewModel.Id, spendTypeViewModel.SpendTypeName);
		}

		public static Gpt.Account ToGptAccount(AiClassifiableAccount classifiableAccount)
		{
			return new Gpt.Account(
				classifiableAccount.AiClassificationHint,
				classifiableAccount.AccountId,
				classifiableAccount.AccountName);
		}
	}
}
