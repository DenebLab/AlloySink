# LogPusher Library Design

## Architecture

### Core Components
1. **LogPusher** - Main class for sending logs
2. **LogEntry** - Structured log entry model
3. **BatchManager** - Handles batching and sending
4. **RetryHandler** - Manages retry logic
5. **AlloyClient** - HTTP client for Alloy OTLP endpoint

## API Design

### LogPusher Class
```csharp
public class LogPusher : IDisposable
{
    public LogPusher(LogPusherOptions options)
    public Task LogAsync(LogLevel level, string message, string? scraperId = null, string? sessionId = null, object? eventData = null)
    public Task LogInfoAsync(string message, string? scraperId = null, string? sessionId = null, object? eventData = null)
    public Task LogErrorAsync(string message, Exception? exception = null, string? scraperId = null, string? sessionId = null)
    public Task FlushAsync()
    public void Dispose()
}
```

### LogPusherOptions
```csharp
public class LogPusherOptions
{
    public string AlloyEndpoint { get; set; } = "http://localhost:4318";
    public string ServiceName { get; set; } = "scraper-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";
    public int BatchSize { get; set; } = 100;
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
```

### LogEntry Model
```csharp
public class LogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; }
    public string ServiceName { get; set; }
    public string ServiceVersion { get; set; }
    public string Environment { get; set; }
    public string? ScraperId { get; set; }
    public string? SessionId { get; set; }
    public object? EventData { get; set; }
    public Exception? Exception { get; set; }
}
```

## OTLP Format

### Log Record Structure
```json
{
  "resourceLogs": [
    {
      "resource": {
        "attributes": [
          {"key": "service.name", "value": {"stringValue": "scraper-service"}},
          {"key": "service.version", "value": {"stringValue": "1.0.0"}},
          {"key": "environment", "value": {"stringValue": "development"}}
        ]
      },
      "scopeLogs": [
        {
          "scope": {"name": "LogPusher"},
          "logRecords": [
            {
              "timeUnixNano": "1640995200000000000",
              "severityText": "INFO",
              "body": {"stringValue": "Scraper started"},
              "attributes": [
                {"key": "scraper.id", "value": {"stringValue": "scraper-1"}},
                {"key": "session.id", "value": {"stringValue": "abc12345"}},
                {"key": "event.data", "value": {"stringValue": "{...}"}}
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

## Implementation Strategy

1. **Phase 1**: Basic LogPusher with synchronous sending
2. **Phase 2**: Add batching and background sending
3. **Phase 3**: Add retry logic and error handling
4. **Phase 4**: Optimize performance and add configuration options

## Benefits vs OpenTelemetry

### LogPusher Advantages:
- ✅ Simpler API (no complex configuration)
- ✅ Direct control over log format
- ✅ Custom batching and retry logic
- ✅ Reduced dependencies
- ✅ Domain-specific features (scraperId, sessionId)

### OpenTelemetry Advantages:
- ✅ Standard protocol compliance
- ✅ Rich ecosystem and tooling
- ✅ Automatic instrumentation
- ✅ Multiple exporters support

## Migration Path

1. Create LogPusher library alongside existing OpenTelemetry
2. Update scraper service to use LogPusher
3. Test functionality and performance
4. Remove OpenTelemetry dependencies
5. Update documentation and examples