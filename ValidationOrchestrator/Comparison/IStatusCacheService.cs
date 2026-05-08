namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for status caching and distribution
/// Provides efficient state caching with TTL, invalidation, and real-time updates
/// </summary>
public interface IStatusCacheService
{
    // Initialization
    /// <summary>
    /// Initialize cache for session
    /// </summary>
    Task<CachedStatus> InitializeCacheAsync(string sessionId);

    /// <summary>
    /// Get or create session cache partition
    /// </summary>
    Task<CachePartition> GetPartitionAsync(string sessionId);

    // Basic Cache Operations
    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string sessionId, string key);

    /// <summary>
    /// Set cached value with optional TTL
    /// </summary>
    Task<bool> SetAsync<T>(string sessionId, string key, T value, int? ttlSeconds = null);

    /// <summary>
    /// Set multiple values in batch
    /// </summary>
    Task<bool> SetBatchAsync<T>(string sessionId, Dictionary<string, T> values, int? ttlSeconds = null);

    /// <summary>
    /// Delete cached value
    /// </summary>
    Task<bool> DeleteAsync(string sessionId, string key);

    /// <summary>
    /// Delete multiple values
    /// </summary>
    Task<int> DeleteBatchAsync(string sessionId, List<string> keys);

    /// <summary>
    /// Clear all cache for session
    /// </summary>
    Task<bool> ClearAsync(string sessionId);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string sessionId, string key);

    /// <summary>
    /// Get keys matching pattern
    /// </summary>
    Task<List<string>> GetKeysAsync(string sessionId, string pattern = "*");

    // Status-Specific Operations
    /// <summary>
    /// Get current overall status
    /// </summary>
    Task<CachedStatus?> GetStatusAsync(string sessionId);

    /// <summary>
    /// Update overall status
    /// </summary>
    Task<bool> UpdateStatusAsync(string sessionId, CachedStatus status);

    /// <summary>
    /// Get pipeline status snapshot
    /// </summary>
    Task<PipelineStatusSnapshot?> GetPipelineSnapshotAsync(string sessionId);

    /// <summary>
    /// Cache pipeline snapshot
    /// </summary>
    Task<bool> CachePipelineSnapshotAsync(string sessionId, PipelineStatusSnapshot snapshot);

    /// <summary>
    /// Get phase status
    /// </summary>
    Task<PhaseStatusCache?> GetPhaseStatusAsync(string sessionId, string phaseName);

    /// <summary>
    /// Update phase status
    /// </summary>
    Task<bool> UpdatePhaseStatusAsync(string sessionId, string phaseName, PhaseStatusCache status);

    /// <summary>
    /// Get all phase statuses
    /// </summary>
    Task<List<PhaseStatusCache>> GetAllPhaseStatusesAsync(string sessionId);

    /// <summary>
    /// Get current pipeline state
    /// </summary>
    Task<PipelineCurrentState?> GetCurrentStateAsync(string sessionId);

    /// <summary>
    /// Update current state
    /// </summary>
    Task<bool> UpdateCurrentStateAsync(string sessionId, PipelineCurrentState state);

    // Batch Updates
    /// <summary>
    /// Apply batch status updates
    /// </summary>
    Task<bool> ApplyBatchUpdateAsync(string sessionId, StatusUpdateBatch batch);

    /// <summary>
    /// Queue status update for batch processing
    /// </summary>
    Task<bool> QueueUpdateAsync(string sessionId, StatusUpdate update);

    /// <summary>
    /// Flush queued updates
    /// </summary>
    Task<int> FlushUpdatesAsync(string sessionId);

    // TTL and Expiration
    /// <summary>
    /// Set TTL for cached value
    /// </summary>
    Task<bool> SetTtlAsync(string sessionId, string key, int ttlSeconds);

    /// <summary>
    /// Extend TTL for cached value
    /// </summary>
    Task<bool> ExtendTtlAsync(string sessionId, string key, int additionalSeconds);

    /// <summary>
    /// Get remaining TTL in seconds
    /// </summary>
    Task<int?> GetTtlAsync(string sessionId, string key);

    /// <summary>
    /// Persist cached value (remove TTL)
    /// </summary>
    Task<bool> PersistAsync(string sessionId, string key);

    /// <summary>
    /// Remove expired entries
    /// </summary>
    Task<int> RemoveExpiredAsync(string sessionId);

    /// <summary>
    /// Remove all expired entries globally
    /// </summary>
    Task<int> RemoveAllExpiredAsync();

    // Invalidation
    /// <summary>
    /// Invalidate specific cache key
    /// </summary>
    Task<bool> InvalidateAsync(string sessionId, string key, string? reason = null);

    /// <summary>
    /// Invalidate cache by pattern
    /// </summary>
    Task<int> InvalidateByPatternAsync(string sessionId, string pattern, string? reason = null);

    /// <summary>
    /// Invalidate all phase-related cache
    /// </summary>
    Task<int> InvalidatePhaseAsync(string sessionId, string phaseName);

    /// <summary>
    /// Invalidate entire session cache
    /// </summary>
    Task<bool> InvalidateSessionAsync(string sessionId, string? reason = null);

    /// <summary>
    /// Get invalidation events
    /// </summary>
    Task<List<CacheInvalidationEvent>> GetInvalidationEventsAsync(string sessionId, int? maxCount = null);

    // Warm-up
    /// <summary>
    /// Start cache warm-up for session
    /// </summary>
    Task<CacheWarmUpPlan> StartWarmUpAsync(
        string sessionId,
        List<string> keysToWarmUp,
        Func<string, Task<object>> warmupFunction);

    /// <summary>
    /// Get warm-up progress
    /// </summary>
    Task<CacheWarmUpPlan?> GetWarmUpProgressAsync(string sessionId);

    /// <summary>
    /// Cancel warm-up process
    /// </summary>
    Task<bool> CancelWarmUpAsync(string sessionId);

    // Stale-While-Revalidate
    /// <summary>
    /// Get stale value if fresh unavailable
    /// </summary>
    Task<StaleCacheEntry<T>?> GetStaleAsync<T>(string sessionId, string key);

    /// <summary>
    /// Trigger revalidation of stale entry
    /// </summary>
    Task<T?> RevalidateAsync<T>(string sessionId, string key);

    // Performance and Statistics
    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Get session cache statistics
    /// </summary>
    Task<CacheStatistics> GetSessionStatisticsAsync(string sessionId);

    /// <summary>
    /// Get performance metrics
    /// </summary>
    Task<CachePerformanceMetrics> GetPerformanceMetricsAsync();

    /// <summary>
    /// Get cache entry details
    /// </summary>
    Task<CacheEntry<object>?> GetEntryDetailsAsync(string sessionId, string key);

    /// <summary>
    /// Get partition information
    /// </summary>
    Task<CachePartition?> GetPartitionDetailsAsync(string sessionId);

    /// <summary>
    /// Get memory usage
    /// </summary>
    Task<long> GetMemoryUsageAsync();

    /// <summary>
    /// Get session memory usage
    /// </summary>
    Task<long> GetSessionMemoryUsageAsync(string sessionId);

    // Bulk Operations
    /// <summary>
    /// Preload common status keys
    /// </summary>
    Task<bool> PreloadAsync(string sessionId);

    /// <summary>
    /// Refresh specific status
    /// </summary>
    Task<bool> RefreshAsync(string sessionId, string key);

    /// <summary>
    /// Refresh all statuses for session
    /// </summary>
    Task<int> RefreshAllAsync(string sessionId);

    /// <summary>
    /// Sync cache with source
    /// </summary>
    Task<bool> SyncAsync(string sessionId);

    // Real-time Updates
    /// <summary>
    /// Subscribe to cache updates
    /// </summary>
    void SubscribeToUpdates(string sessionId, Func<StatusUpdateBatch, Task> callback);

    /// <summary>
    /// Unsubscribe from updates
    /// </summary>
    void UnsubscribeFromUpdates(string sessionId);

    /// <summary>
    /// Broadcast cache update
    /// </summary>
    Task BroadcastUpdateAsync(StatusUpdateBatch batch);

    /// <summary>
    /// Get cache update stream
    /// </summary>
    IAsyncEnumerable<StatusUpdateBatch> GetUpdateStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    // Configuration
    /// <summary>
    /// Configure cache service
    /// </summary>
    void Configure(StatusCacheConfig config);

    /// <summary>
    /// Get current configuration
    /// </summary>
    StatusCacheConfig GetConfiguration();

    // Cleanup
    /// <summary>
    /// Archive old cache entries
    /// </summary>
    Task<int> ArchiveOldEntriesAsync(int olderThanDays);

    /// <summary>
    /// Compact cache (remove gaps, optimize memory)
    /// </summary>
    Task<bool> CompactAsync(string sessionId);

    /// <summary>
    /// Flush entire cache (careful operation)
    /// </summary>
    Task<int> FlushAllAsync();

    /// <summary>
    /// Reset cache statistics
    /// </summary>
    Task<bool> ResetStatisticsAsync();
}
