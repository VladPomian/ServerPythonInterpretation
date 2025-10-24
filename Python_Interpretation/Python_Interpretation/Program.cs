using Python_Interpretation.Services;
using Serilog;

class Program
{

    static async Task Main(string[] args)
    {
        TuneSerilog();

        Log.Information("Приложение запущено.");

        try
        {
            var taskService = new TaskService();

            var rabbitService = new RabbitService();
            await rabbitService.EstablishRabbitConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Критическая ошибка при запуске приложения");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        Console.ReadLine();
    }

    private static void TuneSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/server-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:dd.MM.yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
