using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public class LogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, object> Attributes { get; set; } = new();
    public Exception? Exception { get; set; }
}