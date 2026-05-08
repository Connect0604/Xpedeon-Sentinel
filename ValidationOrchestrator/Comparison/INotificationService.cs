namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for notification distribution and management
/// Provides alert delivery, subscription management, and delivery tracking
/// </summary>
public interface INotificationService
{
    // Notifications
    Task<Notification> SendNotificationAsync(string userId, string type, string title, string message, int priority = 1);
    Task<Notification> SendToSessionAsync(string sessionId, string type, string title, string message);
    Task<bool> BroadcastAsync(string type, string title, string message, List<string>? userIds = null);
    Task<Notification?> GetNotificationAsync(string notificationId);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> AcknowledgeAsync(string notificationId);
    Task<bool> DeleteNotificationAsync(string notificationId);

    // Alerts
    Task<Alert> CreateAlertAsync(string sessionId, string type, string title, string description);
    Task<Alert?> GetAlertAsync(string alertId);
    Task<List<Alert>> GetSessionAlertsAsync(string sessionId);
    Task<List<Alert>> GetUnresolvedAlertsAsync();
    Task<bool> ResolveAlertAsync(string alertId);

    // Subscriptions
    Task<NotificationSubscriber> SubscribeAsync(string userId, string email, string? role = "User");
    Task<bool> UnsubscribeAsync(string userId);
    Task<NotificationSubscriber?> GetSubscriberAsync(string userId);
    Task<List<NotificationSubscriber>> GetSubscribersAsync(string topic = "validation");
    Task<bool> UpdateSubscriberAsync(NotificationSubscriber subscriber);

    // Preferences
    Task<NotificationPreference> GetPreferencesAsync(string userId);
    Task<bool> UpdatePreferencesAsync(string userId, NotificationPreference preferences);
    Task<bool> SetChannelEnabledAsync(string userId, string channel, bool enabled);
    Task<bool> SetQuietHoursAsync(string userId, TimeSpan start, TimeSpan end);

    // Queue
    Task<int> GetQueueSizeAsync();
    Task<List<QueuedNotification>> GetPendingAsync(int limit = 100);
    Task<bool> ProcessQueueAsync();
    Task<bool> RetryFailedAsync();

    // Delivery
    Task<List<DeliveryRecord>> GetDeliveryHistoryAsync(string notificationId);
    Task<NotificationStatistics> GetStatisticsAsync();
    Task<double> GetDeliveryRateAsync();

    // Templates
    Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template);
    Task<NotificationTemplate?> GetTemplateAsync(string templateId);
    Task<List<NotificationTemplate>> GetAllTemplatesAsync();
    Task<bool> UpdateTemplateAsync(NotificationTemplate template);

    // Configuration
    void Configure(NotificationConfig config);
    NotificationConfig GetConfiguration();
}
