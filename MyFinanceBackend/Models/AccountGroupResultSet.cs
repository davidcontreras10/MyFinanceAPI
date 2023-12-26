namespace MyFinanceBackend.Models
{
	public class AccountGroupResultSet
	{
		public int AccountGroupId { get; set; }
		public string AccountGroupName { get; set; }
	}

	public class AccountGroupDetailResultSet : AccountGroupResultSet
	{
		public string AccountGroupDisplayValue { get; set; }
		public int AccountGroupPosition { get; set; }
		public bool DisplayDefault { get; set; }
	}
}
