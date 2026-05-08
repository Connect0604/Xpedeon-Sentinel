# Form 12: Status Cache Service

## Overview

The **Status Cache Service** (`IStatusCacheService`/`StatusCacheService`) provides efficient in-memory caching and distribution of pipeline status with TTL management, invalidation strategies, and real-time update streaming. It enables fast status queries while maintaining consistency through automatic expiration and event-driven invalidation.

**Key Features:**
- In-memory key-value caching with session partitioning
- TTL (Time-To-Live) management with automatic expiration
- Multiple eviction policies (LRU, FIFO, LFU)
- Batch operations for efficient multi-key updates
- Pattern-based key matching and invalidation
- Stale-while-revalidate support for graceful degradation
- Cache warm-up planning and execution
- Real-time update subscriptions and streaming
- Comprehensive statistics and performance metrics
- Session-level memory management
- Cache partitioning for isolation
- Configurable thresholds and retention

**Responsibilities:**
- Store and retrieve cached status efficiently
- Manage entry expiration with TTL
- Invalidate cache based on patterns and events
- Distribute updates to subscribers
- Track cache performance metrics
- Enforce size and entry count limits
- Support phase and state-specific caching

## Architecture

### Cache Entry Structure

**CacheEntry<T>** - Individual cache item
- `Key`: Unique identifier within partition
- `Value`: Cached data
- `CreatedAt` / `LastAccessedAt`: Timestamp metadata
- `ExpiresAt`: Expiration deadline (nullable for persistent entries)
- `AccessCount`: Number of successful retrievals
- `Size`: Estimated memory usage in bytes
- `IsExpired`: Calculated property checking deadline

### Partitioning

**CachePartition** - Session-level isolation
- `PartitionId`: Unique partition identifier
- `SessionId`: Associated session
- `Entries`: Dictionary<string, CacheEntry<object>> storage
- `PartitionSizeBytes`: Current memory usage
- `EntryCount`: Number of cached items
- `CreatedAt` / `LastAccessedAt`: Partition lifecycle tracking

### Status-Specific Models

**CachedStatus** - Overall pipeline snapshot
- Session ID and caching timestamp
- Current phase and progress (0-100)
- Overall pipeline progress
- Elapsed and estimated remaining time
- Quick metrics: discrepancies, issues, quality, risk scores
- Notification counts

**PhaseStatusCache** - Per-phase details
- Phase name, number, status (Pending/Running/Completed/Failed/Skipped)
- Progress percentage and step counters
- Duration and estimated remaining time
- Last activity description and timestamp

**PipelineStatusSnapshot** - All phases summary
- Overall status and progress
- List of all phase statuses
- Completion counters

**PipelineCurrentState** - Operational state
- Current phase and step information
- Progress and timeline data
- Active user count
- Issue counts (total, blocking, unresolved)
- Quality and risk scores

### Cache Configuration

**StatusCacheConfig** - Service settings
- `EnableCaching`: Boolean feature flag
- `DefaultTtlSeconds`: Default expiration (30 seconds)
- `MaxCacheSizeMb`: Memory limit (100 MB)
- `MaxEntriesPerSession`: Entry count limit (1000)
- `EvictionPolicy`: LRU | FIFO | LFU
- `EnableStatistics`: Metrics tracking
- `InvalidationStrategy`: Expiration and invalidation rules
- `EnableCompression`: Data compression
- `StaleWhileRevalidateSeconds`: Graceful degradation window (5 seconds)
- `TrackMetrics`: Performance monitoring

## Interface: IStatusCacheService

### Core Operations (9 methods)
```csharp
Task<T?> GetAsync<T>(string sessionId, string key);
Task<bool> SetAsync<T>(string sessionId, string key, T value, int? ttlSeconds = null);
Task<bool> SetBatchAsync<T>(string sessionId, Dictionary<string, T> values, int? ttlSeconds = null);
Task<bool> DeleteAsync(string sessionId, string key);
Task<int> DeleteBatchAsync(string sessionId, List<string> keys);
Task<bool> ClearAsync(string sessionId);
Task<bool> ExistsAsync(string sessionId, string key);
Task<List<string>> GetKeysAsync(string sessionId, string pattern = "*");
Task<CacheEntry<object>?> GetEntryDetailsAsync(string sessionId, string key);
```

### Status-Specific Operations (10 methods)
```csharp
Task<CachedStatus?> GetStatusAsync(string sessionId);
Task<bool> UpdateStatusAsync(string sessionId, CachedStatus status);
Task<PipelineStatusSnapshot?> GetPipelineSnapshotAsync(string sessionId);
Task<bool> CachePipelineSnapshotAsync(string sessionId, PipelineStatusSnapshot snapshot);
Task<PhaseStatusCache?> GetPhaseStatusAsync(string sessionId, string phaseName);
Task<bool> UpdatePhaseStatusAsync(string sessionId, string phaseName, PhaseStatusCache status);
Task<List<PhaseStatusCache>> GetAllPhaseStatusesAsync(string sessionId);
Task<PipelineCurrentState?> GetCurrentStateAsync(string sessionId);
Task<bool> UpdateCurrentStateAsync(string sessionId, PipelineCurrentState state);
Task<CachePartition?> GetPartitionAsync(string sessionId);
```

### Batch Updates (3 methods)
```csharp
Task<bool> ApplyBatchUpdateAsync(string sessionId, StatusUpdateBatch batch);
Task<bool> QueueUpdateAsync(string sessionId, StatusUpdate update);
Task<int> FlushUpdatesAsync(string sessionId);
```

### TTL Management (5 methods)
```csharp
Task<bool> SetTtlAsync(string sessionId, string key, int ttlSeconds);
Task<bool> ExtendTtlAsync(string sessionId, string key, int additionalSeconds);
Task<int?> GetTtlAsync(string sessionId, string key);
Task<bool> PersistAsync(string sessionId, string key);
Task<int> RemoveExpiredAsync(string sessionId);
```

### Invalidation (6 methods)
```csharp
Task<bool> InvalidateAsync(string sessionId, string key, string? reason = null);
Task<int> InvalidateByPatternAsync(string sessionId, string pattern, string? reason = null);
Task<int> InvalidatePhaseAsync(string sessionId, string phaseName);
Task<bool> InvalidateSessionAsync(string sessionId, string? reason = null);
Task<List<CacheInvalidationEvent>> GetInvalidationEventsAsync(string sessionId, int? maxCount = null);
Task<int> RemoveAllExpiredAsync();
```

### Warm-up (3 methods)
```csharp
Task<CacheWarmUpPlan> StartWarmUpAsync(string sessionId, List<string> keysToWarmUp, Func<string, Task<object>> warmupFunction);
Task<CacheWarmUpPlan?> GetWarmUpProgressAsync(string sessionId);
Task<bool> CancelWarmUpAsync(string sessionId);
```

### Stale-While-Revalidate (2 methods)
```csharp
Task<StaleCacheEntry<T>?> GetStaleAsync<T>(string sessionId, string key);
Task<T?> RevalidateAsync<T>(string sessionId, string key);
```

### Statistics and Monitoring (7 methods)
```csharp
Task<CacheStatistics> GetStatisticsAsync();
Task<CacheStatistics> GetSessionStatisticsAsync(string sessionId);
Task<CachePerformanceMetrics> GetPerformanceMetricsAsync();
Task<long> GetMemoryUsageAsync();
Task<long> GetSessionMemoryUsageAsync(string sessionId);
Task<CachePartition?> GetPartitionDetailsAsync(string sessionId);
Task<bool> ResetStatisticsAsync();
```

### Real-time Updates (4 methods)
```csharp
void SubscribeToUpdates(string sessionId, Func<StatusUpdateBatch, Task> callback);
void UnsubscribeFromUpdates(string sessionId);
Task BroadcastUpdateAsync(StatusUpdateBatch batch);
IAsyncEnumerable<StatusUpdateBatch> GetUpdateStreamAsync(string sessionId, CancellationToken ct);
```

### Configuration (2 methods)
```csharp
void Configure(StatusCacheConfig config);
StatusCacheConfig GetConfiguration();
```

### Bulk Operations (6 methods)
```csharp
Task<bool> PreloadAsync(string sessionId);
Task<bool> RefreshAsync(string sessionId, string key);
Task<int> RefreshAllAsync(string sessionId);
Task<bool> SyncAsync(string sessionId);
Task<bool> CompactAsync(string sessionId);
Task<int> FlushAllAsync();
```

## Usage Examples

### Basic Caching
```csharp
var service = new StatusCacheService();

// Initialize session cache
await service.InitializeCacheAsync("session-123");

// Store and retrieve values
await service.SetAsync("session-123", "status", "Running", ttlSeconds: 30);
var status = await service.GetAsync<string>("session-123", "status");
Console.WriteLine($"Status: {status}");

// Check if key exists
var exists = await service.ExistsAsync("session-123", "status");
```

### Phase Status Caching
```csharp
// Cache phase-specific information
var phaseStatus = new PhaseStatusCache
{
    PhaseName = "Discovery",
    Status = "Running",
    Progress = 75,
    CompletedSteps = 15,
    TotalSteps = 20
};

await service.UpdatePhaseStatusAsync("session-123", "Discovery", phaseStatus);

// Retrieve phase status
var retrieved = await service.GetPhaseStatusAsync("session-123", "Discovery");
Console.WriteLine($"Discovery: {retrieved.Progress}% ({retrieved.CompletedSteps}/{retrieved.TotalSteps})");

// Get all phases
var allPhases = await service.GetAllPhaseStatusesAsync("session-123");
foreach (var phase in allPhases)
{
    Console.WriteLine($"{phase.PhaseName}: {phase.Status}");
}
```

### Batch Operations
```csharp
// Set multiple values at once
var batch = new Dictionary<string, string>
{
    { "phase", "Execution" },
    { "progress", "50" },
    { "status", "Running" }
};
await service.SetBatchAsync("session-123", batch, ttlSeconds: 30);

// Apply batch update with custom logic
var updates = new StatusUpdateBatch
{
    SessionId = "session-123",
    Updates = new()
    {
        new StatusUpdate { Key = "metrics:quality", Value = 88, UpdateType = "Set" },
        new StatusUpdate { Key = "metrics:risk", Value = 25, UpdateType = "Set" }
    }
};
await service.ApplyBatchUpdateAsync("session-123", updates);
```

### TTL and Expiration
```csharp
// Set value with 60-second TTL
await service.SetAsync("session-123", "temp-data", someData, ttlSeconds: 60);

// Check remaining TTL
var ttl = await service.GetTtlAsync("session-123", "temp-data");
Console.WriteLine($"Expires in: {ttl} seconds");

// Extend expiration
await service.ExtendTtlAsync("session-123", "temp-data", additionalSeconds: 30);

// Make entry persistent (no expiration)
await service.PersistAsync("session-123", "temp-data");

// Remove expired entries
var removed = await service.RemoveExpiredAsync("session-123");
Console.WriteLine($"Removed {removed} expired entries");
```

### Pattern-Based Operations
```csharp
// Find all user-related keys
var userKeys = await service.GetKeysAsync("session-123", "user:*");
foreach (var key in userKeys)
{
    Console.WriteLine($"Found: {key}");
}

// Invalidate all metrics
var count = await service.InvalidateByPatternAsync("session-123", "metrics:*", "StaleMetrics");
Console.WriteLine($"Invalidated {count} metric entries");

// Invalidate all phase data
await service.InvalidatePhaseAsync("session-123", "Discovery");

// Get invalidation history
var events = await service.GetInvalidationEventsAsync("session-123", maxCount: 10);
foreach (var evt in events)
{
    Console.WriteLine($"[{evt.InvalidatedAt}] {evt.Key} - {evt.Reason}");
}
```

### Cache Warm-up
```csharp
// Pre-load critical data
var keysToWarmUp = new List<string>
{
    "session-123:status",
    "session-123:pipeline:snapshot",
    "session-123:metrics:summary"
};

var warmupPlan = await service.StartWarmUpAsync(
    "session-123",
    keysToWarmUp,
    async key => await LoadDataFromSourceAsync(key));

// Monitor warm-up progress
while (warmupPlan.Status == "InProgress")
{
    Console.WriteLine($"Warmed up {warmupPlan.WarmedUpKeys}/{warmupPlan.TotalKeysToWarmUp}");
    await Task.Delay(1000);
    warmupPlan = await service.GetWarmUpProgressAsync("session-123");
}

Console.WriteLine($"Warm-up completed: {warmupPlan.Status}");
```

### Stale-While-Revalidate
```csharp
// Try to get value, fallback to stale if expired
var value = await service.GetAsync<string>("session-123", "critical-status");

if (value == null)
{
    // Get stale value while revalidating
    var stale = await service.GetStaleAsync<string>("session-123", "critical-status");
    
    if (stale?.CanRevalidate == true)
    {
        Console.WriteLine($"Using stale value: {stale.StaleValue}");
        // Trigger revalidation asynchronously
        _ = service.RevalidateAsync<string>("session-123", "critical-status");
    }
}
```

### Statistics and Monitoring
```csharp
// Get overall cache statistics
var stats = await service.GetStatisticsAsync();
Console.WriteLine($"Cached sessions: {stats.TotalCachedSessions}");
Console.WriteLine($"Memory usage: {stats.TotalCacheMemoryBytes / 1024}KB");
Console.WriteLine($"Hit rate: {stats.CacheHitRate:F2}%");

// Get session-specific statistics
var sessionStats = await service.GetSessionStatisticsAsync("session-123");
Console.WriteLine($"Session memory: {sessionStats.TotalCacheMemoryBytes}B");

// Get performance metrics
var perf = await service.GetPerformanceMetricsAsync();
Console.WriteLine($"Gets: {perf.TotalGetRequests}");
Console.WriteLine($"Sets: {perf.TotalSetRequests}");
Console.WriteLine($"Hit/Miss ratio: {perf.TotalHits}/{perf.TotalMisses}");

// Get partition details
var partition = await service.GetPartitionDetailsAsync("session-123");
Console.WriteLine($"Entries in partition: {partition.EntryCount}");
```

### Real-time Updates
```csharp
// Subscribe to status updates
service.SubscribeToUpdates("session-123", async batch =>
{
    Console.WriteLine($"[{batch.UpdateTime:HH:mm:ss}] Received {batch.Updates.Count} updates");
    foreach (var update in batch.Updates)
    {
        Console.WriteLine($"  {update.Key} = {update.Value}");
    }
});

// Or use async streaming
await foreach (var batch in service.GetUpdateStreamAsync("session-123"))
{
    Console.WriteLine($"Update batch with {batch.Updates.Count} items");
}

// Broadcast an update
var updateBatch = new StatusUpdateBatch
{
    SessionId = "session-123",
    Updates = new()
    {
        new StatusUpdate { Key = "progress", Value = 75 }
    }
};
await service.BroadcastUpdateAsync(updateBatch);
```

### Maintenance Operations
```csharp
// Preload common keys
await service.PreloadAsync("session-123");

// Refresh specific entry TTL
await service.RefreshAsync("session-123", "status");

// Refresh all entries
var refreshed = await service.RefreshAllAsync("session-123");
Console.WriteLine($"Refreshed {refreshed} entries");

// Compact cache (remove expired, optimize)
await service.CompactAsync("session-123");

// Archive entries older than 30 days
var archived = await service.ArchiveOldEntriesAsync(olderThanDays: 30);
Console.WriteLine($"Archived {archived} old entries");

// Clear entire cache
await service.ClearAsync("session-123");
```

## Configuration

```csharp
var config = new StatusCacheConfig
{
    EnableCaching = true,
    DefaultTtlSeconds = 30,           // 30-second default expiration
    MaxCacheSizeMb = 100,             // 100 MB limit
    MaxEntriesPerSession = 1000,      // 1000 entries per session
    EvictionPolicy = "LRU",           // Least Recently Used eviction
    EnableStatistics = true,          // Track hit/miss rates
    EnableCompression = true,         // Compress cached data
    StaleWhileRevalidateSeconds = 5,  // 5-second graceful degradation window
    TrackMetrics = true,              // Monitor performance
    InvalidationStrategy = new CacheInvalidationStrategy
    {
        InvalidateOnPhaseChange = true,
        InvalidateOnProgressUpdate = false,
        InvalidateOnIssueDetected = true,
        TtlSeconds = 30
    }
};

service.Configure(config);
```

## Eviction Policies

**LRU (Least Recently Used)** - Default
- Removes entry with oldest `LastAccessedAt` when size exceeded
- Optimal for working sets and temporal locality

**FIFO (First-In-First-Out)**
- Removes oldest entry by `CreatedAt` time
- Simple, predictable behavior

**LFU (Least Frequently Used)**
- Removes entry with lowest `AccessCount`
- Optimal when access patterns are stable

## Performance Characteristics

**Time Complexity:**
- `GetAsync`: O(1) - direct dictionary lookup
- `SetAsync`: O(1) - direct insertion
- `DeleteAsync`: O(1) - direct removal
- `GetKeysAsync`: O(n) - pattern matching across n entries
- `InvalidateByPatternAsync`: O(n) - regex match across entries
- `RemoveExpiredAsync`: O(n) - iterate checking expiration
- Eviction: O(n) - sorting for LRU/LFU

**Space Complexity:**
- Per-partition: O(n) where n = entry count
- Per-entry: ~100-200 bytes base + data size
- Typical: 10,000 entries ≈ 10 MB

**Scalability:**
- 100K entries: ~100 MB memory, 1-2ms latency
- 1M entries: ~1 GB memory, 10-20ms latency
- Parallel access: Thread-safe dictionary operations

## Integration Patterns

### With Progress Service
```csharp
// Cache progress snapshots for quick access
var snapshot = new PipelineStatusSnapshot { /* ... */ };
await statusCacheService.CachePipelineSnapshotAsync(sessionId, snapshot);
```

### With Metrics Service
```csharp
// Cache computed metrics
var metrics = new CachedStatus
{
    QualityScore = 85,
    RiskScore = 25,
    OverallProgress = 75
};
await statusCacheService.UpdateStatusAsync(sessionId, metrics);
```

### With Dashboard Service
```csharp
// Broadcast cache updates to dashboard subscribers
service.SubscribeToUpdates(sessionId, async batch =>
{
    await dashboardService.BroadcastUpdateAsync(batch);
});
```

## Testing Strategy

The test suite (`StatusCacheServiceTests.cs`) includes 50+ unit tests covering:
- Basic cache operations (get, set, delete)
- Batch operations
- TTL and expiration management
- Pattern-based invalidation
- Status-specific caching
- Warm-up and preload
- Stale-while-revalidate
- Statistics and monitoring
- Configuration
- Real-time updates
- Integration scenarios

## Related Forms

- **Form 10: Progress Service** - Provides data to cache
- **Form 11: Metrics Service** - Metrics distribution
- **Form 9: Dashboard Service** - Consumes cached status
- **Form 13: Notification Service** - Uses cache for alerts
- **Form 16: Ruflow Orchestrator** - Coordinates caching

## See Also

- `IStatusCacheService`: Interface definition with 60+ methods
- `StatusCacheService`: Implementation with in-memory storage
- `StatusCacheModels.cs`: All model definitions
- `StatusCacheServiceTests.cs`: 50+ comprehensive unit tests
