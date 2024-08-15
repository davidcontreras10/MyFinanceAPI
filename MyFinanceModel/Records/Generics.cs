using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.Records
{
	public record class ItemModifiedRecord(int AccountId, bool IsModified);

	public record class TrxItemModifiedRecord(int AccountId, bool IsModified, int SpendId) : ItemModifiedRecord(AccountId, IsModified);
}
