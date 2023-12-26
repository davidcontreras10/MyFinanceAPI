using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess
{
	public class NewPeriodDatesResult
	{
		public NewPeriodDatesResult(DateTime initialDate, DateTime endDate)
		{
			InitialDate = initialDate;
			EndDate = endDate;
		}


		public DateTime InitialDate { get; set; }

		public DateTime EndDate { get; set; }
    }
}
