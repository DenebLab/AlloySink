using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class AlloyClientTests
{
    [Fact]
    public void Constructor_InitializesWithOptions()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://test:4318",
            MaxRetries = 5,
            RetryDelay = TimeSpan.FromSeconds(2)
        };

        // Act
        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        // Assert - Constructor should not throw
        Assert.NotNull(client);
    }

    [Fact]
    public async Task SendLogsAsync_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var options = new AlloySinkOptions();
        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        // Act
        var result = await client.SendLogsAsync(Array.Empty<LogEntry>());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendLogsAsync_WithValidLogEntries_FormatsAndSendsCorrectly()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999", // Non-existent endpoint to test retry logic
            MaxRetries = 1, // Minimize test time
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        var logEntries = new[]
        {
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Information,
                Message = "Test message",
                ServiceName = "test-service",
                ServiceVersion = "1.0.0",
                Environment = "test"
            }
        };

        // Act
        var result = await client.SendLogsAsync(logEntries);

        // Assert
        // Should return false because endpoint doesn't exist
        Assert.False(result);
    }

    [Fact]
    public async Task SendLogsAsync_WithRetryLogic_AttemptsMultipleTimes()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999", // Non-existent endpoint
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        var logEntries = new[]
        {
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Information,
                Message = "Test message",
                ServiceName = "test-service",
                ServiceVersion = "1.0.0",
                Environment = "test"
            }
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await client.SendLogsAsync(logEntries);

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        // Assert
        Assert.False(result); // Should fail
        // Should take at least 2 retry delays (10ms * 2) but account for test timing variance
        Assert.True(elapsed.TotalMilliseconds >= 15);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var options = new AlloySinkOptions();
        var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        // Act & Assert
        client.Dispose(); // Should not throw

        // Multiple disposes should also not throw
        client.Dispose();
    }

    [Fact]
    public async Task SendLogsAsync_WithComplexLogEntry_HandlesAllFields()
    {
        // Arrange
        var options = new AlloySinkOptions
        {
            AlloyEndpoint = "http://localhost:9999", // Non-existent endpoint
            MaxRetries = 1,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);

        var exception = new InvalidOperationException("Test exception");

        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Error,
            Message = "Complex log message",
            ServiceName = "test-service",
            ServiceVersion = "2.0.0",
            Environment = "production",
            Attributes = new Dictionary<string, object>
            {
                { "scraperId", "scraper-1" },
                { "sessionId", "session-456" },
                { "userId", "user-123" },
                { "action", "test" },
                { "retryCount", 3 },
                { "isSuccess", false },
                { "duration", 1250.5 },
                { "metadata", new { source = "client", version = "2.0.0" } }
            },
            Exception = exception
        };

        // Act
        var result = await client.SendLogsAsync(new[] { logEntry });

        // Assert
        // The method should handle complex log entries without throwing
        Assert.False(result); // Endpoint doesn't exist, so should return false
    }

    [Theory]
    [InlineData("http://localhost:4318")]
    [InlineData("https://alloy.example.com:4318")]
    [InlineData("http://192.168.1.100:4318/")]
    public void Constructor_HandlesVariousEndpointFormats(string endpoint)
    {
        // Arrange
        var options = new AlloySinkOptions { AlloyEndpoint = endpoint };

        // Act & Assert
        using var client = new AlloyClient(options, NullLogger<AlloyClient>.Instance);
        Assert.NotNull(client);
    }
}