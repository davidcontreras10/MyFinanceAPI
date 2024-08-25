namespace MyFinanceModel.Records
{
	public record class SpendOnPeriodId(int SpendId, int AccountPeriodId);
	public record class SpendOnPeriodAccount(SpendOnPeriodId SpendOnPeriodId, int AccountId);
}
