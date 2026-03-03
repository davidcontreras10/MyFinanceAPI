using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Models.Customs
{
	internal class EFDebtRequestAdditional
	{
		public EFDebtRequest EFDebtRequest { get; set; }
		public int DebtorSpendsCount { get; set; }
		public int CreditorSpendsCount { get; set; }
	}
}
