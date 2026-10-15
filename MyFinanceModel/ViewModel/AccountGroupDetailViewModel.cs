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

	/// <summary>
	/// Group as the Accounts screen manages it. <see cref="AccountGroupViewModel.IsSelected"/> means "the group
	/// being viewed" here, so the default flag is exposed separately: a default group is one the Finance page
	/// opens expanded, and several can be default.
	/// </summary>
	public class AccountGroupAdminViewModel : AccountGroupViewModel
	{
		public bool DisplayDefault { get; set; }
		public int AccountCount { get; set; }
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
