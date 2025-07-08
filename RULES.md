

## Project Overview

AlloySink is a .NET 8.0 C# library for sending structured logs directly to Grafana Alloy using OpenTelemetry Protocol (OTLP). It provides a simple API with batching, retry logic, and flexible attribute system for any logging scenario.

## Build System

This project uses **Nuke** as its build automation system with **Git-based versioning**.

### Build Commands

- **Primary build**: `./build.sh` (Linux/macOS) or `.\build.ps1` (Windows)
- **Build targets**:
  - `./build.sh --target Clean` - Clean build outputs
  - `./build.sh --target Restore` - Restore NuGet packages
  - `./build.sh --target Compile` - Build the solution (default)
  - `./build.sh --target Test` - Run unit tests
  - `./build.sh --target Pack` - Create NuGet package
  - `./build.sh --target CI` - Full CI pipeline (Clean + Test + Pack)

### Version Management

- **Git-based versioning**: `Major.Minor.{CommitCount}`
- Version automatically determined from Git commit history
- Assembly versions set during build with Git commit hash
- Example: `1.0.1+b5fe339`

### Project Structure

```
src/
├── AlloySink.sln          # Main solution file
├── AlloySink/             # Main library project
│   ├── AlloySink.csproj   # Library project with NuGet metadata
│   ├── AlloySink.cs       # Main logging class
│   ├── LogEntry.cs        # Log entry model
│   ├── OtlpLogFormatter.cs # OTLP formatting
│   ├── AlloyClient.cs     # HTTP client
│   └── AlloySinkOptions.cs # Configuration
├── AlloySink.Tests/       # Unit and integration tests
│   └── AlloySink.Tests.csproj
└── build/                 # Nuke build system
    ├── Build.cs           # Build targets and logic
    └── _build.csproj      # Build project
```

## Dependencies

- **Target Framework**: .NET 8.0
- **Main Dependencies**:
  - Microsoft.Extensions.Logging.Abstractions 8.0.0
- **Test Dependencies**:
  - xunit 2.4.2
  - Moq 4.20.69
- **Build Dependencies**:
  - Nuke.Common 9.0.4

## Library Design

### Core Features

- **Generic attribute system** - No domain-specific properties
- **OTLP protocol support** - Direct integration with Grafana Alloy
- **Batching and retry logic** - Configurable performance optimization
- **Type-aware serialization** - Proper OTLP value types (string, int, bool, double)
- **Exception handling** - Structured exception logging

### API Design

```csharp
// Simple logging
await alloySink.LogInfoAsync("Application started");

// Generic attributes
await alloySink.LogInfoAsync("User action", new Dictionary<string, object>
{
    { "userId", "user-123" },
    { "action", "login" },
    { "timestamp", DateTime.UtcNow },
    { "success", true }
});
```

## Development Workflow

1. **Local development**: Use default `./build.sh` (Compile target)
2. **Run tests**: `./build.sh --target Test`
3. **Create package**: `./build.sh --target Pack`
4. **Full validation**: `./build.sh --target CI`

## Output Directories

- `output/` - Build outputs and test results
- `artifacts/` - NuGet packages (.nupkg files)

## Testing

- **Comprehensive test suite** - Unit and integration tests
- **xunit framework** - Industry standard testing
- **Test coverage** - All core components tested
- **Mock dependencies** - Isolated unit testing

# Using Gemini CLI for Large Codebase Analysis
Use .claude\project-items\gemini.md