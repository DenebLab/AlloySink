namespace Deneblab.AlloySink;

public class AlloySinkOptions
{
    public string AlloyEndpoint { get; set; } = "http://localhost:4318";
    public string ServiceName { get; set; } = "scraper-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";
    public int BatchSize { get; set; } = 100;
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableBatching { get; set; } = true;
}