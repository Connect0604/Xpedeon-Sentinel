namespace ValidationOrchestrator.Models;

/// <summary>
/// System health status
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public long UptimeMs { get; set; }
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Individual component health
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public int ResponseTimeMs { get; set; }
    public string? Message { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfig
{
    public int CheckIntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
    public List<string> ComponentsToCheck { get; set; } = new();
}
