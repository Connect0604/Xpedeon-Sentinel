namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for system health monitoring
/// </summary>
public interface IHealthCheckService
{
    Task<HealthStatus> GetHealthAsync();
    Task<ComponentHealth> CheckComponentAsync(string componentName);
    Task<bool> IsReadyAsync();
    void Configure(HealthCheckConfig config);
    HealthCheckConfig GetConfiguration();
}
