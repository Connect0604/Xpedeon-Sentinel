namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for notification service
/// </summary>
public class NotificationServiceTests
{
    private readonly INotificationService _service = new NotificationService();

    [Fact]
    public async Task SendNotificationAsync_ShouldCreateNotification()
    {
        // Act
        var notification = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be("user-1");
        notification.Type.Should().Be("info");
        notification.Title.Should().Be("Test");
    }

    [Fact]
    public async Task SendToSessionAsync_ShouldCreateSessionNotification()
    {
        // Act
        var notification = await _service.SendToSessionAsync("session-1", "warning", "Title", "Message");

        // Assert
        notification.Should().NotBeNull();
        notification.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task BroadcastAsync_ShouldSendToMultipleUsers()
    {
        // Arrange
        await _service.SubscribeAsync("user-1", "user1@test.com");
        await _service.SubscribeAsync("user-2", "user2@test.com");

        // Act
        var result = await _service.BroadcastAsync("info", "Broadcast", "Message");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetNotificationAsync_ShouldReturnNotification()
    {
        // Arrange
        var sent = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var retrieved = await _service.GetNotificationAsync(sent.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(sent.Id);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnUserNotifications()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test1", "Message1");
        await _service.SendNotificationAsync("user-1", "warning", "Test2", "Message2");
        await _service.SendNotificationAsync("user-2", "info", "Test3", "Message3");

        // Act
        var notifications = await _service.GetUserNotificationsAsync("user-1");

        // Assert
        notifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnUnreadCount()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test1", "Message1");
        await _service.SendNotificationAsync("user-1", "info", "Test2", "Message2");

        // Act
        var count = await _service.GetUnreadCountAsync("user-1");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead()
    {
        // Arrange
        var notification = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var result = await _service.MarkAsReadAsync(notification.Id);
        var retrieved = await _service.GetNotificationAsync(notification.Id);

        // Assert
        result.Should().BeTrue();
        retrieved.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllAsRead()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test1", "Message1");
        await _service.SendNotificationAsync("user-1", "info", "Test2", "Message2");

        // Act
        var result = await _service.MarkAllAsReadAsync("user-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldAcknowledgeNotification()
    {
        // Arrange
        var notification = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var result = await _service.AcknowledgeAsync(notification.Id);
        var retrieved = await _service.GetNotificationAsync(notification.Id);

        // Assert
        result.Should().BeTrue();
        retrieved.IsAcknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteNotificationAsync_ShouldDeleteNotification()
    {
        // Arrange
        var notification = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var result = await _service.DeleteNotificationAsync(notification.Id);
        var retrieved = await _service.GetNotificationAsync(notification.Id);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task CreateAlertAsync_ShouldCreateAlert()
    {
        // Act
        var alert = await _service.CreateAlertAsync("session-1", "critical", "Critical Issue", "Description");

        // Assert
        alert.Should().NotBeNull();
        alert.SessionId.Should().Be("session-1");
        alert.Type.Should().Be("critical");
    }

    [Fact]
    public async Task GetAlertAsync_ShouldReturnAlert()
    {
        // Arrange
        var alert = await _service.CreateAlertAsync("session-1", "warning", "Warning", "Description");

        // Act
        var retrieved = await _service.GetAlertAsync(alert.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(alert.Id);
    }

    [Fact]
    public async Task GetSessionAlertsAsync_ShouldReturnSessionAlerts()
    {
        // Arrange
        await _service.CreateAlertAsync("session-1", "critical", "Alert1", "Desc1");
        await _service.CreateAlertAsync("session-1", "warning", "Alert2", "Desc2");
        await _service.CreateAlertAsync("session-2", "critical", "Alert3", "Desc3");

        // Act
        var alerts = await _service.GetSessionAlertsAsync("session-1");

        // Assert
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnresolvedAlertsAsync_ShouldReturnUnresolvedAlerts()
    {
        // Arrange
        var alert1 = await _service.CreateAlertAsync("session-1", "critical", "Alert1", "Desc1");
        var alert2 = await _service.CreateAlertAsync("session-1", "warning", "Alert2", "Desc2");
        await _service.ResolveAlertAsync(alert1.Id);

        // Act
        var unresolved = await _service.GetUnresolvedAlertsAsync();

        // Assert
        unresolved.Should().Contain(a => a.Id == alert2.Id);
    }

    [Fact]
    public async Task ResolveAlertAsync_ShouldResolveAlert()
    {
        // Arrange
        var alert = await _service.CreateAlertAsync("session-1", "critical", "Alert", "Desc");

        // Act
        var result = await _service.ResolveAlertAsync(alert.Id);
        var retrieved = await _service.GetAlertAsync(alert.Id);

        // Assert
        result.Should().BeTrue();
        retrieved.IsResolved.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_ShouldCreateSubscriber()
    {
        // Act
        var subscriber = await _service.SubscribeAsync("user-1", "user1@test.com", "Admin");

        // Assert
        subscriber.Should().NotBeNull();
        subscriber.UserId.Should().Be("user-1");
        subscriber.Email.Should().Be("user1@test.com");
        subscriber.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldUnsubscribeUser()
    {
        // Arrange
        await _service.SubscribeAsync("user-1", "user1@test.com");

        // Act
        var result = await _service.UnsubscribeAsync("user-1");
        var subscriber = await _service.GetSubscriberAsync("user-1");

        // Assert
        result.Should().BeTrue();
        subscriber.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetSubscriberAsync_ShouldReturnSubscriber()
    {
        // Arrange
        await _service.SubscribeAsync("user-1", "user1@test.com");

        // Act
        var subscriber = await _service.GetSubscriberAsync("user-1");

        // Assert
        subscriber.Should().NotBeNull();
        subscriber.Email.Should().Be("user1@test.com");
    }

    [Fact]
    public async Task GetSubscribersAsync_ShouldReturnActiveSubscribers()
    {
        // Arrange
        await _service.SubscribeAsync("user-1", "user1@test.com");
        await _service.SubscribeAsync("user-2", "user2@test.com");

        // Act
        var subscribers = await _service.GetSubscribersAsync();

        // Assert
        subscribers.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSubscriberAsync_ShouldUpdateSubscriber()
    {
        // Arrange
        var subscriber = await _service.SubscribeAsync("user-1", "user1@test.com");
        subscriber.PhoneNumber = "555-1234";

        // Act
        var result = await _service.UpdateSubscriberAsync(subscriber);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetPreferencesAsync_ShouldReturnPreferences()
    {
        // Act
        var pref = await _service.GetPreferencesAsync("user-1");

        // Assert
        pref.Should().NotBeNull();
        pref.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ShouldUpdatePreferences()
    {
        // Arrange
        var pref = new NotificationPreference
        {
            UserId = "user-1",
            EmailNotifications = false
        };

        // Act
        var result = await _service.UpdatePreferencesAsync("user-1", pref);
        var retrieved = await _service.GetPreferencesAsync("user-1");

        // Assert
        result.Should().BeTrue();
        retrieved.EmailNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task SetChannelEnabledAsync_ShouldEnableChannel()
    {
        // Act
        var result = await _service.SetChannelEnabledAsync("user-1", "slack", true);
        var pref = await _service.GetPreferencesAsync("user-1");

        // Assert
        result.Should().BeTrue();
        pref.SlackNotifications.Should().BeTrue();
    }

    [Fact]
    public async Task SetQuietHoursAsync_ShouldSetQuietHours()
    {
        // Act
        var result = await _service.SetQuietHoursAsync("user-1", new TimeSpan(22, 0, 0), new TimeSpan(8, 0, 0));
        var pref = await _service.GetPreferencesAsync("user-1");

        // Assert
        result.Should().BeTrue();
        pref.QuietHoursStart.Should().Be(new TimeSpan(22, 0, 0));
    }

    [Fact]
    public async Task GetQueueSizeAsync_ShouldReturnQueueSize()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var size = await _service.GetQueueSizeAsync();

        // Assert
        size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnPendingNotifications()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test1", "Message1");
        await _service.SendNotificationAsync("user-1", "info", "Test2", "Message2");

        // Act
        var pending = await _service.GetPendingAsync();

        // Assert
        pending.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ProcessQueueAsync_ShouldProcessPending()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var result = await _service.ProcessQueueAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RetryFailedAsync_ShouldRetryFailedNotifications()
    {
        // Act
        var result = await _service.RetryFailedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetDeliveryHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var notification = await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var history = await _service.GetDeliveryHistoryAsync(notification.Id);

        // Assert
        history.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalSent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDeliveryRateAsync_ShouldReturnRate()
    {
        // Arrange
        await _service.SendNotificationAsync("user-1", "info", "Test", "Message");

        // Act
        var rate = await _service.GetDeliveryRateAsync();

        // Assert
        rate.Should().BeGreaterThanOrEqualTo(0);
        rate.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldCreateTemplate()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Name = "PhaseComplete",
            Type = "phase_completed",
            EmailSubject = "Phase Complete",
            EmailTemplate = "Phase {{phase}} completed"
        };

        // Act
        var created = await _service.CreateTemplateAsync(template);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTemplateAsync_ShouldReturnTemplate()
    {
        // Arrange
        var template = new NotificationTemplate
        {
            Name = "Test",
            Type = "test",
            EmailSubject = "Subject"
        };
        var created = await _service.CreateTemplateAsync(template);

        // Act
        var retrieved = await _service.GetTemplateAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(created.Id);
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new NotificationConfig { MaxQueueSize = 5000 };

        // Act
        _service.Configure(config);
        var retrieved = _service.GetConfiguration();

        // Assert
        retrieved.MaxQueueSize.Should().Be(5000);
    }
}
