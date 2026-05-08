namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Implementation of system health monitoring
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly Dictionary<string, ComponentHealth> _componentHealth = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private HealthCheckConfig _config = new();

    public async Task<HealthStatus> GetHealthAsync()
    {
        var status = new HealthStatus
        {
            UptimeMs = (long)(DateTime.UtcNow - _startTime).TotalMilliseconds,
            Components = _componentHealth
        };

        var unhealthyCount = _componentHealth.Count(c => c.Value.Status == "Unhealthy");
        var degradedCount = _componentHealth.Count(c => c.Value.Status == "Degraded");

        if (unhealthyCount > 0)
        {
            status.Status = "Unhealthy";
            status.Messages.Add($"{unhealthyCount} components unhealthy");
        }
        else if (degradedCount > 0)
        {
            status.Status = "Degraded";
            status.Messages.Add($"{degradedCount} components degraded");
        }

        return status;
    }

    public async Task<ComponentHealth> CheckComponentAsync(string componentName)
    {
        var health = new ComponentHealth { Name = componentName };

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Simulate health check
            await Task.Delay(10);
            
            sw.Stop();
            health.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
            health.Status = health.ResponseTimeMs < 1000 ? "Healthy" : "Degraded";
        }
        catch (Exception ex)
        {
            health.Status = "Unhealthy";
            health.Message = ex.Message;
        }

        health.LastChecked = DateTime.UtcNow;
        _componentHealth[componentName] = health;

        return health;
    }

    public async Task<bool> IsReadyAsync()
    {
        var status = await GetHealthAsync();
        return status.Status == "Healthy";
    }

    public void Configure(HealthCheckConfig config)
    {
        _config = config;
    }

    public HealthCheckConfig GetConfiguration()
    {
        return _config;
    }
}
