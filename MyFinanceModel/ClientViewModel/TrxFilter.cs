using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ClientViewModel
{
	public enum TrxFilterType
	{
		Unknown = 0,
		PendingTrxs = 1,
		ByDescription = 2,
        ByTrxType = 3
    }

	public interface ITrxFilter
	{
		public TrxFilterType TrxFilterType { get; }
	}

	public class TrxFiltersContainer
	{
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
        public PendingTrxFilter PendingTrxFilter { get; set; }
        public DescriptionTrxFilter DescriptionTrxFilter { get; set; }
        public TrxTypeFilter TrxTypeFilter { get; set; }
    }

	public class PendingTrxFilter : ITrxFilter
	{
		public TrxFilterType TrxFilterType => TrxFilterType.PendingTrxs;
        public bool Value { get; set; }
    }

	public class DescriptionTrxFilter : ITrxFilter
	{
		public TrxFilterType TrxFilterType => TrxFilterType.ByDescription;
        public string SearchText { get; set; }
    }
	
	public class TrxTypeFilter : ITrxFilter
	{
		public TrxFilterType TrxFilterType => TrxFilterType.ByTrxType;
        public int TrxTypeId { get; set; }
    }
}
