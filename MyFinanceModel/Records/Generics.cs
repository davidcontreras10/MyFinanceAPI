using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.Records
{
	public record class AccountWithTrxTypeId(int? TrxTypeId) : IDropDownSelectable
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public bool IsSelected { get; set; }
	}

	public record class ItemModifiedRecord(int AccountId, bool IsModified);

	public record class TrxItemModifiedRecord(int AccountId, bool IsModified, int SpendId, int AccountPeriodId, Guid? RequestId = null) : ItemModifiedRecord(AccountId, IsModified);

	public record class IdentifiableRequest<T>(T Request, Guid RequestId);

	public record class IdDateTime(int AccountId, DateTime DateTime);
}
