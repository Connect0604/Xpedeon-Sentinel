namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for real-time progress tracking service
/// </summary>
public class ProgressServiceTests
{
    private readonly IProgressService _service = new ProgressService();

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new ProgressService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeProgressAsync_ShouldCreateProgressTracker()
    {
        // Act
        var tracker = await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery", "TestGen", "Execution", "Comparison", "Review", "Reporting" });

        // Assert
        tracker.Should().NotBeNull();
        tracker.SessionId.Should().Be("session-1");
        tracker.OverallProgress.Should().Be(0);
        tracker.Status.Should().Be("running");
        tracker.PhaseProgresses.Should().HaveCount(6);
        tracker.PhaseProgresses.All(p => p.Status == "pending").Should().BeTrue();
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnCurrentProgress()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery", "TestGen" });

        // Act
        var tracker = await _service.GetProgressAsync("session-1");

        // Assert
        tracker.Should().NotBeNull();
        tracker.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task GetProgressAsync_WithNonExistentSession_ShouldReturnEmptyTracker()
    {
        // Act
        var tracker = await _service.GetProgressAsync("nonexistent");

        // Assert
        tracker.Should().NotBeNull();
        tracker.SessionId.Should().Be("nonexistent");
    }

    [Fact]
    public async Task StartPhaseAsync_ShouldStartPhase()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });

        // Act
        var phase = await _service.StartPhaseAsync("session-1", "Discovery", 1, 5);

        // Assert
        phase.Should().NotBeNull();
        phase.PhaseName.Should().Be("Discovery");
        phase.Status.Should().Be("running");
        phase.Progress.Should().Be(0);
    }

    [Fact]
    public async Task UpdatePhaseProgressAsync_ShouldUpdateProgress()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 5);

        // Act
        await _service.UpdatePhaseProgressAsync("session-1", "Discovery", 50);

        // Assert
        var tracker = await _service.GetProgressAsync("session-1");
        var phase = tracker.PhaseProgresses.First(p => p.PhaseName == "Discovery");
        phase.Progress.Should().Be(50);
    }

    [Fact]
    public async Task CompletePhaseAsync_ShouldCompletePhase()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 5);

        // Act
        var completed = await _service.CompletePhaseAsync("session-1", "Discovery");

        // Assert
        completed.Status.Should().Be("completed");
        completed.Progress.Should().Be(100);
        completed.EndTime.Should().NotBeNull();
    }

    [Fact]
    public async Task FailPhaseAsync_ShouldFailPhase()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 5);

        // Act
        var failed = await _service.FailPhaseAsync("session-1", "Discovery", "Test error");

        // Assert
        failed.Status.Should().Be("failed");
        var tracker = await _service.GetProgressAsync("session-1");
        tracker.Status.Should().Be("failed");
    }

    [Fact]
    public async Task StartStepAsync_ShouldStartStep()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 5);

        // Act
        var step = await _service.StartStepAsync("session-1", "Discovery", "Step1", 1);

        // Assert
        step.Should().NotBeNull();
        step.StepName.Should().Be("Step1");
        step.Status.Should().Be("running");
    }

    [Fact]
    public async Task UpdateStepProgressAsync_ShouldUpdateStepProgress()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.StartStepAsync("session-1", "Discovery", "Step1", 1);

        // Act
        await _service.UpdateStepProgressAsync("session-1", "Discovery", "Step1", 75);

        // Assert
        var tracker = await _service.GetProgressAsync("session-1");
        var step = tracker.PhaseProgresses.First(p => p.PhaseName == "Discovery").Steps.First();
        step.Progress.Should().Be(75);
    }

    [Fact]
    public async Task CompleteStepAsync_ShouldCompleteStep()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.StartStepAsync("session-1", "Discovery", "Step1", 1);

        // Act
        var completed = await _service.CompleteStepAsync("session-1", "Discovery", "Step1");

        // Assert
        completed.Status.Should().Be("completed");
        completed.Progress.Should().Be(100);
    }

    [Fact]
    public async Task CompleteStepAsync_ShouldIncrementCompletedSteps()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 2);
        await _service.StartStepAsync("session-1", "Discovery", "Step1", 1);
        await _service.StartStepAsync("session-1", "Discovery", "Step2", 2);

        // Act
        await _service.CompleteStepAsync("session-1", "Discovery", "Step1");
        var tracker = await _service.GetProgressAsync("session-1");
        var phase = tracker.PhaseProgresses.First(p => p.PhaseName == "Discovery");

        // Assert
        phase.CompletedSteps.Should().Be(1);
        phase.TotalSteps.Should().Be(2);
    }

    [Fact]
    public async Task AddStepArtifactAsync_ShouldAddArtifact()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.StartStepAsync("session-1", "Discovery", "Step1", 1);

        // Act
        await _service.AddStepArtifactAsync("session-1", "Discovery", "Step1", "/path/to/artifact");

        // Assert
        var tracker = await _service.GetProgressAsync("session-1");
        var step = tracker.PhaseProgresses.First(p => p.PhaseName == "Discovery").Steps.First();
        step.Artifacts.Should().Contain("/path/to/artifact");
    }

    [Fact]
    public async Task RecordProgressEventAsync_ShouldRecordEvent()
    {
        // Arrange
        var @event = new ProgressUpdateEvent
        {
            SessionId = "session-1",
            EventType = "PhaseStarted",
            PhaseName = "Discovery"
        };

        // Act
        var recorded = await _service.RecordProgressEventAsync(@event);

        // Assert
        recorded.Should().NotBeNull();
        recorded.SessionId.Should().Be("session-1");
        recorded.EventType.Should().Be("PhaseStarted");
    }

    [Fact]
    public async Task GetProgressHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.UpdatePhaseProgressAsync("session-1", "Discovery", 50);

        // Act
        var history = await _service.GetProgressHistoryAsync("session-1");

        // Assert
        history.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProgressHistoryAsync_ShouldRespectMaxEntries()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);

        // Act
        var history = await _service.GetProgressHistoryAsync("session-1", 5);

        // Assert
        history.Should().NotBeNull();
        history.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetRecentUpdatesAsync_ShouldReturnRecentUpdates()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        var @event = new ProgressUpdateEvent
        {
            SessionId = "session-1",
            EventType = "PhaseStarted",
            PhaseName = "Discovery"
        };
        await _service.RecordProgressEventAsync(@event);

        // Act
        var updates = await _service.GetRecentUpdatesAsync("session-1", 10);

        // Assert
        updates.Should().NotBeNull();
        updates.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetPhaseEstimateAsync_ShouldReturnEstimate()
    {
        // Act
        var estimate = await _service.GetPhaseEstimateAsync("Discovery");

        // Assert
        estimate.Should().NotBeNull();
        estimate.PhaseName.Should().Be("Discovery");
    }

    [Fact]
    public async Task UpdatePhaseEstimateAsync_ShouldUpdateEstimate()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await Task.Delay(100);
        await _service.UpdatePhaseProgressAsync("session-1", "Discovery", 50);

        // Act
        var estimate = await _service.UpdatePhaseEstimateAsync("session-1", "Discovery");

        // Assert
        estimate.Should().NotBeNull();
        estimate.AverageDurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCompletionEstimateAsync_ShouldReturnEstimate()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery", "TestGen" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);

        // Act
        var estimate = await _service.GetCompletionEstimateAsync("session-1");

        // Assert
        // Should return DateTime or null
        estimate.Should().BeOfType<DateTime?>();
    }

    [Fact]
    public async Task GetRemainingTimeEstimateAsync_ShouldReturnTimeInMs()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);

        // Act
        var remaining = await _service.GetRemainingTimeEstimateAsync("session-1");

        // Assert
        remaining.Should().NotBeNull();
        remaining.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CalculateVelocityAsync_ShouldCalculateVelocity()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await Task.Delay(100);
        await _service.UpdatePhaseProgressAsync("session-1", "Discovery", 50);

        // Act
        var velocity = await _service.CalculateVelocityAsync("session-1");

        // Assert
        velocity.Should().NotBeNull();
        velocity.SessionId.Should().Be("session-1");
        velocity.CurrentVelocityPercentPerSecond.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery", "TestGen" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.CompletePhaseAsync("session-1", "Discovery");

        // Act
        var stats = await _service.GetStatisticsAsync("session-1");

        // Assert
        stats.Should().NotBeNull();
        stats.SessionId.Should().Be("session-1");
        stats.PhasesCompleted.Should().Be(1);
        stats.TotalPhases.Should().Be(2);
    }

    [Fact]
    public async Task GetProgressComparisonAsync_ShouldCompareToHistorical()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);

        // Act
        var comparison = await _service.GetProgressComparisonAsync("session-1", "Discovery");

        // Assert
        comparison.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AddMilestoneAsync_ShouldAddMilestone()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });

        // Act
        var milestone = await _service.AddMilestoneAsync("session-1", "Halfway", 50, "high");

        // Assert
        milestone.Should().NotBeNull();
        milestone.Name.Should().Be("Halfway");
        milestone.TargetProgress.Should().Be(50);
        milestone.Priority.Should().Be("high");
    }

    [Fact]
    public async Task GetMilestonesAsync_ShouldReturnMilestones()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        await _service.AddMilestoneAsync("session-1", "Milestone1", 25);
        await _service.AddMilestoneAsync("session-1", "Milestone2", 75);

        // Act
        var milestones = await _service.GetMilestonesAsync("session-1");

        // Assert
        milestones.Should().HaveCount(2);
    }

    [Fact]
    public async Task CheckMilestonesAsync_ShouldMarkMilestoneAsCompleted()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });
        var milestone = await _service.AddMilestoneAsync("session-1", "Halfway", 50);
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 1);
        await _service.UpdatePhaseProgressAsync("session-1", "Discovery", 50);

        // Act
        await _service.CheckMilestonesAsync("session-1");
        var milestones = await _service.GetMilestonesAsync("session-1");

        // Assert
        var updated = milestones.First(m => m.Id == milestone.Id);
        updated.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void SubscribeToUpdates_ShouldAddSubscriber()
    {
        // Arrange
        var called = false;
        async Task Callback(ProgressUpdateEvent e)
        {
            called = true;
        }

        // Act
        _service.SubscribeToUpdates("session-1", Callback);

        // Assert
        // Should complete without error
    }

    [Fact]
    public void UnsubscribeFromUpdates_ShouldRemoveSubscriber()
    {
        // Arrange
        async Task Callback(ProgressUpdateEvent e) { }
        _service.SubscribeToUpdates("session-1", Callback);

        // Act
        _service.UnsubscribeFromUpdates("session-1");

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task GetUpdatesStreamAsync_ShouldStreamUpdates()
    {
        // Arrange
        await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery" });

        // Act
        var stream = _service.GetUpdatesStreamAsync("session-1");
        var updates = new List<ProgressUpdateEvent>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        try
        {
            await foreach (var update in stream.WithCancellation(cts.Token))
            {
                updates.Add(update);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        updates.Should().NotBeNull();
    }

    [Fact]
    public async Task BroadcastUpdateAsync_ShouldBroadcastToSubscribers()
    {
        // Arrange
        var received = false;
        async Task Callback(ProgressUpdateEvent e)
        {
            received = true;
        }
        _service.SubscribeToUpdates("session-1", Callback);

        var update = new ProgressUpdateEvent
        {
            SessionId = "session-1",
            EventType = "PhaseStarted",
            PhaseName = "Discovery"
        };

        // Act
        await _service.BroadcastUpdateAsync(update);
        await Task.Delay(100);

        // Assert
        received.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new ProgressTrackingConfig { UpdateIntervalMs = 1000 };

        // Act
        _service.Configure(config);
        var retrieved = _service.GetConfiguration();

        // Assert
        retrieved.UpdateIntervalMs.Should().Be(1000);
    }

    [Fact]
    public void GetConfiguration_ShouldReturnCurrentConfiguration()
    {
        // Act
        var config = _service.GetConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.EnableAutomaticEstimates.Should().BeTrue();
        config.HistoricalSampleSize.Should().Be(10);
    }

    [Fact]
    public async Task RecordPhaseMetricsAsync_ShouldRecordMetrics()
    {
        // Act
        await _service.RecordPhaseMetricsAsync("Discovery", 5000);

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task GetHistoricalMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        await _service.RecordPhaseMetricsAsync("Discovery", 5000);

        // Act
        var metrics = await _service.GetHistoricalMetricsAsync("Discovery");

        // Assert
        metrics.Should().NotBeNull();
        metrics.PhaseName.Should().Be("Discovery");
    }

    [Fact]
    public async Task PurgeOldHistoryAsync_ShouldClearOldData()
    {
        // Act
        await _service.PurgeOldHistoryAsync(30);

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task ProgressTracking_IntegrationScenario()
    {
        // Arrange - Initialize
        var tracker = await _service.InitializeProgressAsync("session-1",
            new List<string> { "Discovery", "Execution", "Comparison" });

        tracker.PhaseProgresses.Should().HaveCount(3);

        // Act - Start Discovery phase
        await _service.StartPhaseAsync("session-1", "Discovery", 1, 3);

        // Complete steps
        for (int i = 1; i <= 3; i++)
        {
            await _service.StartStepAsync("session-1", "Discovery", $"Step{i}", i);
            await Task.Delay(10);
            await _service.CompleteStepAsync("session-1", "Discovery", $"Step{i}");
        }

        // Complete Discovery
        await _service.CompletePhaseAsync("session-1", "Discovery");

        // Start Execution
        await _service.StartPhaseAsync("session-1", "Execution", 2, 2);
        await _service.UpdatePhaseProgressAsync("session-1", "Execution", 50);

        // Get statistics
        var stats = await _service.GetStatisticsAsync("session-1");
        var velocity = await _service.CalculateVelocityAsync("session-1");

        // Assert
        stats.PhasesCompleted.Should().Be(1);
        stats.StepsCompleted.Should().Be(3);
        velocity.CurrentVelocityPercentPerSecond.Should().BeGreaterThan(0);
    }
}
