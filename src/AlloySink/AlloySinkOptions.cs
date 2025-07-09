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
    
    // HTTP Client Configuration
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Allow bypassing SSL certificate validation. Only works when IsDebugMode is true for security.
    /// In production, this setting is ignored and certificates are always validated.
    /// </summary>
    public bool AcceptAnyCertificate { get; set; } = false;
    
    /// <summary>
    /// Indicates if the application is running in debug mode. Only when true can SSL certificate validation be bypassed.
    /// Should be set to false in production environments for security.
    /// </summary>
    public bool IsDebugMode { get; set; } = 
#if DEBUG
        true;
#else
        false;
#endif
    
    // Authentication Configuration
    public AlloySinkAuthorizationType AuthorizationType { get; set; } = AlloySinkAuthorizationType.None;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string CustomAuthorizationHeader { get; set; } = string.Empty;
}

public enum AlloySinkAuthorizationType
{
    None,
    Basic,
    Bearer,
    Custom
}