using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xunit;

using Deneblab.AlloySink;

namespace AlloySink.Tests;

public class OtlpLogFormatterTests
{
    [Fact]
    public void FormatLogs_WithSingleLogEntry_ReturnsValidOtlpJson()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Level = LogLevel.Information,
            Message = "Test message",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Attributes = new Dictionary<string, object>
            {
                { "userId", "user-123" },
                { "sessionId", "session-456" }
            }
        };

        // Act
        var json = OtlpLogFormatter.FormatLogs(new[] { logEntry });

        // Assert
        Assert.NotEmpty(json);

        var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("resourceLogs", out var resourceLogs));
        Assert.Equal(JsonValueKind.Array, resourceLogs.ValueKind);
        Assert.Equal(1, resourceLogs.GetArrayLength());

        var resourceLog = resourceLogs[0];
        Assert.True(resourceLog.TryGetProperty("resource", out var resource));
        Assert.True(resourceLog.TryGetProperty("scopeLogs", out var scopeLogs));

        var scopeLog = scopeLogs[0];
        Assert.True(scopeLog.TryGetProperty("logRecords", out var logRecords));
        Assert.Equal(1, logRecords.GetArrayLength());

        var logRecord = logRecords[0];
        Assert.True(logRecord.TryGetProperty("body", out var body));
        Assert.Equal("Test message", body.GetProperty("stringValue").GetString());
        Assert.True(logRecord.TryGetProperty("severityText", out var severity));
        Assert.Equal("INFO", severity.GetString());
    }

    [Fact]
    public void FormatLogs_WithMultipleLogEntries_ReturnsBatchedOtlpJson()
    {
        // Arrange
        var logEntries = new[]
        {
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Information,
                Message = "First message",
                ServiceName = "test-service",
                ServiceVersion = "1.0.0",
                Environment = "test"
            },
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Warning,
                Message = "Second message",
                ServiceName = "test-service",
                ServiceVersion = "1.0.0",
                Environment = "test"
            }
        };

        // Act
        var json = OtlpLogFormatter.FormatLogs(logEntries);

        // Assert
        var document = JsonDocument.Parse(json);
        var logRecords = document.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords");

        Assert.Equal(2, logRecords.GetArrayLength());
        Assert.Equal("First message", logRecords[0].GetProperty("body").GetProperty("stringValue").GetString());
        Assert.Equal("Second message", logRecords[1].GetProperty("body").GetProperty("stringValue").GetString());
        Assert.Equal("INFO", logRecords[0].GetProperty("severityText").GetString());
        Assert.Equal("WARN", logRecords[1].GetProperty("severityText").GetString());
    }

    [Fact]
    public void FormatLogs_WithException_IncludesExceptionAttributes()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Error,
            Message = "Error occurred",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Exception = exception
        };

        // Act
        var json = OtlpLogFormatter.FormatLogs(new[] { logEntry });

        // Assert
        var document = JsonDocument.Parse(json);
        var attributes = document.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0]
            .GetProperty("attributes");

        var hasExceptionType = false;
        var hasExceptionMessage = false;

        foreach (var attr in attributes.EnumerateArray())
        {
            var key = attr.GetProperty("key").GetString();
            if (key == "exception.type")
            {
                hasExceptionType = true;
                Assert.Equal("InvalidOperationException", attr.GetProperty("value").GetProperty("stringValue").GetString());
            }
            if (key == "exception.message")
            {
                hasExceptionMessage = true;
                Assert.Equal("Test exception", attr.GetProperty("value").GetProperty("stringValue").GetString());
            }
        }

        Assert.True(hasExceptionType);
        Assert.True(hasExceptionMessage);
    }

    [Fact]
    public void FormatLogs_WithComplexAttributes_SerializesCorrectly()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Information,
            Message = "User action",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test",
            Attributes = new Dictionary<string, object>
            {
                { "userId", "user-123" },
                { "action", "login" },
                { "count", 5 },
                { "success", true },
                { "score", 95.5 },
                { "metadata", new { source = "web", device = "mobile" } }
            }
        };

        // Act
        var json = OtlpLogFormatter.FormatLogs(new[] { logEntry });

        // Assert
        var document = JsonDocument.Parse(json);
        var attributes = document.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0]
            .GetProperty("attributes");

        var foundAttributes = new Dictionary<string, JsonElement>();
        foreach (var attr in attributes.EnumerateArray())
        {
            var key = attr.GetProperty("key").GetString();
            var value = attr.GetProperty("value");
            if (key != null) foundAttributes[key] = value;
        }

        Assert.True(foundAttributes.ContainsKey("userId"));
        Assert.Equal("user-123", foundAttributes["userId"].GetProperty("stringValue").GetString());

        Assert.True(foundAttributes.ContainsKey("count"));
        Assert.Equal(5, foundAttributes["count"].GetProperty("intValue").GetInt32());

        Assert.True(foundAttributes.ContainsKey("success"));
        Assert.True(foundAttributes["success"].GetProperty("boolValue").GetBoolean());

        Assert.True(foundAttributes.ContainsKey("score"));
        Assert.Equal(95.5, foundAttributes["score"].GetProperty("doubleValue").GetDouble());

        Assert.True(foundAttributes.ContainsKey("metadata"));
        var metadataJson = foundAttributes["metadata"].GetProperty("stringValue").GetString();
        Assert.Contains("web", metadataJson);
        Assert.Contains("mobile", metadataJson);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRACE", 1)]
    [InlineData(LogLevel.Debug, "DEBUG", 5)]
    [InlineData(LogLevel.Information, "INFO", 9)]
    [InlineData(LogLevel.Warning, "WARN", 13)]
    [InlineData(LogLevel.Error, "ERROR", 17)]
    [InlineData(LogLevel.Critical, "FATAL", 21)]
    public void FormatLogs_WithDifferentLogLevels_MapsToCorrectSeverity(LogLevel level, string expectedText, int expectedNumber)
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = "Test message",
            ServiceName = "test-service",
            ServiceVersion = "1.0.0",
            Environment = "test"
        };

        // Act
        var json = OtlpLogFormatter.FormatLogs(new[] { logEntry });

        // Assert
        var document = JsonDocument.Parse(json);
        var logRecord = document.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Assert.Equal(expectedText, logRecord.GetProperty("severityText").GetString());
        Assert.Equal(expectedNumber, logRecord.GetProperty("severityNumber").GetInt32());
    }

    [Fact]
    public void FormatLogs_WithEmptyCollection_ReturnsEmptyString()
    {
        // Act
        var json = OtlpLogFormatter.FormatLogs(Array.Empty<LogEntry>());

        // Assert
        Assert.Equal(string.Empty, json);
    }
}