# AlloySink

A lightweight .NET 8.0 C# library for sending structured logs directly to Grafana Alloy using OpenTelemetry Protocol (OTLP). Provides a simple API with batching, retry logic, and flexible attribute system for any logging scenario.

## Features

- **Direct OTLP integration** - Send logs directly to Grafana Alloy (port 4318)
- **Semantic versioning** - Automated with AbcVersion tool
- **Flexible attributes** - Generic `Dictionary<string, object>` for any data
- **Batch processing** - Configurable batch size and intervals
- **Retry logic** - Robust error handling with exponential backoff
- **Thread-safe design** - Concurrent logging support
- **Comprehensive testing** - 54 tests covering all scenarios ✅
- **Production ready** - Used in production environments

## Quick Start

### Installation

Install from NuGet:

```bash
dotnet add package AlloySink
```

Or via Package Manager:
```powershell
Install-Package AlloySink
```

### Basic Usage

```csharp
using Deneblab.AlloySink;

var options = new AlloySinkOptions
{
    AlloyEndpoint = "http://localhost:4318",
    ServiceName = "my-service",
    ServiceVersion = "1.0.0",
    Environment = "production",
    EnableBatching = true,
    BatchSize = 100,
    BatchInterval = TimeSpan.FromSeconds(5)
};

using var alloySink = new AlloySink(options);

// Simple logging
await alloySink.LogInfoAsync("Application started");

// Log with custom attributes
await alloySink.LogInfoAsync("User logged in", new Dictionary<string, object>
{
    { "userId", "user-456" },
    { "sessionId", "session-789" },
    { "loginTime", DateTime.UtcNow },
    { "ipAddress", "192.168.1.100" }
});

// Log error with exception and attributes
try
{
    // Some operation
}
catch (Exception ex)
{
    await alloySink.LogErrorAsync("Operation failed", ex, new Dictionary<string, object>
    {
        { "operation", "data-processing" },
        { "userId", "user-456" },
        { "retryCount", 3 }
    });
}

// Ensure all logs are sent before shutdown
await alloySink.FlushAsync();
```

## Configuration

### AlloySinkOptions

| Property | Default | Description |
|----------|---------|-------------|
| `AlloyEndpoint` | `http://localhost:4318` | Alloy OTLP HTTP endpoint |
| `ServiceName` | `scraper-service` | Service name for telemetry |
| `ServiceVersion` | `1.0.0` | Service version |
| `Environment` | `development` | Environment (dev/staging/prod) |
| `BatchSize` | `100` | Number of logs to batch together |
| `BatchInterval` | `5 seconds` | Time interval for batch sending |
| `MaxRetries` | `3` | Maximum retry attempts for failed requests |
| `RetryDelay` | `1 second` | Delay between retry attempts |
| `EnableBatching` | `true` | Enable/disable batching |

## API Reference

### AlloySink Class

#### Methods

- `LogAsync(LogLevel, string, Dictionary<string, object>?)` - Log with specified level
- `LogInfoAsync(string, Dictionary<string, object>?)` - Log info message
- `LogErrorAsync(string, Exception?, Dictionary<string, object>?)` - Log error with exception
- `LogWarningAsync(string, Dictionary<string, object>?)` - Log warning message
- `LogDebugAsync(string, Dictionary<string, object>?)` - Log debug message
- `FlushAsync()` - Flush all pending logs immediately

#### Parameters

- `message` - Log message
- `attributes` - Optional dictionary of custom attributes/metadata
- `exception` - Optional exception for error logs

#### Supported Attribute Types

The attributes dictionary supports these value types with proper OTLP formatting:
- `string` - Mapped to stringValue
- `int`/`long` - Mapped to intValue  
- `double`/`float` - Mapped to doubleValue
- `bool` - Mapped to boolValue
- `object` - JSON serialized to stringValue

## OTLP Format

The library sends logs in OpenTelemetry Protocol (OTLP) format to Alloy:

```json
{
  "resourceLogs": [
    {
      "resource": {
        "attributes": [
          {"key": "service.name", "value": {"stringValue": "my-service"}},
          {"key": "service.version", "value": {"stringValue": "1.0.0"}},
          {"key": "environment", "value": {"stringValue": "production"}}
        ]
      },
      "scopeLogs": [
        {
          "scope": {"name": "AlloySink"},
          "logRecords": [
            {
              "timeUnixNano": "1640995200000000000",
              "severityText": "INFO",
              "body": {"stringValue": "User logged in"},
              "attributes": [
                {"key": "userId", "value": {"stringValue": "user-456"}},
                {"key": "sessionId", "value": {"stringValue": "session-789"}},
                {"key": "loginTime", "value": {"stringValue": "2024-01-01T12:00:00Z"}},
                {"key": "success", "value": {"boolValue": true}}
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

## Benefits vs OpenTelemetry

### AlloySink Advantages
- ✅ Simpler API (no complex configuration)
- ✅ Direct control over log format
- ✅ Custom batching and retry logic
- ✅ Reduced dependencies
- ✅ Flexible attribute system for any metadata

### OpenTelemetry Advantages
- ✅ Standard protocol compliance
- ✅ Rich ecosystem and tooling
- ✅ Automatic instrumentation
- ✅ Multiple exporters support

## Error Handling

The library handles various error scenarios:

- **Connection failures** - Automatic retry with exponential backoff
- **HTTP errors** - Logged with response details
- **Serialization errors** - Graceful handling of malformed data
- **Timeout errors** - Configurable timeout with retry

## Thread Safety

AlloySink is thread-safe and can be used concurrently from multiple threads. Internal batching uses concurrent collections and proper synchronization.

## Disposal

Always dispose of AlloySink instances to ensure proper cleanup:

```csharp
using var alloySink = new AlloySink(options);
// or
alloySink.Dispose();
```

The dispose method will:
1. Stop background batching timer
2. Flush all pending logs
3. Clean up HTTP client resources

## Build and Development

### Building from Source

```bash
git clone https://github.com/DenebLab/AlloySink.git
cd AlloySink
./build.sh --target CI
```

### Build Targets

- `./build.sh` - Default build (Compile)
- `./build.sh --target Test` - Run all tests (54 tests ✅)
- `./build.sh --target Pack` - Create NuGet package
- `./build.sh --target CI` - Full pipeline (Clean + Test + Pack)

### Versioning

- **AbcVersion**: Semantic versioning with `abcversion -p semversion`
- **Base version**: Defined in `.abcversion.json` (currently 0.1.0)
- **Auto-increment**: Patch version increments automatically
- **Override**: Use `PACKAGE_VERSION` environment variable

### GitHub Actions

- **CI**: Runs on push to main/develop branches
- **Publishing**: Automatic NuGet publishing on production branch
- **Testing**: All 54 tests must pass before publishing

## Requirements

- .NET 8.0 or later
- Microsoft.Extensions.Logging.Abstractions 8.0.0+
- System.Text.Json (included in .NET 8.0)

## License

This project is licensed under the MIT License.