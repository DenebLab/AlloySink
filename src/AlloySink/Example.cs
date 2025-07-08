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

        using var alloySink = new AlloySink(options);

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
}