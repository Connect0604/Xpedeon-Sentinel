namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for dashboard service (Form 9)
/// </summary>
public class DashboardServiceTests
{
    private readonly IDashboardService _service = new DashboardService();

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new DashboardService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeDashboardAsync_ShouldCreateDashboardState()
    {
        // Act
        var state = await _service.InitializeDashboardAsync("session-1", "client-1");

        // Assert
        state.Should().NotBeNull();
        state.SessionId.Should().Be("session-1");
        state.ClientId.Should().Be("client-1");
        state.PipelineStatus.Should().NotBeNull();
        state.PipelineStatus.Phases.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetDashboardStateAsync_ShouldReturnState()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        var state = await _service.GetDashboardStateAsync("session-1");

        // Assert
        state.Should().NotBeNull();
        state.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task GetDashboardStateAsync_WithNonExistentSession_ShouldReturnEmptyState()
    {
        // Act
        var state = await _service.GetDashboardStateAsync("nonexistent");

        // Assert
        state.Should().NotBeNull();
        state.SessionId.Should().Be("nonexistent");
    }

    [Fact]
    public async Task RefreshDashboardAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var originalTime = (await _service.GetDashboardStateAsync("session-1")).LastUpdated;

        // Act
        await Task.Delay(10);
        var refreshed = await _service.RefreshDashboardAsync("session-1");

        // Assert
        refreshed.LastUpdated.Should().BeAfter(originalTime);
    }

    [Fact]
    public async Task UpdatePhaseStatusAsync_ShouldUpdatePhase()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        await _service.UpdatePhaseStatusAsync("session-1", "Discovery", "InProgress", 50);

        // Assert
        var state = await _service.GetDashboardStateAsync("session-1");
        var phase = state.PipelineStatus.Phases.First(p => p.PhaseName == "Discovery");
        phase.Status.Should().Be("InProgress");
        phase.Progress.Should().Be(50);
    }

    [Fact]
    public async Task CompletePhaseAsync_ShouldCompletePhase()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        var completed = await _service.CompletePhaseAsync("session-1", "Discovery", 1000);

        // Assert
        completed.Status.Should().Be("Completed");
        completed.Progress.Should().Be(100);
        completed.DurationMs.Should().Be(1000);
        completed.EndTime.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordPhaseErrorAsync_ShouldRecordError()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        await _service.RecordPhaseErrorAsync("session-1", "Discovery", "Test error");

        // Assert
        var state = await _service.GetDashboardStateAsync("session-1");
        state.PipelineStatus.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AddAlertAsync_ShouldAddAlert()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        var alert = await _service.AddAlertAsync("session-1", "warning", "Test Alert", "Test message");

        // Assert
        alert.Should().NotBeNull();
        alert.Title.Should().Be("Test Alert");
        alert.Type.Should().Be("warning");
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldReturnAlerts()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        await _service.AddAlertAsync("session-1", "warning", "Alert 1", "Message 1");
        await _service.AddAlertAsync("session-1", "error", "Alert 2", "Message 2");

        // Act
        var alerts = await _service.GetAlertsAsync("session-1");

        // Assert
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldExcludeAcknowledgedByDefault()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var alert = await _service.AddAlertAsync("session-1", "warning", "Alert", "Message");
        await _service.AcknowledgeAlertAsync(alert.Id);

        // Act
        var alerts = await _service.GetAlertsAsync("session-1", false);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task AcknowledgeAlertAsync_ShouldAcknowledgeAlert()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var alert = await _service.AddAlertAsync("session-1", "warning", "Alert", "Message");

        // Act
        await _service.AcknowledgeAlertAsync(alert.Id);

        // Assert
        var allAlerts = await _service.GetAlertsAsync("session-1", true);
        allAlerts.First(a => a.Id == alert.Id).Acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task ClearAlertsAsync_ShouldClearAlerts()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        await _service.AddAlertAsync("session-1", "warning", "Alert", "Message");

        // Act
        await _service.ClearAlertsAsync("session-1");

        // Assert
        var alerts = await _service.GetAlertsAsync("session-1", true);
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProgressItemAsync_ShouldAddProgressItem()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        var item = await _service.AddProgressItemAsync("session-1", "Test Progress", "InProgress");

        // Assert
        item.Should().NotBeNull();
        item.Title.Should().Be("Test Progress");
        item.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnProgressItems()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        await _service.AddProgressItemAsync("session-1", "Item 1", "Completed");
        await _service.AddProgressItemAsync("session-1", "Item 2", "InProgress");

        // Act
        var items = await _service.GetProgressAsync("session-1");

        // Assert
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateProgressItemAsync_ShouldUpdateProgress()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var item = await _service.AddProgressItemAsync("session-1", "Item", "Pending");

        // Act
        await _service.UpdateProgressItemAsync(item.Id, "Completed", 5000);

        // Assert
        var items = await _service.GetProgressAsync("session-1");
        var updated = items.First(i => i.Id == item.Id);
        updated.Status.Should().Be("Completed");
        updated.DurationMs.Should().Be(5000);
    }

    [Fact]
    public async Task UpdateMetricsAsync_ShouldUpdateMetrics()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var risk = new MigrationRiskAssessment { OverallRiskScore = 45, OverallHealthScore = 55 };

        // Act
        await _service.UpdateMetricsAsync("session-1", null, risk);

        // Assert
        var metrics = await _service.GetMetricsAsync("session-1");
        metrics.RiskScore.Should().NotBeNull();
        metrics.RiskScore.Value.Should().Contain("45");
    }

    [Fact]
    public async Task GetExecutiveSnapshotAsync_ShouldReturnSnapshot()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");
        var risk = new MigrationRiskAssessment
        {
            OverallRiskScore = 30,
            OverallHealthScore = 70,
            Recommendation = new MigrationRecommendation { Decision = MigrationDecision.Go }
        };

        // Act
        await _service.UpdateExecutiveSnapshotAsync("session-1", risk);
        var snapshot = await _service.GetExecutiveSnapshotAsync("session-1");

        // Assert
        snapshot.Decision.Should().Be("Go");
        snapshot.OverallRiskScore.Should().Be(30);
    }

    [Fact]
    public async Task SendNotificationAsync_ShouldSendNotification()
    {
        // Act
        var notification = await _service.SendNotificationAsync(
            "user-1",
            "blocker",
            "Blocker Found",
            "Critical issue detected",
            5);

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be("user-1");
        notification.Title.Should().Be("Blocker Found");
        notification.Priority.Should().Be(5);
    }

    [Fact]
    public async Task GetNotificationsAsync_ShouldReturnNotifications()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Notification 1", "Message 1");
        await _service.SendNotificationAsync("user-1", "error", "Notification 2", "Message 2");

        // Act
        var notifications = await _service.GetNotificationsAsync("user-1");

        // Assert
        notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotificationsAsync_ShouldFilterUnreadOnly()
    {
        // Arrange
        var notif = await _service.SendNotificationAsync("user-1", "info", "Notification", "Message");
        await _service.MarkNotificationAsReadAsync(notif.Id);

        // Act
        var unread = await _service.GetNotificationsAsync("user-1", true);

        // Assert
        unread.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateUserSessionAsync_ShouldCreateSession()
    {
        // Act
        var session = await _service.CreateUserSessionAsync("user-1", "session-1");

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be("user-1");
        session.SessionId.Should().Be("session-1");
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSessionAsync_ShouldUpdateView()
    {
        // Arrange
        await _service.CreateUserSessionAsync("user-1", "session-1");

        // Act
        await _service.UpdateUserSessionAsync("user-1", "session-1", "technical");

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task EndUserSessionAsync_ShouldEndSession()
    {
        // Arrange
        await _service.CreateUserSessionAsync("user-1", "session-1");

        // Act
        await _service.EndUserSessionAsync("user-1");

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task BroadcastUpdateAsync_ShouldBroadcastUpdate()
    {
        // Arrange
        var update = new DashboardUpdate
        {
            SessionId = "session-1",
            EventType = "Test",
            Message = "Test message"
        };

        // Act
        await _service.BroadcastUpdateAsync(update);

        // Assert
        // Should complete without error
    }

    [Fact]
    public void SubscribeToUpdates_ShouldAddSubscriber()
    {
        // Arrange
        var called = false;
        async Task Callback(DashboardUpdate u)
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
        // Act
        _service.UnsubscribeFromUpdates("session-1");

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task ExportDashboardAsync_ShouldExport()
    {
        // Act
        var export = await _service.ExportDashboardAsync("session-1", "json");

        // Assert
        export.Should().NotBeNull();
        export.Format.Should().Be("json");
        export.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealth()
    {
        // Act
        var health = await _service.GetHealthAsync();

        // Assert
        health.Should().NotBeNull();
        health.Status.Should().Be("healthy");
        health.UptimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetActiveSessionsCountAsync_ShouldReturnCount()
    {
        // Arrange
        await _service.InitializeDashboardAsync("session-1", "client-1");

        // Act
        var count = await _service.GetActiveSessionsCountAsync();

        // Assert
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetServiceMetricsAsync_ShouldReturnMetrics()
    {
        // Act
        var metrics = await _service.GetServiceMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.ActiveDashboards.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new DashboardConfig { DarkMode = true };

        // Act
        _service.Configure(config);
        var retrieved = _service.GetConfiguration();

        // Assert
        retrieved.DarkMode.Should().BeTrue();
    }

    [Fact]
    public void ConfigureLiveUpdates_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new LiveUpdateConfig { UpdateIntervalMs = 500 };

        // Act
        _service.ConfigureLiveUpdates(config);
        var retrieved = _service.GetLiveUpdateConfig();

        // Assert
        retrieved.UpdateIntervalMs.Should().Be(500);
    }
}
