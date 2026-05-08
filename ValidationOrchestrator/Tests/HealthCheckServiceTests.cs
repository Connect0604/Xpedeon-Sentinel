namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

public class HealthCheckServiceTests
{
    private readonly IHealthCheckService _service = new HealthCheckService();

    [Fact]
    public async Task GetHealthAsync_ShouldReturnStatus()
    {
        var status = await _service.GetHealthAsync();
        status.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckComponentAsync_ShouldReturnComponentHealth()
    {
        var health = await _service.CheckComponentAsync("Database");
        health.Name.Should().Be("Database");
        health.Status.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IsReadyAsync_ShouldReturnBoolean()
    {
        var ready = await _service.IsReadyAsync();
        ready.Should().BeOfType<bool>();
    }

    [Fact]
    public void Configure_ShouldSetConfig()
    {
        var config = new HealthCheckConfig { CheckIntervalSeconds = 60 };
        _service.Configure(config);
        _service.GetConfiguration().CheckIntervalSeconds.Should().Be(60);
    }
}
