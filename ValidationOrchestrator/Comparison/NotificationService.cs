namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Implementation of notification distribution and management
/// </summary>
public class NotificationService : INotificationService
{
    private readonly Dictionary<string, Notification> _notifications = new();
    private readonly Dictionary<string, NotificationSubscriber> _subscribers = new();
    private readonly Dictionary<string, NotificationPreference> _preferences = new();
    private readonly List<QueuedNotification> _queue = new();
    private readonly List<Alert> _alerts = new();
    private readonly Dictionary<string, NotificationTemplate> _templates = new();
    private readonly List<DeliveryRecord> _deliveryHistory = new();
    private NotificationConfig _config = new();

    private long _totalSent;
    private long _totalDelivered;
    private long _totalFailed;

    public async Task<Notification> SendNotificationAsync(
        string userId,
        string type,
        string title,
        string message,
        int priority = 1)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Priority = priority
        };

        _notifications[notification.Id] = notification;
        _totalSent++;

        // Queue for delivery
        var queued = new QueuedNotification
        {
            Notification = notification,
            Recipients = new() { userId },
            Channels = _config.EnabledChannels,
            Status = "Pending"
        };

        _queue.Add(queued);
        return notification;
    }

    public async Task<Notification> SendToSessionAsync(
        string sessionId,
        string type,
        string title,
        string message)
    {
        var notification = new Notification
        {
            SessionId = sessionId,
            Type = type,
            Title = title,
            Message = message
        };

        _notifications[notification.Id] = notification;
        _totalSent++;

        return notification;
    }

    public async Task<bool> BroadcastAsync(
        string type,
        string title,
        string message,
        List<string>? userIds = null)
    {
        var recipientIds = userIds ?? _subscribers.Keys.ToList();

        foreach (var userId in recipientIds)
        {
            await SendNotificationAsync(userId, type, title, message);
        }

        return true;
    }

    public async Task<Notification?> GetNotificationAsync(string notificationId)
    {
        _notifications.TryGetValue(notificationId, out var notification);
        return notification;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var notifications = _notifications.Values
            .Where(n => n.UserId == userId)
            .ToList();

        if (unreadOnly)
        {
            notifications = notifications.Where(n => !n.IsRead).ToList();
        }

        return notifications.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return _notifications.Values.Count(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            notification.ReadAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var count = 0;

        foreach (var notification in _notifications.Values.Where(n => n.UserId == userId && !n.IsRead))
        {
            notification.ReadAt = DateTime.UtcNow;
            count++;
        }

        return count > 0;
    }

    public async Task<bool> AcknowledgeAsync(string notificationId)
    {
        if (_notifications.TryGetValue(notificationId, out var notification))
        {
            notification.AcknowledgedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId)
    {
        return _notifications.Remove(notificationId);
    }

    public async Task<Alert> CreateAlertAsync(
        string sessionId,
        string type,
        string title,
        string description)
    {
        var alert = new Alert
        {
            SessionId = sessionId,
            Type = type,
            Title = title,
            Description = description
        };

        _alerts.Add(alert);
        return alert;
    }

    public async Task<Alert?> GetAlertAsync(string alertId)
    {
        return _alerts.FirstOrDefault(a => a.Id == alertId);
    }

    public async Task<List<Alert>> GetSessionAlertsAsync(string sessionId)
    {
        return _alerts.Where(a => a.SessionId == sessionId).OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<List<Alert>> GetUnresolvedAlertsAsync()
    {
        return _alerts.Where(a => !a.IsResolved).OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<bool> ResolveAlertAsync(string alertId)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);

        if (alert != null)
        {
            alert.ResolvedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public async Task<NotificationSubscriber> SubscribeAsync(
        string userId,
        string email,
        string? role = "User")
    {
        var subscriber = new NotificationSubscriber
        {
            UserId = userId,
            Email = email,
            Role = role ?? "User"
        };

        _subscribers[userId] = subscriber;
        _preferences[userId] = new() { UserId = userId };

        return subscriber;
    }

    public async Task<bool> UnsubscribeAsync(string userId)
    {
        if (_subscribers.TryGetValue(userId, out var subscriber))
        {
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            subscriber.IsActive = false;
            return true;
        }

        return false;
    }

    public async Task<NotificationSubscriber?> GetSubscriberAsync(string userId)
    {
        _subscribers.TryGetValue(userId, out var subscriber);
        return subscriber;
    }

    public async Task<List<NotificationSubscriber>> GetSubscribersAsync(string topic = "validation")
    {
        return _subscribers.Values
            .Where(s => s.IsActive && s.Topics.Contains(topic))
            .ToList();
    }

    public async Task<bool> UpdateSubscriberAsync(NotificationSubscriber subscriber)
    {
        _subscribers[subscriber.UserId] = subscriber;
        return true;
    }

    public async Task<NotificationPreference> GetPreferencesAsync(string userId)
    {
        if (!_preferences.TryGetValue(userId, out var pref))
        {
            pref = new() { UserId = userId };
            _preferences[userId] = pref;
        }

        return pref;
    }

    public async Task<bool> UpdatePreferencesAsync(string userId, NotificationPreference preferences)
    {
        preferences.UserId = userId;
        _preferences[userId] = preferences;
        return true;
    }

    public async Task<bool> SetChannelEnabledAsync(string userId, string channel, bool enabled)
    {
        var pref = await GetPreferencesAsync(userId);

        switch (channel.ToLower())
        {
            case "email":
                pref.EmailNotifications = enabled;
                break;
            case "slack":
                pref.SlackNotifications = enabled;
                break;
            case "sms":
                pref.SmsNotifications = enabled;
                break;
            case "push":
                pref.PushNotifications = enabled;
                break;
        }

        _preferences[userId] = pref;
        return true;
    }

    public async Task<bool> SetQuietHoursAsync(string userId, TimeSpan start, TimeSpan end)
    {
        var pref = await GetPreferencesAsync(userId);
        pref.QuietHoursStart = start;
        pref.QuietHoursEnd = end;
        _preferences[userId] = pref;
        return true;
    }

    public async Task<int> GetQueueSizeAsync()
    {
        return _queue.Count(q => q.Status == "Pending");
    }

    public async Task<List<QueuedNotification>> GetPendingAsync(int limit = 100)
    {
        return _queue.Where(q => q.Status == "Pending").Take(limit).ToList();
    }

    public async Task<bool> ProcessQueueAsync()
    {
        var pending = _queue.Where(q => q.Status == "Pending").ToList();

        foreach (var item in pending)
        {
            item.Status = "Sent";
            item.SentAt = DateTime.UtcNow;
            _totalDelivered++;

            // Record delivery
            foreach (var recipient in item.Recipients)
            {
                _deliveryHistory.Add(new DeliveryRecord
                {
                    NotificationId = item.Notification.Id,
                    UserId = recipient,
                    Channel = item.Channels.FirstOrDefault() ?? "email",
                    Status = "Delivered",
                    DeliveredAt = DateTime.UtcNow
                });
            }
        }

        return true;
    }

    public async Task<bool> RetryFailedAsync()
    {
        var failed = _queue.Where(q => q.Status == "Failed" && q.RetryCount < q.MaxRetries).ToList();

        foreach (var item in failed)
        {
            item.RetryCount++;
            item.Status = "Pending";
        }

        return true;
    }

    public async Task<List<DeliveryRecord>> GetDeliveryHistoryAsync(string notificationId)
    {
        return _deliveryHistory.Where(d => d.NotificationId == notificationId).ToList();
    }

    public async Task<NotificationStatistics> GetStatisticsAsync()
    {
        return new NotificationStatistics
        {
            TotalSent = _totalSent,
            TotalDelivered = _totalDelivered,
            TotalFailed = _totalFailed,
            DeliveryRate = _totalSent > 0 ? (_totalDelivered / (double)_totalSent) * 100 : 0,
            ActiveSubscribers = _subscribers.Count(s => s.Value.IsActive),
            AverageDeliveryTimeMs = _deliveryHistory.Count > 0 ?
                (long)_deliveryHistory.Average(d => d.DeliveryTimeMs) : 0,
            ChannelStats = new()
            {
                { "email", _deliveryHistory.Count(d => d.Channel == "email") },
                { "sms", _deliveryHistory.Count(d => d.Channel == "sms") },
                { "slack", _deliveryHistory.Count(d => d.Channel == "slack") },
                { "push", _deliveryHistory.Count(d => d.Channel == "push") }
            }
        };
    }

    public async Task<double> GetDeliveryRateAsync()
    {
        var stats = await GetStatisticsAsync();
        return stats.DeliveryRate;
    }

    public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template)
    {
        if (string.IsNullOrEmpty(template.Id))
        {
            template.Id = Guid.NewGuid().ToString();
        }

        _templates[template.Id] = template;
        return template;
    }

    public async Task<NotificationTemplate?> GetTemplateAsync(string templateId)
    {
        _templates.TryGetValue(templateId, out var template);
        return template;
    }

    public async Task<List<NotificationTemplate>> GetAllTemplatesAsync()
    {
        return _templates.Values.ToList();
    }

    public async Task<bool> UpdateTemplateAsync(NotificationTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        _templates[template.Id] = template;
        return true;
    }

    public void Configure(NotificationConfig config)
    {
        _config = config;
    }

    public NotificationConfig GetConfiguration()
    {
        return _config;
    }
}
