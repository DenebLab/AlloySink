using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public interface IAlloySink : IDisposable
{
    Task LogAsync(LogLevel level, string message, Dictionary<string, object>? attributes = null);
    Task LogInfoAsync(string message, Dictionary<string, object>? attributes = null);
    Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, object>? attributes = null);
    Task LogWarningAsync(string message, Dictionary<string, object>? attributes = null);
    Task LogDebugAsync(string message, Dictionary<string, object>? attributes = null);
    Task FlushAsync();
}