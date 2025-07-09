namespace Deneblab.AlloySink;

public interface IAlloyClient : IDisposable
{
    Task<bool> SendLogsAsync(IEnumerable<LogEntry> logEntries);
}