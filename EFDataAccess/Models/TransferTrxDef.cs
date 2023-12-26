using EFDataAccess.Models.Customs;
using System;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
	public partial class TransferTrxDef : IAutomaticTaskDef
	{
        public Guid TransferTrxDefId { get; set; }
        public int ToAccountId { get; set; }

        public virtual Account ToAccount { get; set; }
        public virtual AutomaticTask TransferTrxDefNavigation { get; set; }

		[NotMapped]
		public AutomaticTask AutomaticTaskNavigation
		{
			get => TransferTrxDefNavigation;
			set
			{
				TransferTrxDefNavigation = value;
			}
		}

		[NotMapped]
		public Guid TrxDefId
		{
			get => TransferTrxDefId;
			set
			{
				TransferTrxDefId = value;
			}
		}
	}
}
