namespace MyFinanceWebApiCore.Config
{
	public class MongoOptions
	{
		public const string SectionName = "Mongo";

		public string DatabaseName { get; set; } = default!;
	}

}
