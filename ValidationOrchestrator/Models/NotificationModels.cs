namespace ValidationOrchestrator.Models;

/// <summary>
/// Notification message for user delivery
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // info, warning, error, critical, success
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Priority { get; set; } = 1; // 1-5, higher = more urgent
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
    public bool IsAcknowledged => AcknowledgedAt.HasValue;
    public string? RelatedResource { get; set; } // e.g., "phase:Discovery"
    public List<string> ActionUrls { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// User notification preferences
/// </summary>
public class NotificationPreference
{
    public string UserId { get; set; } = string.Empty;
    public bool EnableNotifications { get; set; } = true;
    public bool EmailNotifications { get; set; } = true;
    public bool SlackNotifications { get; set; } = false;
    public bool SmsNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;

    // Severity filtering
    public bool NotifyOnInfo { get; set; } = false;
    public bool NotifyOnWarning { get; set; } = true;
    public bool NotifyOnError { get; set; } = true;
    public bool NotifyOnCritical { get; set; } = true;

    // Quiet hours
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }

    // Contact info
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? SlackUserId { get; set; }
    public string? SlackChannel { get; set; }

    // Batching
    public int? BatchNotificationsMinutes { get; set; }
    public int MaxNotificationsPerDay { get; set; } = 100;
}

/// <summary>
/// Alert for blocking issues
/// </summary>
public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // blocker, critical, warning, info
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; } = 1; // 1-5
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public bool IsResolved => ResolvedAt.HasValue;
    public string? Component { get; set; } // e.g., "Discovery", "Comparison"
    public List<string> AffectedTables { get; set; } = new();
    public string? ResolutionGuidance { get; set; }
    public string? MitigationAction { get; set; }
    public int? EstimatedImpactedUsers { get; set; }
}

/// <summary>
/// Notification queue entry
/// </summary>
public class QueuedNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Notification Notification { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public List<string> Channels { get; set; } = new();
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Sending, Sent, Failed, Bounced
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? FailureReason { get; set; }
    public Dictionary<string, DeliveryStatus> DeliveryStatuses { get; set; } = new();
}

/// <summary>
/// Delivery status for individual channel
/// </summary>
public class DeliveryStatus
{
    public string Channel { get; set; } = string.Empty; // email, slack, sms, push
    public string Status { get; set; } = "Pending"; // Pending, Sent, Delivered, Failed, Bounced
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// Notification template
/// </summary>
public class NotificationTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // PhaseStarted, PhaseCompleted, IssueDetected, etc.
    public string? EmailSubject { get; set; }
    public string? EmailTemplate { get; set; }
    public string? SlackTemplate { get; set; }
    public string? SmsTemplate { get; set; }
    public string? PushTitle { get; set; }
    public string? PushBody { get; set; }
    public List<string> AvailableVariables { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Notification batch
/// </summary>
public class NotificationBatch
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public List<Notification> Notifications { get; set; } = new();
    public int TotalCount => Notifications.Count;
    public int ReadCount => Notifications.Count(n => n.IsRead);
    public int UnreadCount => TotalCount - ReadCount;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification statistics
/// </summary>
public class NotificationStatistics
{
    public long TotalSent { get; set; }
    public long TotalDelivered { get; set; }
    public long TotalFailed { get; set; }
    public long TotalBounced { get; set; }
    public double DeliveryRate { get; set; } // 0-100
    public double BounceRate { get; set; } // 0-100
    public long AverageDeliveryTimeMs { get; set; }
    public int ActiveSubscribers { get; set; }
    public Dictionary<string, long> ChannelStats { get; set; } = new();
}

/// <summary>
/// Subscriber for notifications
/// </summary>
public class NotificationSubscriber
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? SlackId { get; set; }
    public string Role { get; set; } = "User"; // Admin, Manager, User, Observer
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
    public List<string> Topics { get; set; } = new(); // "validation", "migration", "alerts"
    public NotificationPreference Preferences { get; set; } = new();
}

/// <summary>
/// Notification delivery history
/// </summary>
public class DeliveryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NotificationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public long DeliveryTimeMs => DeliveredAt.HasValue ? (long)(DeliveredAt.Value - SentAt).TotalMilliseconds : 0;
    public string? ExternalId { get; set; } // Provider message ID
}

/// <summary>
/// Notification policy
/// </summary>
public class NotificationPolicy
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty; // e.g., "phase_completed" or "risk_score > 50"
    public List<Notification> TriggeredNotifications { get; set; } = new();
    public string Frequency { get; set; } = "Immediate"; // Immediate, Hourly, Daily, Weekly
    public List<string> ApplicableRoles { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Notification configuration
/// </summary>
public class NotificationConfig
{
    public bool EnableNotifications { get; set; } = true;
    public int MaxQueueSize { get; set; } = 10000;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 60;
    public int BatchSizeForDeletion { get; set; } = 100;
    public int NotificationRetentionDays { get; set; } = 30;
    public string DefaultChannel { get; set; } = "email";
    public List<string> EnabledChannels { get; set; } = new() { "email", "push" };
    public Dictionary<string, string> ProviderApiKeys { get; set; } = new();
    public int MaxNotificationsPerUser { get; set; } = 100;
    public int BatchNotificationDelaySeconds { get; set; } = 300;
}

/// <summary>
/// Notification distribution event
/// </summary>
public class NotificationEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // Created, Sent, Delivered, Read, Failed, Bounced
    public string NotificationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> EventData { get; set; } = new();
}
