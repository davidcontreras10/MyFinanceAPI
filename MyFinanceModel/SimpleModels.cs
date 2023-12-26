using System;

namespace MyFinanceModel
{
	public class ResourceAccessReportRow
	{
		public int ApplicationResourceId { get; set; }
		public string ApplicationResourceName { get; set; }
		public int ApplicationModuleId { get; set; }
		public string ApplicationModuleName { get; set; }
		public int ResourceActionId { get; set; }
		public string ResourceActionName { get; set; }
		public int ResourceAccessLevelId { get; set; }
		public string ResourceAccessLevelName { get; set; }
	}

    public class DateTimeModel
    {
        public DateTime NewDateTime { get; set; }
    }
}