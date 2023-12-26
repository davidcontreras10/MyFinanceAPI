using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EFDataAccess.Models.Customs
{
	public interface IAutomaticTaskDef
	{
		[NotMapped]
		public AutomaticTask AutomaticTaskNavigation { get; set; }

		[NotMapped]
		public Guid TrxDefId { get; set; }
	}
}
