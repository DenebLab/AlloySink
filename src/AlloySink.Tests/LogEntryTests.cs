using Microsoft.Extensions.Logging;
using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class LogEntryTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Act
        var logEntry = new LogEntry();

        // Assert
        Assert.Equal(default(DateTimeOffset), logEntry.Timestamp);
        Assert.Equal(default(LogLevel), logEntry.Level);
        Assert.Equal(string.Empty, logEntry.Message);
        Assert.Equal(string.Empty, logEntry.ServiceName);
        Assert.Equal(string.Empty, logEntry.ServiceVersion);
        Assert.Equal(string.Empty, logEntry.Environment);
        Assert.NotNull(logEntry.Attributes);
        Assert.Empty(logEntry.Attributes);
        Assert.Null(logEntry.Exception);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var exception = new InvalidOperationException("Test exception");
        var attributes = new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "action", "test" },
            { "count", 42 }
        };

        // Act
        var logEntry = new LogEntry
        {
            Timestamp = timestamp,
            Level = LogLevel.Error,
            Message = "Test message",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "production",
            Attributes = attributes,
            Exception = exception
        };

        // Assert
        Assert.Equal(timestamp, logEntry.Timestamp);
        Assert.Equal(LogLevel.Error, logEntry.Level);
        Assert.Equal("Test message", logEntry.Message);
        Assert.Equal("test-service", logEntry.ServiceName);
        Assert.Equal("1.0.0", logEntry.ServiceVersion);
        Assert.Equal("production", logEntry.Environment);
        Assert.Equal(attributes, logEntry.Attributes);
        Assert.Equal(exception, logEntry.Exception);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void Level_AcceptsAllLogLevels(LogLevel level)
    {
        // Arrange
        var logEntry = new LogEntry();

        // Act
        logEntry.Level = level;

        // Assert
        Assert.Equal(level, logEntry.Level);
    }

    [Fact]
    public void Attributes_CanBeModified()
    {
        // Arrange
        var logEntry = new LogEntry();

        // Act
        logEntry.Attributes["userId"] = "user-123";
        logEntry.Attributes["action"] = "login";
        logEntry.Attributes["count"] = 42;

        // Assert
        Assert.Equal(3, logEntry.Attributes.Count);
        Assert.Equal("user-123", logEntry.Attributes["userId"]);
        Assert.Equal("login", logEntry.Attributes["action"]);
        Assert.Equal(42, logEntry.Attributes["count"]);
    }

    [Fact]
    public void Attributes_CanContainComplexObjects()
    {
        // Arrange
        var complexData = new
        {
            UserId = "user-123",
            Action = "login",
            Metadata = new
            {
                IPAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0"
            },
            Tags = new[] { "authentication", "security" }
        };
        var logEntry = new LogEntry();

        // Act
        logEntry.Attributes["complexData"] = complexData;
        logEntry.Attributes["simpleValue"] = "test";
        logEntry.Attributes["numericValue"] = 123;
        logEntry.Attributes["booleanValue"] = true;

        // Assert
        Assert.Equal(4, logEntry.Attributes.Count);
        Assert.Equal(complexData, logEntry.Attributes["complexData"]);
        Assert.Equal("test", logEntry.Attributes["simpleValue"]);
        Assert.Equal(123, logEntry.Attributes["numericValue"]);
        Assert.Equal(true, logEntry.Attributes["booleanValue"]);
    }

    [Fact]
    public void Exception_CanBeAnyExceptionType()
    {
        // Arrange
        var logEntry = new LogEntry();
        var argumentException = new ArgumentException("Invalid argument", "paramName");
        var nullRefException = new NullReferenceException("Object reference not set");
        var customException = new CustomTestException("Custom message");

        // Act & Assert
        logEntry.Exception = argumentException;
        Assert.Equal(argumentException, logEntry.Exception);

        logEntry.Exception = nullRefException;
        Assert.Equal(nullRefException, logEntry.Exception);

        logEntry.Exception = customException;
        Assert.Equal(customException, logEntry.Exception);
    }

    private class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }
}