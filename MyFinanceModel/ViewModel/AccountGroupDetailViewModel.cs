namespace MyFinanceModel.ViewModel
{
	public class AccountGroupViewModel : IDropDownSelectable
	{
		public int AccountGroupId { get; set; }
		public string AccountGroupName { get; set; }
		public int AccountGroupPosition { get; set; }
		public string AccountGroupDisplayValue { get; set; }

		public int Id => AccountGroupId;
	    public string Name => AccountGroupName;
	    public virtual bool IsSelected { get; set; }
	}

	public class AccountGroupDetailViewModel : AccountGroupViewModel
	{
		public bool DisplayDefault { get; set; }
		public override bool IsSelected => DisplayDefault;
	}

	public class AccountGroupPosition : IDropDownSelectable
	{
		public int AccountGroupId { get; set; }
		public int Position { get; set; }
		public int Id => Position;
	    public string Name { get; set; }
		public bool IsSelected { get; set; }
	}
}
