using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public interface ITransferService
    {
        Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccountAsync(int accountPeriodId, int currencyId,
            string userId, BalanceTypes balanceType);
        Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrenciesAsync(int accountId, string userId);
        Task<TransferAccountDataViewModel> GetBasicAccountInfoAsync(int accountPeriodId, string userId);
        Task<IEnumerable<ItemModified>> SubmitTransferAsync(TransferClientViewModel transferClientViewModel);
    }
}