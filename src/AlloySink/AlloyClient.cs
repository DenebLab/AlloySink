using System.Text;

namespace Deneblab.AlloySink;

public class AlloyClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AlloySinkOptions _options;
    private readonly string _endpoint;

    public AlloyClient(AlloySinkOptions options)
    {
        _options = options;
        _endpoint = $"{options.AlloyEndpoint.TrimEnd('/')}/v1/logs";
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
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
                
                Console.WriteLine($"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send logs (attempt {attempts + 1}/{_options.MaxRetries}): {ex.Message}");
            }

            attempts++;
            
            if (attempts < _options.MaxRetries)
            {
                await Task.Delay(_options.RetryDelay);
            }
        }

        return false;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}