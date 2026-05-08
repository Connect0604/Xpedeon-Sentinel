namespace ValidationOrchestrator.Models;

/// <summary>
/// Configuration options for database connections
/// </summary>
public class DatabaseConnectionOptions
{
    public string LegacyConnectionString { get; set; } = string.Empty;
    public string BlazonConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 300; // 5 minutes
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
