using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class AlloySinkIntegrationTests
{
    [Fact]
    public async Task AlloySink_WithBatchingDisabled_SendsLogsImmediately()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999", // Non-existent endpoint for testing
            EnableBatching = false,
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Act & Assert - Should not throw
        await alloySink.LogInfoAsync("Test message", new Dictionary<string, object> { { "component", "test" } });
        await alloySink.LogErrorAsync("Error message", new Exception("Test exception"), new Dictionary<string, object> { { "userId", "user-123" } });
        await alloySink.LogWarningAsync("Warning message");
        await alloySink.LogDebugAsync("Debug message");
    }

    [Fact]
    public async Task AlloySink_WithBatchingEnabled_BatchesLogs()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999", // Non-existent endpoint for testing
            EnableBatching = true,
            BatchSize = 3,
            BatchInterval = TimeSpan.FromSeconds(1),
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Act
        await alloySink.LogInfoAsync("Message 1");
        await alloySink.LogInfoAsync("Message 2");
        await alloySink.LogInfoAsync("Message 3"); // Should trigger batch send

        // Give a moment for async processing
        await Task.Delay(50);

        // Assert - Should not throw and batch should be processed
        await alloySink.FlushAsync();
    }

    [Fact]
    public async Task AlloySink_LogAsync_WithAllParameters_CreatesCorrectLogEntry()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = false,
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        var attributes = new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "action", "test" },
            { "timestamp", DateTime.UtcNow }
        };

        // Act & Assert - Should not throw
        await alloySink.LogAsync(LogLevel.Information, "Test message", attributes);
    }

    [Fact]
    public async Task AlloySink_LogErrorAsync_WithException_HandlesException()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = false,
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        var exception = new InvalidOperationException("Test exception with inner",
            new ArgumentException("Inner exception"));

        // Act & Assert - Should not throw
        await alloySink.LogErrorAsync("Error occurred", exception, new Dictionary<string, object>
        {
            { "operation", "test-operation" },
            { "userId", "user-123" }
        });
    }

    [Fact]
    public async Task AlloySink_FlushAsync_SendsPendingLogs()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = true,
            BatchSize = 10, // Higher than test messages
            BatchInterval = TimeSpan.FromMinutes(10), // Long interval
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Act
        await alloySink.LogInfoAsync("Message 1");
        await alloySink.LogInfoAsync("Message 2");

        // Flush should send the pending logs
        await alloySink.FlushAsync();

        // Assert - Should complete without throwing
        await alloySink.FlushAsync(); // Multiple flushes should be safe
    }

    [Fact]
    public async Task AlloySink_ConcurrentLogging_HandlesMultipleThreads()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = true,
            BatchSize = 20,
            BatchInterval = TimeSpan.FromSeconds(1),
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Act - Log from multiple threads concurrently
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 5; j++)
                {
                    await alloySink.LogInfoAsync($"Thread {threadId} Message {j}", new Dictionary<string, object>
                    {
                        { "threadId", threadId },
                        { "messageId", j },
                        { "timestamp", DateTime.UtcNow }
                    });
                }
            }));
        }

        await Task.WhenAll(tasks);
        await alloySink.FlushAsync();

        // Assert - Should complete without throwing or deadlocking
    }

    [Fact]
    public void AlloySink_Dispose_HandlesGracefulShutdown()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = true,
            BatchSize = 10,
            BatchInterval = TimeSpan.FromSeconds(1)
        };

        var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Add some logs
        alloySink.LogInfoAsync("Message before dispose").Wait();

        // Act & Assert - Should not throw
        alloySink.Dispose();

        // Multiple disposes should be safe
        alloySink.Dispose();
    }

    [Fact]
    public async Task AlloySink_AfterDispose_IgnoresNewLogs()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = false
        };

        var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);
        alloySink.Dispose();

        // Act & Assert - Should not throw
        await alloySink.LogInfoAsync("Message after dispose");
        await alloySink.LogErrorAsync("Error after dispose", new Exception("Test"));
        await alloySink.FlushAsync();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task AlloySink_WithDifferentBatchSizes_HandlesBatching(int batchSize)
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999",
            EnableBatching = true,
            BatchSize = batchSize,
            BatchInterval = TimeSpan.FromSeconds(1),
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var alloySink = new Deneblab.AlloySink.AlloySink(options, NullLoggerFactory.Instance);

        // Act - Send more logs than batch size
        var logCount = batchSize + 2;
        for (int i = 0; i < logCount; i++)
        {
            await alloySink.LogInfoAsync($"Message {i}");
        }

        await alloySink.FlushAsync();

        // Assert - Should complete without throwing
    }
}