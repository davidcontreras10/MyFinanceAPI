using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceWebApiCore.Authentication;
using MyFinanceWebApiCore.Config;
using MyFinanceWebApiCore.FilterAttributes;
using MyFinanceWebApiCore.Services;
using Serilog;

namespace MyFinanceWebApiCore
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy("CorsPolicy", builder =>
				{
					builder.WithOrigins("https://icy-sea-0f0fb8a10.2.azurestaticapps.net", "http://localhost:4350", "https://localhost:4350")
						   .AllowAnyHeader()
						   .AllowAnyMethod()
						   .AllowCredentials()
						   .WithExposedHeaders("Content-Disposition");
				});
			});

			services.AddControllers(options =>
			{
				options.Filters.Add<HttpResponseExceptionFilter>();
			});
			RegisterServices(services);
			services.AddHttpClient();
			services.ConfigureSettings(Configuration);
			services.Configure<ApiBehaviorOptions>(options =>
			{
				options.SuppressModelStateInvalidFilter = true;
			});
			services.AddSingleton(Log.Logger);
			services.AddSwaggerGen();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1",
					Title = "Implement Swagger UI",
					Description = "A simple example to Implement Swagger UI",
				});

				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					In = ParameterLocation.Header,
					Description = "Auth token",
					Name = "Authorization",
					Type = SecuritySchemeType.Http,
					BearerFormat = "JWT",
					Scheme = "Bearer"
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type=ReferenceType.SecurityScheme,
								Id="Bearer"
							}
						},
						new string[]{}
					}
				});
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseCors("CorsPolicy");

			app.UseMiddleware<AuthenticationMiddleware>();

			app.UseHttpsRedirection();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}


			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Showing API V1");
			});
		}

		private void RegisterServices(IServiceCollection services)
		{
			var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddDebug();
			});

			services.AddDbContext<MyFinanceContext>(
				options =>
				{
					options.UseSqlServer(Configuration.GetConnectionString("SqlServerLocalConnection"))
					.UseLoggerFactory(loggerFactory)  // Set the logger factory
					.EnableSensitiveDataLogging();

				});
			services.AddSingleton<IBackendSettings, BackendSettings>();
			services.AddScoped<IAuthenticationService, AuthenticationService>();

			services.AddScoped<ITrxExchangeService, TrxExchangeService>();
			services.AddScoped<ITransferService, TransferService>();
			services.AddScoped<IUsersService, UsersService>();
			services.AddScoped<ISpendsService, SpendsService>();
			services.AddScoped<IAccountService, AccountService>();
			services.AddScoped<ICurrencyService, CurrencyService>();
			services.AddScoped<ISpendTypeService, SpendTypeService>();
			services.AddScoped<IAuthorizationService, AuthorizationService>();
			services.AddScoped<IUserAuthorizeService, UserAuthorizeService>();
			services.AddScoped<IEmailService, EmailService>();
			services.AddScoped<IAccountGroupService, AccountGroupService>();

			services.AddScoped<IAccountGroupRepository, EFAccountGroupRepository>();
			services.AddScoped<ISpendTypeRepository, EFSpendTypeRepository>();
			services.AddScoped<IUserRespository, EFUserRepository>();
			services.AddScoped<ISpendsRepository, EFSpendsRepository>();
			services.AddScoped<IAuthorizationDataRepository, EFAuthorizationDataRepository>();
			services.AddScoped<IAccountRepository, EFAccountRepository>();
			services.AddScoped<ITransferRepository, EFTransferRepository>();
			services.AddScoped<IAutomaticTaskRepository, EFAutomaticTaskRepository>();
			services.AddScoped<ILoanRepository, EFLoanRepository>();
			services.AddScoped<IResourceAccessRepository, EFResourceAccessRepository>();
			
			services.AddScoped<IScheduledTasksService, ScheduledTasksService>();
			services.AddScoped<IAccountFinanceService, AccountFinanceService>();
			services.AddScoped<ILoanService, LoanService>();
		}
	}
}
