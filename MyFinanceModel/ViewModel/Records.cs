using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
	public record class BasicUserViewModel
	{
		public Guid UserId { get; set; }
		public string Username { get; set; }
        public string Name { get; set; }
    }

	public record class CreateSimpleDebtRequestVm(IReadOnlyCollection<BasicCurrencyViewModel> SupportedCurrencies, IReadOnlyCollection<BasicUserViewModel> SupportedUsers);
}
