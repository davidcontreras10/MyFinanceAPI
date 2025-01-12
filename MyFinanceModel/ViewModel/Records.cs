using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel;

public record BasicUserViewModel(Guid UserId, string Username, string Name);

public record CreateSimpleDebtRequestVm(IReadOnlyCollection<BasicCurrencyViewModel> SupportedCurrencies, IReadOnlyCollection<BasicUserViewModel> SupportedUsers);


