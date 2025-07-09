using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public class Example
{
    public static async Task RunExample()
    {
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:4318",
            ServiceName = "example-service",
            ServiceVersion = "1.0.0",
            Environment = "development",
            EnableBatching = true,
            BatchSize = 10,
            BatchInterval = TimeSpan.FromSeconds(5)
        };

        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        using var alloySink = new AlloySink(options, loggerFactory);

        // Simple logging
        await alloySink.LogInfoAsync("Application started");

        // Logging with attributes
        await alloySink.LogInfoAsync("User logged in", new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "sessionId", "session-456" },
            { "loginTime", DateTime.UtcNow },
            { "success", true }
        });

        await alloySink.LogWarningAsync("Low memory warning", new Dictionary<string, object>
        {
            { "memoryUsage", 85 },
            { "threshold", 80 },
            { "component", "memory-monitor" }
        });

        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            await alloySink.LogErrorAsync("An error occurred", ex, new Dictionary<string, object>
            {
                { "operation", "data-processing" },
                { "retryCount", 3 },
                { "userId", "user-123" }
            });
        }

        await alloySink.FlushAsync();
    }

    public static async Task RunDependencyInjectionExample()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();

        // Add AlloySink with configuration
        services.AddAlloySink(options =>
        {
            options.AlloyEndpoint = "http://localhost:4318";
            options.ServiceName = "example-service";
            options.ServiceVersion = "1.0.0";
            options.Environment = "development";
            options.EnableBatching = true;
            options.BatchSize = 10;
            options.BatchInterval = TimeSpan.FromSeconds(5);
        });

        // Or use the simpler overload
        // services.AddAlloySink("http://localhost:4318", "example-service", "1.0.0", "development");

        using var serviceProvider = services.BuildServiceProvider();
        var alloySink = serviceProvider.GetRequiredService<IAlloySink>();

        // Simple logging
        await alloySink.LogInfoAsync("Application started via DI");

        // Logging with attributes
        await alloySink.LogInfoAsync("User logged in", new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "sessionId", "session-456" },
            { "loginTime", DateTime.UtcNow },
            { "success", true }
        });

        await alloySink.FlushAsync();
    }
}