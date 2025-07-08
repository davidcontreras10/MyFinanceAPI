namespace MyFinanceWebApiCore.Config
{
	public class MongoOptions
	{
		public const string SectionName = "Mongo";

		public string ConnectionString { get; set; } = default!;
		public string DatabaseName { get; set; } = default!;
	}

}
