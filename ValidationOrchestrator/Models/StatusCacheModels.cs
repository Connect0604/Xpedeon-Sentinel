namespace ValidationOrchestrator.Models;

/// <summary>
/// Cached status snapshot for efficient state distribution
/// </summary>
public class CachedStatus
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;

    // Current phase status
    public string CurrentPhase { get; set; } = string.Empty;
    public int CurrentPhaseNumber { get; set; }
    public double PhaseProgress { get; set; } // 0-100

    // Overall progress
    public double OverallProgress { get; set; } // 0-100
    public string PipelineStatus { get; set; } = "Running"; // Running, Paused, Completed, Failed

    // Timeline
    public long ElapsedMs { get; set; }
    public long? EstimatedRemainingMs { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }

    // Quick metrics
    public int TotalDiscrepancies { get; set; }
    public int CriticalIssues { get; set; }
    public double QualityScore { get; set; } // 0-100
    public double RiskScore { get; set; } // 0-100

    // Notifications
    public int UnreadAlertCount { get; set; }
    public int UnreadNotificationCount { get; set; }

    // Metadata
    public Dictionary<string, object> CustomData { get; set; } = new();
}

/// <summary>
/// Status for individual phase
/// </summary>
public class PhaseStatusCache
{
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseNumber { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed, Skipped
    public double Progress { get; set; } // 0-100
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public long DurationMs { get; set; }
    public long EstimatedRemainingMs { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? LastActivity { get; set; }
    public DateTime? LastActivityTime { get; set; }
}

/// <summary>
/// Quick snapshot of all phase statuses
/// </summary>
public class PipelineStatusSnapshot
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
    public string OverallStatus { get; set; } = "Running";
    public double OverallProgress { get; set; } // 0-100
    public List<PhaseStatusCache> Phases { get; set; } = new();
    public int TotalPhasesCompleted { get; set; }
    public int TotalPhases { get; set; }
}

/// <summary>
/// Aggregated current state for quick access
/// </summary>
public class PipelineCurrentState
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StateTime { get; set; } = DateTime.UtcNow;

    // Phase state
    public string CurrentPhase { get; set; } = string.Empty;
    public double CurrentPhaseProgress { get; set; }
    public string CurrentPhaseStatus { get; set; } = "Pending";
    public string CurrentStep { get; set; } = string.Empty;

    // Progress state
    public double OverallProgress { get; set; }
    public long ElapsedMs { get; set; }
    public long EstimatedRemainingMs { get; set; }
    public DateTime? EstimatedCompletion { get; set; }

    // Session state
    public string PipelineStatus { get; set; } = "Running";
    public int ActiveUsers { get; set; }
    public string ClientId { get; set; } = string.Empty;

    // Issue state
    public int TotalIssues { get; set; }
    public int BlockingIssues { get; set; }
    public int UnresolvedIssues { get; set; }

    // Quality state
    public double QualityScore { get; set; }
    public double RiskScore { get; set; }
    public string ReadinessStatus { get; set; } = string.Empty;

    // Latest activity
    public string LastEvent { get; set; } = string.Empty;
    public DateTime LastEventTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache statistics and metrics
/// </summary>
public class CacheStatistics
{
    public int TotalCachedSessions { get; set; }
    public int ActiveCachedSessions { get; set; }
    public int ExpiredCachedSessions { get; set; }
    public long TotalCacheMemoryBytes { get; set; }
    public double AverageCacheMemoryPerSessionBytes { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRate { get; set; } // 0-100
    public long CacheEvictions { get; set; }
    public long AverageResponseTimeMs { get; set; }
    public long TotalGetRequests { get; set; }
    public long TotalSetRequests { get; set; }
    public long TotalDeleteRequests { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache entry with metadata
/// </summary>
public class CacheEntry<T>
{
    public string Key { get; set; } = string.Empty;
    public T Value { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public int AccessCount { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public long Size { get; set; } // Estimated size in bytes

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;
    public long Age => (long)(DateTime.UtcNow - CreatedAt).TotalMilliseconds;
}

/// <summary>
/// Batch status update
/// </summary>
public class StatusUpdateBatch
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    public List<StatusUpdate> Updates { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Individual status update
/// </summary>
public class StatusUpdate
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public string? PreviousValue { get; set; }
    public string UpdateType { get; set; } = "Set"; // Set, Increment, Append
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Invalidation strategy for cache entries
/// </summary>
public class CacheInvalidationStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public int? TtlSeconds { get; set; }
    public bool InvalidateOnPhaseChange { get; set; }
    public bool InvalidateOnProgressUpdate { get; set; }
    public bool InvalidateOnIssueDetected { get; set; }
    public int? MaxAge { get; set; }
    public int? MaxAccessCount { get; set; }
}

/// <summary>
/// Cache configuration
/// </summary>
public class StatusCacheConfig
{
    /// <summary>Enable caching</summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>Default TTL in seconds</summary>
    public int DefaultTtlSeconds { get; set; } = 30;

    /// <summary>Maximum cache size in MB</summary>
    public int MaxCacheSizeMb { get; set; } = 100;

    /// <summary>Maximum entries per session</summary>
    public int MaxEntriesPerSession { get; set; } = 1000;

    /// <summary>Eviction policy</summary>
    public string EvictionPolicy { get; set; } = "LRU"; // LRU, FIFO, LFU

    /// <summary>Enable cache statistics</summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>Cache invalidation strategy</summary>
    public CacheInvalidationStrategy InvalidationStrategy { get; set; } = new();

    /// <summary>Compress cached data</summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>Cache stale-while-revalidate window (seconds)</summary>
    public int StaleWhileRevalidateSeconds { get; set; } = 5;

    /// <summary>Enable metrics tracking</summary>
    public bool TrackMetrics { get; set; } = true;
}

/// <summary>
/// Stale cache entry with revalidation capability
/// </summary>
public class StaleCacheEntry<T>
{
    public string Key { get; set; } = string.Empty;
    public T StaleValue { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool CanRevalidate { get; set; }
    public long RevalidateWindowMs { get; set; }
    public Func<Task<T>>? Revalidator { get; set; }
}

/// <summary>
/// Cache warm-up plan
/// </summary>
public class CacheWarmUpPlan
{
    public string SessionId { get; set; } = string.Empty;
    public List<string> KeysToWarmUp { get; set; } = new();
    public Func<string, Task<object>>? WarmupFunction { get; set; }
    public DateTime PlannedAt { get; set; } = DateTime.UtcNow;
    public int TotalKeysToWarmUp { get; set; }
    public int WarmedUpKeys { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
}

/// <summary>
/// Cache invalidation event
/// </summary>
public class CacheInvalidationEvent
{
    public string Key { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; // Expired, Manual, PhaseChange, IssueDetected
    public DateTime InvalidatedAt { get; set; } = DateTime.UtcNow;
    public object? OldValue { get; set; }
    public string InvalidationType { get; set; } = "Single"; // Single, Multiple, All
}

/// <summary>
/// Cache performance monitoring
/// </summary>
public class CachePerformanceMetrics
{
    public long TotalGetRequests { get; set; }
    public long TotalSetRequests { get; set; }
    public long TotalDeleteRequests { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public long TotalEvictions { get; set; }
    public double AverageGetTimeMs { get; set; }
    public double AverageSetTimeMs { get; set; }
    public long PeakMemoryUsageBytes { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache partition for session isolation
/// </summary>
public class CachePartition
{
    public string PartitionId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Dictionary<string, CacheEntry<object>> Entries { get; set; } = new();
    public long PartitionSizeBytes { get; set; }
    public int EntryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}
