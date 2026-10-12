using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ApiFunctions
{
    public class DailyRefresh
    {
	    private readonly ILogger<DailyRefresh> _logger;

	    public DailyRefresh(ILogger<DailyRefresh> logger)
	    {
		    _logger = logger;
	    }

	    // ReSharper disable UnusedMember.Local
	    private const string TestCron = "0 */2 * * * *";
	    private const string DailyCron = "0 1 6 * * *";
	    private const string WeeklyCron = "0 0 6 * * 1";
	    // ReSharper restore UnusedMember.Local

		[Function(nameof(DailyRefresh))]
        public async Task Run([TimerTrigger(DailyCron)]TimerInfo myTimer)
        {
	        var str = Environment.GetEnvironmentVariable("DefaultMYFNDB");
	        if (string.IsNullOrWhiteSpace(str))
	        {
		        const string error = "Connection string DefaultMYFNDB not found";
				_logger.LogError(error);
				throw new Exception(error);
	        }

			_logger.LogInformation("Connected. Executing script.");
			await using var conn = new SqlConnection(str);
			conn.Open();
			var text = @"
					BEGIN
						DECLARE @currentUserId UNIQUEIDENTIFIER
						DECLARE @currentDate DATETIME = GETDATE()
						DECLARE tempusersCursor CURSOR FAST_FORWARD LOCAL
						FOR SELECT UserId FROM dbo.AppUser
						OPEN tempusersCursor
						FETCH NEXT FROM tempusersCursor INTO @currentUserId
						WHILE @@FETCH_STATUS = 0
						BEGIN
							EXEC dbo.SpUpdateCurrentPeriods @pUserId = @currentUserId, @pDate = @currentDate
							FETCH NEXT FROM tempusersCursor INTO @currentUserId
						END
						CLOSE tempusersCursor
						INSERT INTO [dbo].[DailyJob] (JobDate, EventDesc) VALUES (@currentDate, 'Function local script EveryDay local');
					END
					";

			await using var cmd = new SqlCommand(text, conn) {CommandTimeout = conn.ConnectionTimeout};
			await cmd.ExecuteNonQueryAsync();
			_logger.LogInformation("DailyRefresh. Executed");
		}
    }
}
