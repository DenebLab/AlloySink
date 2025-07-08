using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public class AlloySink : IDisposable
{
    private readonly AlloySinkOptions _options;
    private readonly AlloyClient _client;
    private readonly ConcurrentQueue<LogEntry> _logQueue;
    private readonly Timer? _batchTimer;
    private readonly SemaphoreSlim _flushSemaphore;
    private volatile bool _disposed;

    public AlloySink(AlloySinkOptions options)
    {
        _options = options;
        _client = new AlloyClient(options);
        _logQueue = new ConcurrentQueue<LogEntry>();
        _flushSemaphore = new SemaphoreSlim(1, 1);

        if (_options.EnableBatching)
        {
            _batchTimer = new Timer(async _ => await FlushBatchAsync(), null, _options.BatchInterval, _options.BatchInterval);
        }
    }

    public async Task LogAsync(LogLevel level, string message, Dictionary<string, object>? attributes = null)
    {
        if (_disposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = message,
            ServiceName = _options.ServiceName,
            ServiceVersion = _options.ServiceVersion,
            Environment = _options.Environment,
            Attributes = attributes ?? new Dictionary<string, object>()
        };

        await LogEntryAsync(logEntry);
    }

    public async Task LogInfoAsync(string message, Dictionary<string, object>? attributes = null)
    {
        await LogAsync(LogLevel.Information, message, attributes);
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, object>? attributes = null)
    {
        if (_disposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Error,
            Message = message,
            ServiceName = _options.ServiceName,
            ServiceVersion = _options.ServiceVersion,
            Environment = _options.Environment,
            Attributes = attributes ?? new Dictionary<string, object>(),
            Exception = exception
        };

        await LogEntryAsync(logEntry);
    }

    public async Task LogWarningAsync(string message, Dictionary<string, object>? attributes = null)
    {
        await LogAsync(LogLevel.Warning, message, attributes);
    }

    public async Task LogDebugAsync(string message, Dictionary<string, object>? attributes = null)
    {
        await LogAsync(LogLevel.Debug, message, attributes);
    }

    private async Task LogEntryAsync(LogEntry logEntry)
    {
        if (_disposed) return;

        if (_options.EnableBatching)
        {
            _logQueue.Enqueue(logEntry);
            
            if (_logQueue.Count >= _options.BatchSize)
            {
                await FlushBatchAsync();
            }
        }
        else
        {
            await _client.SendLogsAsync(new[] { logEntry });
        }
    }

    public async Task FlushAsync()
    {
        if (_disposed) return;
        await FlushBatchAsync();
    }

    private async Task FlushBatchAsync()
    {
        if (_disposed) return;

        await _flushSemaphore.WaitAsync();
        try
        {
            var logsToSend = new List<LogEntry>();
            
            while (_logQueue.TryDequeue(out var logEntry) && logsToSend.Count < _options.BatchSize)
            {
                logsToSend.Add(logEntry);
            }

            if (logsToSend.Any())
            {
                await _client.SendLogsAsync(logsToSend);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        _batchTimer?.Dispose();
        
        FlushAsync().GetAwaiter().GetResult();
        
        _client?.Dispose();
        _flushSemaphore?.Dispose();
    }
}