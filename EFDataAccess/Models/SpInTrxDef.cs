using EFDataAccess.Models.Customs;
using System;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
	public partial class SpInTrxDef : IAutomaticTaskDef
	{
		public Guid SpInTrxDefId { get; set; }
		public bool IsSpendTrx { get; set; }

		public virtual AutomaticTask SpInTrxDefNavigation { get; set; }

		[NotMapped]
		public AutomaticTask AutomaticTaskNavigation
		{
			get => SpInTrxDefNavigation;
			set
			{
				SpInTrxDefNavigation = value;
			}
		}

		[NotMapped]
		public Guid TrxDefId
		{
			get => SpInTrxDefId;
			set
			{
				SpInTrxDefId = value;
			}
		}
	}
}