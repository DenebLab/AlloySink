using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace RobeNova.Modules.LokiPusher;

public class LokiPusherService : IDisposable
{
    private const int MaxBatchSize = 100;
    private static readonly TimeSpan _maxBatchDelay = TimeSpan.FromSeconds(5);
    private readonly ArrayBufferWriter<byte> _bufferWriter;
    private readonly Channel<LokiLogItem> _channel;
    private readonly HttpClient _httpClient;
    private readonly JsonWriterOptions _jsonWriterOptions;
    private readonly ILogger<LokiPusherService> _log;
    private readonly LokiPusherOptions _pusherOptions;
    private readonly CancellationTokenSource _processingCts;
    private readonly Task _processingTask;

    public LokiPusherService(ILogger<LokiPusherService> log, LokiPusherOptions pusherOptions)
    {
        _log = log;
        _pusherOptions = pusherOptions;
        _bufferWriter = new ArrayBufferWriter<byte>();
        _jsonWriterOptions = new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = true
        };

        _httpClient = CreateHttpClient(pusherOptions);

        // Create an unbounded channel to ensure we never block writers
        _channel = Channel.CreateUnbounded<LokiLogItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start the processing task
        _processingCts = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessLogsAsync(_processingCts.Token));
    }

    public void Dispose()
    {
        try
        {
            _processingCts.Cancel();
            _processingTask.Wait(TimeSpan.FromSeconds(30)); // Give it 30 seconds to finish
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error during LokiPusher shutdown");
        }
        finally
        {
            _processingCts.Dispose();
            _httpClient.Dispose();
            _channel.Writer.Complete();
        }
    }

    public async Task WriteLogAsync(LokiLogItem logItem, CancellationToken cancellationToken = default)
    {
        try
        {
            await _channel.Writer.WriteAsync(logItem, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to write log item to channel");
        }
    }

    private async Task ProcessLogsAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var batch = new List<LokiLogItem>();

                try
                {
                    // Wait for the first item
                    if (!await _channel.Reader.WaitToReadAsync(stoppingToken)) break;

                    // Read the first item
                    if (_channel.Reader.TryRead(out var firstItem)) batch.Add(firstItem);

                    // Start a timeout task
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(_maxBatchDelay);

                    // Keep reading until we hit batch size or timeout
                    try
                    {
                        while (batch.Count < MaxBatchSize)
                        {
                            if (!await _channel.Reader.WaitToReadAsync(cts.Token)) break;

                            if (_channel.Reader.TryRead(out var item)) batch.Add(item);
                        }
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Timeout reached, continue with current batch
                    }

                    if (batch.Count > 0) await PushAsync(batch, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _log.LogError(ex, "Error processing log batch");
                    await Task.Delay(5000, stoppingToken); // Back off on error
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
            _log.LogInformation("LokiPusher service stopping");
        }
    }

    private HttpClient CreateHttpClient(LokiPusherOptions pusherOptions)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = 10,
            EnableMultipleHttp2Connections = true,
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
        };

        if (pusherOptions.Address.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true // Accept any certificate for development
            };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(pusherOptions.Address),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Configure authentication
        ConfigureAuthentication(client, pusherOptions);

        // Add common headers
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(pusherOptions.Application))
            client.DefaultRequestHeaders.Add("X-Scope-OrgID", pusherOptions.Application);

        return client;
    }

    private void ConfigureAuthentication(HttpClient client, LokiPusherOptions pusherOptions)
    {
        switch (pusherOptions.PusherAuthorizationType)
        {
            case LokiPusherAuthorizationType.Basic when !string.IsNullOrEmpty(pusherOptions.Username):
                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{pusherOptions.Username}:{pusherOptions.Password}"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
                break;

            case LokiPusherAuthorizationType.Bearer when !string.IsNullOrEmpty(pusherOptions.Token):
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", pusherOptions.Token);
                break;

            case LokiPusherAuthorizationType.Custom when !string.IsNullOrEmpty(pusherOptions.CustomAuthorizationHeader):
                client.DefaultRequestHeaders.Add("Authorization", pusherOptions.CustomAuthorizationHeader);
                break;
        }
    }

    private async Task PushAsync(IEnumerable<LokiLogItem> items, CancellationToken cancellationToken)
    {
        try
        {
            var writer = new Utf8JsonWriter(_bufferWriter, _jsonWriterOptions);

            writer.WriteStartObject();
            writer.WriteStartArray("streams");

            foreach (var item in items)
            {
                writer.WriteStartObject();

                // Labels
                writer.WriteStartObject("stream");
                // Add default labels from pusherOptions
                writer.WriteString("app", _pusherOptions.Application);
                writer.WriteString("instance", _pusherOptions.Instance);
                writer.WriteString("machine", _pusherOptions.MachineName);
                if (!string.IsNullOrEmpty(_pusherOptions.Tags)) writer.WriteString("tags", _pusherOptions.Tags);

                // Add custom labels
                foreach (var label in _pusherOptions.CustomLabels) writer.WriteString(label.Key, label.Value);

                // Add item-specific labels
                foreach (var label in item.Labels) writer.WriteString(label.Key, label.Value);
                writer.WriteEndObject();

                // Values
                writer.WriteStartArray("values");
                writer.WriteStartArray();

                var timestampNs = item.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;
                writer.WriteStringValue(timestampNs.ToString());

                var message = item.Payload switch
                {
                    string s => s,
                    _ => JsonSerializer.Serialize(item.Payload)
                };
                writer.WriteStringValue(message);

                writer.WriteEndArray();
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            await writer.FlushAsync(cancellationToken);

            var content = new ByteArrayContent(_bufferWriter.WrittenMemory.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            await TryPost(content, cancellationToken);
        }
        finally
        {
            _bufferWriter.Clear();
        }
    }

    private async Task TryPost(ByteArrayContent content, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/loki/api/v1/push")
            {
                Content = content
            };

            using var response =
                await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _log.ZLogInformation($"ContentLength: {content.Headers.ContentLength}, Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _log.ZLogWarning($"Request failed with status code: {response.StatusCode}. Response: {responseBody}");
            }
        }
        catch (HttpRequestException ex)
        {
            _log.ZLogError($"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _log.ZLogError($"Request timed out: {ex.Message}");
        }
        catch (Exception ex)
        {
            _log.ZLogError($"Unexpected error: {ex.Message}");
        }
        finally
        {
            content.Dispose();
        }
    }

   
}



public enum LokiPusherType
{
    Json,
    Protobuf
}

public enum LokiPusherAuthorizationType
{
    None, // No authorization
    Basic, // Basic Authentication
    Bearer, // Bearer Token Authentication
    Custom // Custom Authorization
}

public class LokiPusherOptions
{
    public LokiPusherType PusherType { get; set; } = LokiPusherType.Protobuf;
    public string Address { get; set; } = string.Empty;
    public string MachineName { get; set; } = Environment.MachineName;
    public string Instance { get; set; } = Guid.CreateVersion7().ToString();
    public string Application { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool CacheLabels { get; set; } = true;

    // Authorization options
    public LokiPusherAuthorizationType PusherAuthorizationType { get; set; } = LokiPusherAuthorizationType.None;
    public string Username { get; set; } = string.Empty; // For Basic Authentication
    public string Password { get; set; } = string.Empty; // For Basic Authentication
    public string Token { get; set; } = string.Empty; // For Bearer Token or Custom

    public string CustomAuthorizationHeader { get; set; } = string.Empty; // For custom authorization schemes
    // private readonly List<KeyValuePair<string, string>> _labelsPairs = [];

    public List<KeyValuePair<string, string>> CustomLabels { get; } = []; // For Bearer Token or Custom

   
}

public class LokiLogItem
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public object Payload { get; set; } = default;
    public Dictionary<string, string> Labels { get; set; } = new();
}