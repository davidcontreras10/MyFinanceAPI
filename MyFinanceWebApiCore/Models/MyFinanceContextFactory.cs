using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MyFinanceWebApiCore.Models
{
	public class MyFinanceContextFactory : IDesignTimeDbContextFactory<MyFinanceContext>
	{
		public MyFinanceContext CreateDbContext(string[] args)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true)
				.AddEnvironmentVariables()
				.Build();

			var optionsBuilder = new DbContextOptionsBuilder<MyFinanceContext>();
			optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

			return new MyFinanceContext(optionsBuilder.Options);
		}
	}
}
