using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Deneblab.AlloySink;

public class AlloySink : IAlloySink, IAsyncDisposable
{
    private Task _backgroundProcessor;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IAlloyClient _client;
    private Channel<LogEntry> _logChannel;
    private ChannelWriter<LogEntry> _logWriter;
    private readonly AlloySinkOptions _options;
    private readonly ILogger<AlloySink> _logger;
    private volatile bool _disposed;

    public AlloySink(AlloySinkOptions options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _logger = loggerFactory.CreateLogger<AlloySink>();
        _client = new AlloyClient(options, loggerFactory.CreateLogger<AlloyClient>());
        _cancellationTokenSource = new CancellationTokenSource();

        InitializeChannel();
    }

    public AlloySink(AlloySinkOptions options, IAlloyClient alloyClient, ILogger<AlloySink> logger)
    {
        _options = options;
        _logger = logger;
        _client = alloyClient;
        _cancellationTokenSource = new CancellationTokenSource();

        InitializeChannel();
    }

    private void InitializeChannel()
    {
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _logChannel = Channel.CreateBounded<LogEntry>(channelOptions);
        _logWriter = _logChannel.Writer;

        _backgroundProcessor = Task.Run(ProcessLogsAsync, _cancellationTokenSource.Token);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _logWriter.Complete();
        }
        catch (ChannelClosedException)
        {
            // Channel already closed
        }
        catch (InvalidOperationException)
        {
            // Channel already closed
        }

        await _cancellationTokenSource.CancelAsync();

        try
        {
            // Wait for background processor with timeout to prevent deadlock
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _backgroundProcessor.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled or timeout
        }
        catch (TimeoutException)
        {
            // Timeout reached, continue with cleanup
        }

        _client?.Dispose();
        _cancellationTokenSource?.Dispose();
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

    public async Task LogErrorAsync(string message, Exception? exception = null,
        Dictionary<string, object>? attributes = null)
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

        try
        {
            await _logWriter.WriteAsync(logEntry, _cancellationTokenSource.Token);
        }
        catch (InvalidOperationException)
        {
            // Channel was closed, ignore
        }
    }

    public async Task FlushAsync()
    {
        if (_disposed) return;

        try
        {
            _logWriter.Complete();
            await _backgroundProcessor;
        }
        catch (ChannelClosedException)
        {
            // Channel already closed
        }
        catch (InvalidOperationException)
        {
            // Channel already closed
        }
    }

    private async Task ProcessLogsAsync()
    {
        var reader = _logChannel.Reader;
        var batch = new List<LogEntry>();
        var lastSendTime = DateTime.UtcNow;

        try
        {
            while (await reader.WaitToReadAsync(_cancellationTokenSource.Token))
                while (reader.TryRead(out var logEntry))
                {
                    batch.Add(logEntry);

                    var shouldSendBatch = !_options.EnableBatching ||
                                          batch.Count >= _options.BatchSize ||
                                          DateTime.UtcNow - lastSendTime >= _options.BatchInterval;

                    if (shouldSendBatch && batch.Count > 0)
                    {
                        await _client.SendLogsAsync(batch.ToArray());
                        batch.Clear();
                        lastSendTime = DateTime.UtcNow;
                    }
                }

            // Send remaining logs when channel is completed
            if (batch.Count > 0) await _client.SendLogsAsync(batch.ToArray());
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }
}