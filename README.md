# AlloySink

A lightweight .NET 8.0 C# library for sending structured logs directly to Grafana Alloy using OpenTelemetry Protocol (OTLP). Provides a simple API with batching, retry logic, and flexible attribute system for any logging scenario.

## Features

- **Direct OTLP integration** - Send logs directly to Grafana Alloy (port 4318)
- **Multiple authentication types** - Basic, Bearer, and Custom authentication support
- **SSL/TLS security** - Production-safe certificate validation with debug bypass
- **Async disposal pattern** - Proper resource cleanup with timeout protection
- **HTTP/2 optimization** - Connection pooling and keep-alive settings
- **Flexible attributes** - Generic `Dictionary<string, object>` for any data
- **Batch processing** - Configurable batch size and intervals
- **Retry logic** - Robust error handling with exponential backoff
- **Thread-safe design** - Concurrent logging support
- **Comprehensive testing** - 59 tests covering all scenarios ✅
- **Production ready** - Used in production environments

## Quick Start

### Installation

Install from NuGet:

```bash
dotnet add package Deneblab.AlloySink
```

Or via Package Manager:
```powershell
Install-Package Deneblab.AlloySink
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

## Authentication & Security

### Authentication Types

AlloySink supports multiple authentication methods:

#### Basic Authentication
```csharp
var options = new AlloySinkOptions
{
    AlloyEndpoint = "https://alloy.example.com:4318",
    AuthorizationType = AlloySinkAuthorizationType.Basic,
    Username = "your-username",
    Password = "your-password"
};
```

#### Bearer Token Authentication
```csharp
var options = new AlloySinkOptions
{
    AlloyEndpoint = "https://alloy.example.com:4318",
    AuthorizationType = AlloySinkAuthorizationType.Bearer,
    Token = "your-bearer-token"
};
```

#### Custom Authentication Header
```csharp
var options = new AlloySinkOptions
{
    AlloyEndpoint = "https://alloy.example.com:4318",
    AuthorizationType = AlloySinkAuthorizationType.Custom,
    CustomAuthorizationHeader = "ApiKey your-api-key"
};
```

### SSL Certificate Configuration

For production environments, SSL certificates are always validated. For development/testing scenarios, you can bypass certificate validation:

```csharp
var options = new AlloySinkOptions
{
    AlloyEndpoint = "https://localhost:4318",
    AcceptAnyCertificate = true,  // Only works when IsDebugMode = true
    IsDebugMode = true            // Set to false in production
};
```

⚠️ **Security Note**: SSL certificate bypass only works when `IsDebugMode = true`. In production builds, certificates are always validated regardless of the `AcceptAnyCertificate` setting.

### HTTP Client Configuration

AlloySink uses an optimized HTTP client with the following features:

- **HTTP/2 Support**: Enabled by default for better performance
- **Connection Pooling**: Connections are reused for 10 minutes
- **Keep-Alive**: Optimized ping settings for persistent connections
- **Timeout Protection**: Configurable request timeout (default: 30 seconds)

```csharp
var options = new AlloySinkOptions
{
    AlloyEndpoint = "https://alloy.example.com:4318",
    RequestTimeout = TimeSpan.FromSeconds(60),  // Custom timeout
    MaxRetries = 5,                             // Retry failed requests
    RetryDelay = TimeSpan.FromSeconds(2)        // Delay between retries
};
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
| `RequestTimeout` | `30 seconds` | HTTP request timeout |
| `AcceptAnyCertificate` | `false` | Allow invalid SSL certificates (debug only) |
| `IsDebugMode` | `true` (debug) / `false` (release) | Enable debug features |
| `AuthorizationType` | `None` | Authentication method (None/Basic/Bearer/Custom) |
| `Username` | `""` | Username for Basic authentication |
| `Password` | `""` | Password for Basic authentication |
| `Token` | `""` | Token for Bearer authentication |
| `CustomAuthorizationHeader` | `""` | Custom authorization header value |

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

AlloySink implements both `IDisposable` and `IAsyncDisposable` patterns for proper resource cleanup:

### Synchronous Disposal
```csharp
using var alloySink = new AlloySink(options);
// Automatic disposal when scope ends

// Or manual disposal
alloySink.Dispose();
```

### Asynchronous Disposal (Recommended)
```csharp
await using var alloySink = new AlloySink(options);
// Automatic async disposal when scope ends

// Or manual async disposal
await alloySink.DisposeAsync();
```

### Disposal Process

The disposal process includes:
1. **Stop log acceptance** - New logs are ignored
2. **Complete the channel** - Signal background processor to finish
3. **Cancel background processing** - Stop with timeout protection
4. **Flush pending logs** - Send any remaining batched logs
5. **Clean up resources** - Dispose HTTP client and cancellation tokens

**Timeout Protection**: The async disposal waits up to 10 seconds for background processing to complete before forcing cleanup, preventing deadlocks.

⚠️ **Important**: Always dispose AlloySink instances to ensure all logs are sent and resources are properly cleaned up.

## Build and Development

### Building from Source

```bash
git clone https://github.com/DenebLab/AlloySink.git
cd AlloySink
./build.sh --target CI
```

### Build Targets

- `./build.sh` - Default build (Compile)
- `./build.sh --target Test` - Run all tests (59 tests ✅)
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
- **Testing**: All 59 tests must pass before publishing

## Requirements

- .NET 8.0 or later
- Microsoft.Extensions.Logging.Abstractions 8.0.0+
- System.Text.Json (included in .NET 8.0)

## License

This project is licensed under the MIT License.