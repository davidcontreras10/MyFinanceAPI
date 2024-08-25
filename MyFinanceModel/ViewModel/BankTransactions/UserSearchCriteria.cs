using System;
using System.Collections.Generic;


namespace MyFinanceModel.ViewModel.BankTransactions
{
	public interface IUserSearchCriteria
	{
		string RequesterUserId { get; set; }
	}

	public class BankTrxSearchCriteria
	{
		public class AppTransactionIds : IUserSearchCriteria
		{
			public string RequesterUserId { get; set; }
			public IReadOnlyCollection<int> TransactionIds { get; set; }
		}

		public class RefNumberSearchCriteria : IUserSearchCriteria
		{
			public string RequesterUserId { get; set; }
			public string RefNumber { get; set; }
        }

		public class DescriptionSearchCriteria : IUserSearchCriteria
		{
			public string RequesterUserId { get; set; }
			public string Description { get; set; }
		}

		public class DateSearchCriteria : IUserSearchCriteria
		{
			public string RequesterUserId { get; set; }
			public DateOnly Date { get; set; }
		}
	}

}
