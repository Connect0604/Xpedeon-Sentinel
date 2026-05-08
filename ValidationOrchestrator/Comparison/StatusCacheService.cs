namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Implementation of status caching and distribution
/// </summary>
public class StatusCacheService : IStatusCacheService
{
    private readonly Dictionary<string, CachePartition> _partitions = new();
    private readonly Dictionary<string, CachedStatus> _statusCache = new();
    private readonly Dictionary<string, List<StatusUpdate>> _updateQueues = new();
    private readonly Dictionary<string, List<Func<StatusUpdateBatch, Task>>> _subscribers = new();
    private readonly Dictionary<string, List<CacheInvalidationEvent>> _invalidationEvents = new();
    private readonly Dictionary<string, CacheWarmUpPlan> _warmupPlans = new();
    private readonly List<CachePerformanceMetrics> _performanceHistory = new();
    private StatusCacheConfig _config = new();

    private long _totalGets;
    private long _totalSets;
    private long _totalDeletes;
    private long _hits;
    private long _misses;
    private long _evictions;

    public async Task<CachedStatus> InitializeCacheAsync(string sessionId)
    {
        var status = new CachedStatus
        {
            SessionId = sessionId,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_config.DefaultTtlSeconds)
        };

        _statusCache[sessionId] = status;
        await GetPartitionAsync(sessionId);

        return status;
    }

    public async Task<CachePartition> GetPartitionAsync(string sessionId)
    {
        if (!_partitions.TryGetValue(sessionId, out var partition))
        {
            partition = new CachePartition
            {
                PartitionId = $"{sessionId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow
            };
            _partitions[sessionId] = partition;
        }

        partition.LastAccessedAt = DateTime.UtcNow;
        return partition;
    }

    public async Task<T?> GetAsync<T>(string sessionId, string key)
    {
        _totalGets++;
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            var cacheEntry = (CacheEntry<object>)entry;

            if (cacheEntry.IsExpired)
            {
                partition.Entries.Remove(key);
                _misses++;
                return default;
            }

            cacheEntry.AccessCount++;
            cacheEntry.LastAccessedAt = DateTime.UtcNow;
            _hits++;

            return cacheEntry.Value is T typedValue ? typedValue : default;
        }

        _misses++;
        return default;
    }

    public async Task<bool> SetAsync<T>(string sessionId, string key, T value, int? ttlSeconds = null)
    {
        _totalSets++;
        var partition = await GetPartitionAsync(sessionId);

        var entry = new CacheEntry<object>
        {
            Key = key,
            Value = (object?)value ?? new object(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = ttlSeconds.HasValue ? DateTime.UtcNow.AddSeconds(ttlSeconds.Value) : null,
            Size = EstimateSize(value)
        };

        partition.Entries[key] = entry;
        partition.EntryCount = partition.Entries.Count;
        UpdatePartitionSize(partition);

        if (partition.PartitionSizeBytes > _config.MaxCacheSizeMb * 1024 * 1024)
        {
            await EvictEntriesAsync(sessionId);
        }

        return true;
    }

    public async Task<bool> SetBatchAsync<T>(string sessionId, Dictionary<string, T> values, int? ttlSeconds = null)
    {
        foreach (var kvp in values)
        {
            await SetAsync(sessionId, kvp.Key, kvp.Value, ttlSeconds);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(string sessionId, string key)
    {
        _totalDeletes++;
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.Remove(key))
        {
            partition.EntryCount--;
            UpdatePartitionSize(partition);
            return true;
        }

        return false;
    }

    public async Task<int> DeleteBatchAsync(string sessionId, List<string> keys)
    {
        int deleted = 0;
        foreach (var key in keys)
        {
            if (await DeleteAsync(sessionId, key))
            {
                deleted++;
            }
        }

        return deleted;
    }

    public async Task<bool> ClearAsync(string sessionId)
    {
        var partition = await GetPartitionAsync(sessionId);
        partition.Entries.Clear();
        partition.EntryCount = 0;
        partition.PartitionSizeBytes = 0;
        _statusCache.Remove(sessionId);

        return true;
    }

    public async Task<bool> ExistsAsync(string sessionId, string key)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            var cacheEntry = (CacheEntry<object>)entry;
            return !cacheEntry.IsExpired;
        }

        return false;
    }

    public async Task<List<string>> GetKeysAsync(string sessionId, string pattern = "*")
    {
        var partition = await GetPartitionAsync(sessionId);
        var regex = PatternToRegex(pattern);

        return partition.Entries
            .Where(kvp => !((CacheEntry<object>)kvp.Value).IsExpired && regex.IsMatch(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public async Task<CachedStatus?> GetStatusAsync(string sessionId)
    {
        if (_statusCache.TryGetValue(sessionId, out var status))
        {
            if (!status.IsExpired)
            {
                return status;
            }

            _statusCache.Remove(sessionId);
        }

        return null;
    }

    public async Task<bool> UpdateStatusAsync(string sessionId, CachedStatus status)
    {
        status.CachedAt = DateTime.UtcNow;
        status.ExpiresAt = DateTime.UtcNow.AddSeconds(_config.DefaultTtlSeconds);
        _statusCache[sessionId] = status;

        return true;
    }

    public async Task<PipelineStatusSnapshot?> GetPipelineSnapshotAsync(string sessionId)
    {
        return await GetAsync<PipelineStatusSnapshot>(sessionId, $"{sessionId}:pipeline:snapshot");
    }

    public async Task<bool> CachePipelineSnapshotAsync(string sessionId, PipelineStatusSnapshot snapshot)
    {
        return await SetAsync(sessionId, $"{sessionId}:pipeline:snapshot", snapshot, _config.DefaultTtlSeconds);
    }

    public async Task<PhaseStatusCache?> GetPhaseStatusAsync(string sessionId, string phaseName)
    {
        return await GetAsync<PhaseStatusCache>(sessionId, $"{sessionId}:phase:{phaseName}");
    }

    public async Task<bool> UpdatePhaseStatusAsync(string sessionId, string phaseName, PhaseStatusCache status)
    {
        await InvalidatePhaseAsync(sessionId, phaseName);
        return await SetAsync(sessionId, $"{sessionId}:phase:{phaseName}", status, _config.DefaultTtlSeconds);
    }

    public async Task<List<PhaseStatusCache>> GetAllPhaseStatusesAsync(string sessionId)
    {
        var keys = await GetKeysAsync(sessionId, $"{sessionId}:phase:*");
        var statuses = new List<PhaseStatusCache>();

        foreach (var key in keys)
        {
            if (await GetAsync<PhaseStatusCache>(sessionId, key) is PhaseStatusCache status)
            {
                statuses.Add(status);
            }
        }

        return statuses;
    }

    public async Task<PipelineCurrentState?> GetCurrentStateAsync(string sessionId)
    {
        return await GetAsync<PipelineCurrentState>(sessionId, $"{sessionId}:state:current");
    }

    public async Task<bool> UpdateCurrentStateAsync(string sessionId, PipelineCurrentState state)
    {
        state.StateTime = DateTime.UtcNow;
        return await SetAsync(sessionId, $"{sessionId}:state:current", state, _config.DefaultTtlSeconds);
    }

    public async Task<bool> ApplyBatchUpdateAsync(string sessionId, StatusUpdateBatch batch)
    {
        foreach (var update in batch.Updates)
        {
            switch (update.UpdateType.ToLower())
            {
                case "set":
                    await SetAsync(sessionId, update.Key, update.Value, _config.DefaultTtlSeconds);
                    break;
                case "increment":
                    if (double.TryParse(update.Value?.ToString(), out var numValue))
                    {
                        await SetAsync(sessionId, update.Key, numValue, _config.DefaultTtlSeconds);
                    }
                    break;
                case "append":
                    // Append to existing value
                    break;
            }
        }

        await BroadcastUpdateAsync(batch);
        return true;
    }

    public async Task<bool> QueueUpdateAsync(string sessionId, StatusUpdate update)
    {
        if (!_updateQueues.ContainsKey(sessionId))
        {
            _updateQueues[sessionId] = new();
        }

        _updateQueues[sessionId].Add(update);
        return true;
    }

    public async Task<int> FlushUpdatesAsync(string sessionId)
    {
        if (!_updateQueues.TryGetValue(sessionId, out var updates))
        {
            return 0;
        }

        int count = updates.Count;

        foreach (var update in updates)
        {
            await SetAsync(sessionId, update.Key, update.Value, _config.DefaultTtlSeconds);
        }

        updates.Clear();
        return count;
    }

    public async Task<bool> SetTtlAsync(string sessionId, string key, int ttlSeconds)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            ((CacheEntry<object>)entry).ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds);
            return true;
        }

        return false;
    }

    public async Task<bool> ExtendTtlAsync(string sessionId, string key, int additionalSeconds)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            var cacheEntry = (CacheEntry<object>)entry;
            if (cacheEntry.ExpiresAt.HasValue)
            {
                cacheEntry.ExpiresAt = cacheEntry.ExpiresAt.Value.AddSeconds(additionalSeconds);
                return true;
            }
        }

        return false;
    }

    public async Task<int?> GetTtlAsync(string sessionId, string key)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            var cacheEntry = (CacheEntry<object>)entry;
            if (cacheEntry.ExpiresAt.HasValue)
            {
                var remaining = (int)(cacheEntry.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? remaining : null;
            }
        }

        return null;
    }

    public async Task<bool> PersistAsync(string sessionId, string key)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            ((CacheEntry<object>)entry).ExpiresAt = null;
            return true;
        }

        return false;
    }

    public async Task<int> RemoveExpiredAsync(string sessionId)
    {
        var partition = await GetPartitionAsync(sessionId);
        var expiredKeys = partition.Entries
            .Where(kvp => ((CacheEntry<object>)kvp.Value).IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            partition.Entries.Remove(key);
        }

        partition.EntryCount = partition.Entries.Count;
        return expiredKeys.Count;
    }

    public async Task<int> RemoveAllExpiredAsync()
    {
        int total = 0;

        foreach (var sessionId in _partitions.Keys.ToList())
        {
            total += await RemoveExpiredAsync(sessionId);
        }

        return total;
    }

    public async Task<bool> InvalidateAsync(string sessionId, string key, string? reason = null)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.Remove(key))
        {
            partition.EntryCount--;
            RecordInvalidation(sessionId, key, reason ?? "Manual");
            return true;
        }

        return false;
    }

    public async Task<int> InvalidateByPatternAsync(string sessionId, string pattern, string? reason = null)
    {
        var partition = await GetPartitionAsync(sessionId);
        var regex = PatternToRegex(pattern);
        var keysToRemove = partition.Entries.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            partition.Entries.Remove(key);
            RecordInvalidation(sessionId, key, reason ?? "PatternMatch");
        }

        partition.EntryCount = partition.Entries.Count;
        return keysToRemove.Count;
    }

    public async Task<int> InvalidatePhaseAsync(string sessionId, string phaseName)
    {
        return await InvalidateByPatternAsync(sessionId, $"{sessionId}:phase:{phaseName}*", "PhaseChange");
    }

    public async Task<bool> InvalidateSessionAsync(string sessionId, string? reason = null)
    {
        await ClearAsync(sessionId);
        RecordInvalidation(sessionId, "*", reason ?? "SessionInvalidation");
        return true;
    }

    public async Task<List<CacheInvalidationEvent>> GetInvalidationEventsAsync(string sessionId, int? maxCount = null)
    {
        if (!_invalidationEvents.TryGetValue(sessionId, out var events))
        {
            return new();
        }

        return maxCount.HasValue ? events.TakeLast(maxCount.Value).ToList() : events;
    }

    public async Task<CacheWarmUpPlan> StartWarmUpAsync(
        string sessionId,
        List<string> keysToWarmUp,
        Func<string, Task<object>> warmupFunction)
    {
        var plan = new CacheWarmUpPlan
        {
            SessionId = sessionId,
            KeysToWarmUp = keysToWarmUp,
            WarmupFunction = warmupFunction,
            TotalKeysToWarmUp = keysToWarmUp.Count
        };

        _warmupPlans[sessionId] = plan;

        // Execute warm-up asynchronously
        _ = ExecuteWarmUpAsync(sessionId, plan);

        return plan;
    }

    public async Task<CacheWarmUpPlan?> GetWarmUpProgressAsync(string sessionId)
    {
        _warmupPlans.TryGetValue(sessionId, out var plan);
        return plan;
    }

    public async Task<bool> CancelWarmUpAsync(string sessionId)
    {
        if (_warmupPlans.TryGetValue(sessionId, out var plan))
        {
            plan.Status = "Cancelled";
            return true;
        }

        return false;
    }

    public async Task<StaleCacheEntry<T>?> GetStaleAsync<T>(string sessionId, string key)
    {
        var partition = await GetPartitionAsync(sessionId);

        if (partition.Entries.TryGetValue(key, out var entry))
        {
            var cacheEntry = (CacheEntry<object>)entry;
            if (cacheEntry.IsExpired && cacheEntry.ExpiresAt.HasValue)
            {
                var revalidateWindow = _config.StaleWhileRevalidateSeconds * 1000;
                var isInWindow = (DateTime.UtcNow - cacheEntry.ExpiresAt.Value).TotalMilliseconds < revalidateWindow;

                if (isInWindow)
                {
                    return new StaleCacheEntry<T>
                    {
                        Key = key,
                        StaleValue = cacheEntry.Value is T tv ? tv : default!,
                        ExpiresAt = cacheEntry.ExpiresAt.Value,
                        CanRevalidate = true,
                        RevalidateWindowMs = revalidateWindow
                    };
                }
            }
        }

        return null;
    }

    public async Task<T?> RevalidateAsync<T>(string sessionId, string key)
    {
        // Trigger refresh of stale entry
        return await GetAsync<T>(sessionId, key);
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return new CacheStatistics
        {
            TotalCachedSessions = _partitions.Count,
            ActiveCachedSessions = _partitions.Count(p => !p.Value.Entries.Values.Cast<CacheEntry<object>>().All(e => e.IsExpired)),
            TotalCacheMemoryBytes = _partitions.Values.Sum(p => p.PartitionSizeBytes),
            CacheHits = _hits,
            CacheMisses = _misses,
            CacheHitRate = _hits + _misses > 0 ? (_hits / (double)(_hits + _misses)) * 100 : 0,
            TotalGetRequests = _totalGets,
            TotalSetRequests = _totalSets,
            TotalDeleteRequests = _totalDeletes,
            CacheEvictions = _evictions
        };
    }

    public async Task<CacheStatistics> GetSessionStatisticsAsync(string sessionId)
    {
        if (!_partitions.TryGetValue(sessionId, out var partition))
        {
            return new() { TotalCachedSessions = 0 };
        }

        return new CacheStatistics
        {
            TotalCachedSessions = 1,
            ActiveCachedSessions = 1,
            TotalCacheMemoryBytes = partition.PartitionSizeBytes
        };
    }

    public async Task<CachePerformanceMetrics> GetPerformanceMetricsAsync()
    {
        var metrics = new CachePerformanceMetrics
        {
            TotalGetRequests = _totalGets,
            TotalSetRequests = _totalSets,
            TotalDeleteRequests = _totalDeletes,
            TotalHits = _hits,
            TotalMisses = _misses,
            TotalEvictions = _evictions,
            MeasuredAt = DateTime.UtcNow
        };

        _performanceHistory.Add(metrics);
        return metrics;
    }

    public async Task<CacheEntry<object>?> GetEntryDetailsAsync(string sessionId, string key)
    {
        var partition = await GetPartitionAsync(sessionId);
        partition.Entries.TryGetValue(key, out var entry);
        return entry as CacheEntry<object>;
    }

    public async Task<CachePartition?> GetPartitionDetailsAsync(string sessionId)
    {
        _partitions.TryGetValue(sessionId, out var partition);
        return partition;
    }

    public async Task<long> GetMemoryUsageAsync()
    {
        return _partitions.Values.Sum(p => p.PartitionSizeBytes);
    }

    public async Task<long> GetSessionMemoryUsageAsync(string sessionId)
    {
        if (_partitions.TryGetValue(sessionId, out var partition))
        {
            return partition.PartitionSizeBytes;
        }

        return 0;
    }

    public async Task<bool> PreloadAsync(string sessionId)
    {
        var commonKeys = new[] { $"{sessionId}:status", $"{sessionId}:state:current", $"{sessionId}:pipeline:snapshot" };

        foreach (var key in commonKeys)
        {
            await RefreshAsync(sessionId, key);
        }

        return true;
    }

    public async Task<bool> RefreshAsync(string sessionId, string key)
    {
        return await SetTtlAsync(sessionId, key, _config.DefaultTtlSeconds);
    }

    public async Task<int> RefreshAllAsync(string sessionId)
    {
        var partition = await GetPartitionAsync(sessionId);

        foreach (var key in partition.Entries.Keys)
        {
            await RefreshAsync(sessionId, key);
        }

        return partition.Entries.Count;
    }

    public async Task<bool> SyncAsync(string sessionId)
    {
        // Sync cache with source systems
        return true;
    }

    public void SubscribeToUpdates(string sessionId, Func<StatusUpdateBatch, Task> callback)
    {
        if (!_subscribers.ContainsKey(sessionId))
        {
            _subscribers[sessionId] = new();
        }

        _subscribers[sessionId].Add(callback);
    }

    public void UnsubscribeFromUpdates(string sessionId)
    {
        if (_subscribers.ContainsKey(sessionId))
        {
            _subscribers[sessionId].Clear();
        }
    }

    public async Task BroadcastUpdateAsync(StatusUpdateBatch batch)
    {
        if (_subscribers.TryGetValue(batch.SessionId, out var callbacks))
        {
            await Task.WhenAll(callbacks.Select(cb => cb(batch)));
        }
    }

    public async IAsyncEnumerable<StatusUpdateBatch> GetUpdateStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Simulate updates from queue
            if (_updateQueues.TryGetValue(sessionId, out var updates) && updates.Count > 0)
            {
                yield return new StatusUpdateBatch
                {
                    SessionId = sessionId,
                    Updates = updates.ToList()
                };

                updates.Clear();
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public void Configure(StatusCacheConfig config)
    {
        _config = config;
    }

    public StatusCacheConfig GetConfiguration()
    {
        return _config;
    }

    public async Task<int> ArchiveOldEntriesAsync(int olderThanDays)
    {
        int archived = 0;
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        foreach (var partition in _partitions.Values)
        {
            var oldKeys = partition.Entries
                .Where(kvp => ((CacheEntry<object>)kvp.Value).CreatedAt < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldKeys)
            {
                partition.Entries.Remove(key);
                archived++;
            }
        }

        return archived;
    }

    public async Task<bool> CompactAsync(string sessionId)
    {
        if (!_partitions.TryGetValue(sessionId, out var partition))
        {
            return false;
        }

        // Remove expired entries
        var expiredKeys = partition.Entries
            .Where(kvp => ((CacheEntry<object>)kvp.Value).IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            partition.Entries.Remove(key);
        }

        UpdatePartitionSize(partition);
        return true;
    }

    public async Task<int> FlushAllAsync()
    {
        int total = _partitions.Sum(p => p.Value.EntryCount);
        _partitions.Clear();
        return total;
    }

    public async Task<bool> ResetStatisticsAsync()
    {
        _totalGets = 0;
        _totalSets = 0;
        _totalDeletes = 0;
        _hits = 0;
        _misses = 0;
        _evictions = 0;
        _performanceHistory.Clear();

        return true;
    }

    // Private Helper Methods

    private long EstimateSize<T>(T value)
    {
        // Rough estimation
        return value?.ToString()?.Length ?? 0;
    }

    private void UpdatePartitionSize(CachePartition partition)
    {
        partition.PartitionSizeBytes = partition.Entries.Values
            .Cast<CacheEntry<object>>()
            .Sum(e => e.Size);
    }

    private async Task EvictEntriesAsync(string sessionId)
    {
        var partition = await GetPartitionAsync(sessionId);

        switch (_config.EvictionPolicy.ToUpper())
        {
            case "LRU":
                var lruKey = partition.Entries
                    .OrderBy(kvp => ((CacheEntry<object>)kvp.Value).LastAccessedAt)
                    .First().Key;
                partition.Entries.Remove(lruKey);
                _evictions++;
                break;

            case "FIFO":
                var fifoKey = partition.Entries
                    .OrderBy(kvp => ((CacheEntry<object>)kvp.Value).CreatedAt)
                    .First().Key;
                partition.Entries.Remove(fifoKey);
                _evictions++;
                break;
        }

        partition.EntryCount = partition.Entries.Count;
        UpdatePartitionSize(partition);
    }

    private void RecordInvalidation(string sessionId, string key, string reason)
    {
        if (!_invalidationEvents.ContainsKey(sessionId))
        {
            _invalidationEvents[sessionId] = new();
        }

        _invalidationEvents[sessionId].Add(new CacheInvalidationEvent
        {
            Key = key,
            Reason = reason,
            InvalidatedAt = DateTime.UtcNow
        });
    }

    private System.Text.RegularExpressions.Regex PatternToRegex(string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return new System.Text.RegularExpressions.Regex(regexPattern);
    }

    private async Task ExecuteWarmUpAsync(string sessionId, CacheWarmUpPlan plan)
    {
        plan.Status = "InProgress";

        foreach (var key in plan.KeysToWarmUp)
        {
            if (plan.WarmupFunction != null)
            {
                var value = await plan.WarmupFunction(key);
                await SetAsync(sessionId, key, value, _config.DefaultTtlSeconds);
            }

            plan.WarmedUpKeys++;
        }

        plan.Status = "Completed";
    }
}
