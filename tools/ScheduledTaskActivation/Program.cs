using System.Text.Json;
using System.Text.Json.Serialization;
using MyFinanceModel;

namespace ScheduledTaskActivation;

public static class Program
{
    private const string ConnectionVariable = "MYFINANCE_SCHEDULE_ACTIVATION_CONNECTION";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length is not (2 or 3) || args[0] != "--plan" ||
            args.Length == 3 && args[2] != "--apply")
        {
            Console.Error.WriteLine("Usage: dotnet run --project tools/ScheduledTaskActivation -- --plan <selection.json> [--apply]");
            return 1;
        }
        try
        {
            var connection = Environment.GetEnvironmentVariable(ConnectionVariable);
            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentException($"Set {ConnectionVariable} explicitly; no application settings are loaded.");
            var plan = JsonSerializer.Deserialize<ActivationPlan>(await File.ReadAllTextAsync(args[1]), JsonOptions)
                ?? throw new ArgumentException("The activation plan is empty.");
            var runner = new ActivationRunner(connection, TimeProvider.System);
            if (args.Length == 3)
            {
                var completed = await runner.ApplyAsync(plan,
                    id => Console.Error.WriteLine($"Activation confirmed for {id:D}."));
                Console.WriteLine(JsonSerializer.Serialize(new { activatedTaskIds = completed }, JsonOptions));
            }
            else
                Console.WriteLine(JsonSerializer.Serialize(await runner.PreviewAsync(plan), JsonOptions));
            return 0;
        }
        catch (Exception error) when (error is ArgumentException or JsonException or ServiceException)
        {
            Console.Error.WriteLine(error.Message);
            if (args.Length == 3)
                Console.Error.WriteLine("Earlier task activations may have committed. Keep the reviewed plan and inspect progress before resuming.");
            return 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Activation stopped ({error.GetType().Name}). No financial work was invoked. " +
                "Some earlier task activations may have committed; inspect state and resume with the same reviewed plan. " +
                "Check database connectivity, schema and operator file access. Connection details are not printed.");
            return 1;
        }
    }
}
