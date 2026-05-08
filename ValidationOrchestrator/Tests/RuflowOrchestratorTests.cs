namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

public class RuflowOrchestratorTests
{
    private readonly IRuflowOrchestrator _orchestrator = new RuflowOrchestrator();

    [Fact]
    public async Task InitializeAsync_ShouldCreateWorkflow()
    {
        var config = new WorkflowConfig();
        var context = await _orchestrator.InitializeAsync("session-1", config);

        context.Should().NotBeNull();
        context.SessionId.Should().Be("session-1");
        context.Phases.Should().HaveCount(6);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteAllPhases()
    {
        var config = new WorkflowConfig();
        await _orchestrator.InitializeAsync("session-1", config);

        var context = await _orchestrator.ExecuteAsync("session-1");

        context.Status.Should().Be("Completed");
        context.Phases.All(p => p.Status == "Completed").Should().BeTrue();
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnStatus()
    {
        var config = new WorkflowConfig();
        await _orchestrator.InitializeAsync("session-1", config);

        var context = await _orchestrator.GetStatusAsync("session-1");

        context.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task ExecutePhaseAsync_ShouldExecutePhase()
    {
        var config = new WorkflowConfig();
        await _orchestrator.InitializeAsync("session-1", config);

        var phase = await _orchestrator.ExecutePhaseAsync("session-1", "Discovery");

        phase.PhaseName.Should().Be("Discovery");
        phase.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetPhaseStatusAsync_ShouldReturnPhaseStatus()
    {
        var config = new WorkflowConfig();
        await _orchestrator.InitializeAsync("session-1", config);

        var phase = await _orchestrator.GetPhaseStatusAsync("session-1", "Discovery");

        phase.PhaseName.Should().Be("Discovery");
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelWorkflow()
    {
        var config = new WorkflowConfig();
        await _orchestrator.InitializeAsync("session-1", config);

        var result = await _orchestrator.CancelAsync("session-1");

        result.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetConfig()
    {
        var config = new WorkflowConfig { MaxRetries = 5 };
        _orchestrator.Configure(config);

        _orchestrator.GetConfiguration().MaxRetries.Should().Be(5);
    }
}
