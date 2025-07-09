using Microsoft.Extensions.Logging;
using System.Text.Json;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public static class TestHelpers
{
    public static LogEntry CreateTestLogEntry(
        LogLevel level = LogLevel.Information,
        string message = "Test message",
        string serviceName = "test-service",
        string serviceVersion = "1.0.0",
        string environment = "test",
        Dictionary<string, object>? attributes = null,
        Exception? exception = null)
    {
        return new LogEntry
        {
            Timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Level = level,
            Message = message,
            ServiceName = serviceName,
            ServiceVersion = serviceVersion,
            Environment = environment,
            Attributes = attributes ?? new Dictionary<string, object>(),
            Exception = exception
        };
    }

    public static AlloySinkOptions CreateTestOptions(
        string endpoint = "http://localhost:4318",
        bool enableBatching = true,
        int batchSize = 100,
        int maxRetries = 3)
    {
        return new AlloySinkOptions
        {
            AlloyEndpoint = endpoint,
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            EnableBatching = enableBatching,
            BatchSize = batchSize,
            BatchInterval = TimeSpan.FromSeconds(1),
            MaxRetries = maxRetries,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };
    }

    public static void ValidateOtlpJsonStructure(string json)
    {
        var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("resourceLogs", out var resourceLogs))
            throw new InvalidOperationException("Missing resourceLogs property");

        if (resourceLogs.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("resourceLogs should be an array");

        if (resourceLogs.GetArrayLength() == 0)
            throw new InvalidOperationException("resourceLogs array should not be empty");

        var resourceLog = resourceLogs[0];

        if (!resourceLog.TryGetProperty("resource", out _))
            throw new InvalidOperationException("Missing resource property");

        if (!resourceLog.TryGetProperty("scopeLogs", out var scopeLogs))
            throw new InvalidOperationException("Missing scopeLogs property");

        var scopeLog = scopeLogs[0];

        if (!scopeLog.TryGetProperty("logRecords", out var logRecords))
            throw new InvalidOperationException("Missing logRecords property");

        if (logRecords.GetArrayLength() == 0)
            throw new InvalidOperationException("logRecords array should not be empty");
    }

    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? interval = null)
    {
        var checkInterval = interval ?? TimeSpan.FromMilliseconds(50);
        var endTime = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < endTime)
        {
            if (condition())
                return true;

            await Task.Delay(checkInterval);
        }

        return false;
    }

    public static Exception CreateNestedTestException()
    {
        try
        {
            try
            {
                throw new ArgumentException("Inner exception message");
            }
            catch (Exception inner)
            {
                throw new InvalidOperationException("Outer exception message", inner);
            }
        }
        catch (Exception outer)
        {
            return outer;
        }
    }

    public static object CreateComplexEventData()
    {
        return new
        {
            UserId = "user-12345",
            Action = "user_login",
            Timestamp = DateTime.UtcNow,
            Metadata = new
            {
                IPAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                SessionDuration = TimeSpan.FromMinutes(45).TotalMilliseconds
            },
            Tags = new[] { "authentication", "security", "audit" },
            Success = true,
            RetryCount = 0,
            AdditionalData = new Dictionary<string, object>
            {
                { "feature_flags", new[] { "new_ui", "advanced_logging" } },
                { "user_preferences", new { theme = "dark", notifications = true } },
                { "performance_metrics", new { load_time_ms = 250, memory_usage_mb = 45.2 } }
            }
        };
    }

    public static IEnumerable<LogEntry> CreateBatchOfLogEntries(int count, string baseMessage = "Batch message")
    {
        return Enumerable.Range(1, count).Select(i => CreateTestLogEntry(
            level: (LogLevel)(i % 6), // Cycle through log levels
            message: $"{baseMessage} {i}",
            attributes: new Dictionary<string, object>
            {
                { "messageNumber", i },
                { "batchTest", true },
                { "componentId", $"component-{i % 3 + 1}" },
                { "sessionId", $"session-{Guid.NewGuid():N}" }
            }
        ));
    }
}