using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deneblab.AlloySink;

public class AlloyClient : IAlloyClient
{
    private readonly HttpClient _httpClient;
    private readonly AlloySinkOptions _options;
    private readonly string _endpoint;
    private readonly ILogger<AlloyClient> _logger;

    public AlloyClient(AlloySinkOptions options, ILogger<AlloyClient> logger)
    {
        _options = options;
        _endpoint = $"{options.AlloyEndpoint.TrimEnd('/')}/v1/logs";
        _httpClient = CreateHttpClient(options);
        _logger = logger;
    }

    public async Task<bool> SendLogsAsync(IEnumerable<LogEntry> logEntries)
    {
        var logs = logEntries.ToList();
        if (!logs.Any()) return true;

        var jsonContent = OtlpLogFormatter.FormatLogs(logs);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        return await SendWithRetryAsync(content);
    }

    private async Task<bool> SendWithRetryAsync(HttpContent content)
    {
        var attempts = 0;

        while (attempts < _options.MaxRetries)
        {
            try
            {
                var response = await _httpClient.PostAsync(_endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogWarning("HTTP {StatusCode}: {ResponseContent}", response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send logs (attempt {Attempt}/{MaxRetries})", attempts + 1, _options.MaxRetries);
            }

            attempts++;

            if (attempts < _options.MaxRetries)
            {
                await Task.Delay(_options.RetryDelay);
            }
        }

        return false;
    }

    private HttpClient CreateHttpClient(AlloySinkOptions options)
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

        if (options.AlloyEndpoint.StartsWith("https", StringComparison.OrdinalIgnoreCase) && options.AcceptAnyCertificate && options.IsDebugMode)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            };
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.AlloyEndpoint),
            Timeout = options.RequestTimeout
        };

        ConfigureAuthentication(client, options);
        
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Log warning if SSL bypass is requested but not allowed due to production mode
        if (options.AcceptAnyCertificate && !options.IsDebugMode)
        {
            _logger.LogWarning("SSL certificate validation bypass requested but ignored in production mode for security");
        }
        
        return client;
    }

    private void ConfigureAuthentication(HttpClient client, AlloySinkOptions options)
    {
        switch (options.AuthorizationType)
        {
            case AlloySinkAuthorizationType.Basic when !string.IsNullOrEmpty(options.Username):
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
                break;

            case AlloySinkAuthorizationType.Bearer when !string.IsNullOrEmpty(options.Token):
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.Token);
                break;

            case AlloySinkAuthorizationType.Custom when !string.IsNullOrEmpty(options.CustomAuthorizationHeader):
                client.DefaultRequestHeaders.Add("Authorization", options.CustomAuthorizationHeader);
                break;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}