using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace MyFinanceWebApiCore.Models
{
	public class MyFinanceContextFactory : IDesignTimeDbContextFactory<MyFinanceContext>
	{
		public MyFinanceContext CreateDbContext(string[] args)
		{
			//System.Diagnostics.Debugger.Launch();
			var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var configurationBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json");

			if (!string.IsNullOrWhiteSpace(environment))
			{
				configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);
			}

			configurationBuilder = configurationBuilder.AddEnvironmentVariables();
			var configuration = configurationBuilder.Build();
			var optionsBuilder = new DbContextOptionsBuilder<MyFinanceContext>();
			var connectionString = configuration.GetConnectionString("DefaultConnection");
			optionsBuilder.UseSqlServer(connectionString);
			return new MyFinanceContext(optionsBuilder.Options);
		}
	}
}

