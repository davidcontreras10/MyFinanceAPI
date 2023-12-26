namespace MyFinanceModel
{
    public enum ResourceActionNames
    {
        Unknown = 0,
        View = 1,
        Add = 2,
        Edit = 3,
        Delete = 4,
        EditSensitive = 5
    }

	public enum ApplicationModules
	{
		Unknown = 0,
		User = 1,
		MainSpend = 2,
		Accounts = 3,
		SpendType = 4,
		Loan = 5
	}

    public enum ApplicationResources
    {
        Unknown = 0,
        Users = 1,
		Spends = 2
    }

    public enum ResourceAccesLevels
    {
        Unknown = 0,
        Any = 1,
        Self = 2,
        Owned = 3,
		LoanRelated = 4,
		TransferRelated = 5,
		AnyOther = 6,
        AddRegular = 7
    }

    public class UserAssignedAccess
    {   
        public string UserId { get; set; }
        public ApplicationResources ApplicationResource { get; set; }
        public ResourceActionNames ResourceActionName { get; set; }
        public ResourceAccesLevels ResourceAccesLevel { get; set; }
    }
}
