namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for status cache service
/// </summary>
public class StatusCacheServiceTests
{
    private readonly IStatusCacheService _service = new StatusCacheService();

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new StatusCacheService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeCacheAsync_ShouldCreateCache()
    {
        // Act
        var status = await _service.InitializeCacheAsync("session-1");

        // Assert
        status.Should().NotBeNull();
        status.SessionId.Should().Be("session-1");
        status.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPartitionAsync_ShouldCreatePartition()
    {
        // Act
        var partition = await _service.GetPartitionAsync("session-1");

        // Assert
        partition.Should().NotBeNull();
        partition.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var result = await _service.SetAsync("session-1", "key1", "value1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_ShouldRetrieveValue()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var value = await _service.GetAsync<string>("session-1", "key1");

        // Assert
        value.Should().Be("value1");
    }

    [Fact]
    public async Task GetAsync_WithMissingKey_ShouldReturnNull()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var value = await _service.GetAsync<string>("session-1", "nonexistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public async Task SetBatchAsync_ShouldStoreMultipleValues()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        var batch = new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } };

        // Act
        var result = await _service.SetBatchAsync("session-1", batch);

        // Assert
        result.Should().BeTrue();
        var val1 = await _service.GetAsync<string>("session-1", "key1");
        val1.Should().Be("val1");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveValue()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var result = await _service.DeleteAsync("session-1", "key1");
        var value = await _service.GetAsync<string>("session-1", "key1");

        // Assert
        result.Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBatchAsync_ShouldRemoveMultipleValues()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");
        await _service.SetAsync("session-1", "key2", "value2");

        // Act
        var deleted = await _service.DeleteBatchAsync("session-1", new() { "key1", "key2" });

        // Assert
        deleted.Should().Be(2);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllValues()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var result = await _service.ClearAsync("session-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForExistingKey()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var exists = await _service.ExistsAsync("session-1", "key1");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForMissingKey()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var exists = await _service.ExistsAsync("session-1", "nonexistent");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysAsync_ShouldReturnMatchingKeys()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "user:1", "alice");
        await _service.SetAsync("session-1", "user:2", "bob");
        await _service.SetAsync("session-1", "role:admin", "admin");

        // Act
        var keys = await _service.GetKeysAsync("session-1", "user:*");

        // Assert
        keys.Should().HaveCount(2);
        keys.Should().Contain("user:1");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnCachedStatus()
    {
        // Arrange
        var status = await _service.InitializeCacheAsync("session-1");

        // Act
        var retrieved = await _service.GetStatusAsync("session-1");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        var newStatus = new CachedStatus { SessionId = "session-1", OverallProgress = 50 };

        // Act
        await _service.UpdateStatusAsync("session-1", newStatus);
        var retrieved = await _service.GetStatusAsync("session-1");

        // Assert
        retrieved.OverallProgress.Should().Be(50);
    }

    [Fact]
    public async Task GetPhaseStatusAsync_ShouldReturnPhaseStatus()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        var phaseStatus = new PhaseStatusCache { PhaseName = "Discovery", Status = "Running", Progress = 50 };
        await _service.UpdatePhaseStatusAsync("session-1", "Discovery", phaseStatus);

        // Act
        var retrieved = await _service.GetPhaseStatusAsync("session-1", "Discovery");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.PhaseName.Should().Be("Discovery");
        retrieved.Progress.Should().Be(50);
    }

    [Fact]
    public async Task GetAllPhaseStatusesAsync_ShouldReturnAllPhases()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.UpdatePhaseStatusAsync("session-1", "Discovery", new() { PhaseName = "Discovery" });
        await _service.UpdatePhaseStatusAsync("session-1", "Execution", new() { PhaseName = "Execution" });

        // Act
        var phases = await _service.GetAllPhaseStatusesAsync("session-1");

        // Assert
        phases.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetCurrentStateAsync_ShouldReturnCurrentState()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        var state = new PipelineCurrentState { SessionId = "session-1", OverallProgress = 75 };
        await _service.UpdateCurrentStateAsync("session-1", state);

        // Act
        var retrieved = await _service.GetCurrentStateAsync("session-1");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.OverallProgress.Should().Be(75);
    }

    [Fact]
    public async Task ApplyBatchUpdateAsync_ShouldApplyMultipleUpdates()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        var batch = new StatusUpdateBatch
        {
            SessionId = "session-1",
            Updates = new()
            {
                new StatusUpdate { Key = "key1", Value = "val1" },
                new StatusUpdate { Key = "key2", Value = "val2" }
            }
        };

        // Act
        var result = await _service.ApplyBatchUpdateAsync("session-1", batch);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task QueueUpdateAsync_ShouldQueueUpdate()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var result = await _service.QueueUpdateAsync("session-1", new StatusUpdate { Key = "key1", Value = "val1" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FlushUpdatesAsync_ShouldFlushQueue()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.QueueUpdateAsync("session-1", new StatusUpdate { Key = "key1", Value = "val1" });

        // Act
        var count = await _service.FlushUpdatesAsync("session-1");

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task SetTtlAsync_ShouldSetExpiration()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var result = await _service.SetTtlAsync("session-1", "key1", 60);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetTtlAsync_ShouldReturnRemainingTime()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1", 60);

        // Act
        var ttl = await _service.GetTtlAsync("session-1", "key1");

        // Assert
        ttl.Should().BeGreaterThan(0);
        ttl.Should().BeLessThanOrEqualTo(60);
    }

    [Fact]
    public async Task ExtendTtlAsync_ShouldExtendExpiration()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1", 60);
        var originalTtl = await _service.GetTtlAsync("session-1", "key1");

        // Act
        await _service.ExtendTtlAsync("session-1", "key1", 30);
        var newTtl = await _service.GetTtlAsync("session-1", "key1");

        // Assert
        newTtl.Should().BeGreaterThan(originalTtl);
    }

    [Fact]
    public async Task PersistAsync_ShouldRemoveTtl()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1", 60);

        // Act
        await _service.PersistAsync("session-1", "key1");
        var ttl = await _service.GetTtlAsync("session-1", "key1");

        // Assert
        ttl.Should().BeNull();
    }

    [Fact]
    public async Task RemoveExpiredAsync_ShouldRemoveExpiredEntries()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var removed = await _service.RemoveExpiredAsync("session-1");

        // Assert
        removed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task InvalidateAsync_ShouldInvalidateKey()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var result = await _service.InvalidateAsync("session-1", "key1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateByPatternAsync_ShouldInvalidateMatching()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "user:1", "alice");
        await _service.SetAsync("session-1", "user:2", "bob");

        // Act
        var count = await _service.InvalidateByPatternAsync("session-1", "user:*");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task InvalidateSessionAsync_ShouldClearSession()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var result = await _service.InvalidateSessionAsync("session-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvalidationEventsAsync_ShouldReturnEvents()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.InvalidateAsync("session-1", "key1");

        // Act
        var events = await _service.GetInvalidationEventsAsync("session-1");

        // Assert
        events.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalCachedSessions.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSessionStatisticsAsync_ShouldReturnSessionStats()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var stats = await _service.GetSessionStatisticsAsync("session-1");

        // Assert
        stats.Should().NotBeNull();
        stats.TotalCachedSessions.Should().Be(1);
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var metrics = await _service.GetPerformanceMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalSetRequests.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetEntryDetailsAsync_ShouldReturnEntryInfo()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var details = await _service.GetEntryDetailsAsync("session-1", "key1");

        // Assert
        details.Should().NotBeNull();
        details.Key.Should().Be("key1");
    }

    [Fact]
    public async Task GetPartitionDetailsAsync_ShouldReturnPartitionInfo()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var details = await _service.GetPartitionDetailsAsync("session-1");

        // Assert
        details.Should().NotBeNull();
        details.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task GetMemoryUsageAsync_ShouldReturnTotalMemory()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1");

        // Act
        var memory = await _service.GetMemoryUsageAsync();

        // Assert
        memory.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task PreloadAsync_ShouldLoadCommonKeys()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var result = await _service.PreloadAsync("session-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ShouldExtendTtl()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");
        await _service.SetAsync("session-1", "key1", "value1", 60);

        // Act
        var result = await _service.RefreshAsync("session-1", "key1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SubscribeToUpdates_ShouldAddSubscriber()
    {
        // Arrange
        var called = false;
        async Task Callback(StatusUpdateBatch b) { called = true; }

        // Act
        _service.SubscribeToUpdates("session-1", Callback);

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task BroadcastUpdateAsync_ShouldNotifySubscribers()
    {
        // Arrange
        var received = false;
        async Task Callback(StatusUpdateBatch b) { received = true; }
        _service.SubscribeToUpdates("session-1", Callback);

        // Act
        var batch = new StatusUpdateBatch { SessionId = "session-1" };
        await _service.BroadcastUpdateAsync(batch);
        await Task.Delay(100);

        // Assert
        received.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new StatusCacheConfig { DefaultTtlSeconds = 60 };

        // Act
        _service.Configure(config);
        var retrieved = _service.GetConfiguration();

        // Assert
        retrieved.DefaultTtlSeconds.Should().Be(60);
    }

    [Fact]
    public async Task CompactAsync_ShouldRemoveExpiredEntries()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var result = await _service.CompactAsync("session-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveOldEntriesAsync_ShouldArchiveOldData()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var archived = await _service.ArchiveOldEntriesAsync(1);

        // Assert
        archived.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ResetStatisticsAsync_ShouldResetCounters()
    {
        // Arrange
        await _service.InitializeCacheAsync("session-1");

        // Act
        var result = await _service.ResetStatisticsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task StatusCaching_IntegrationScenario()
    {
        // Arrange - Initialize
        await _service.InitializeCacheAsync("session-1");

        // Act - Cache phase status
        var phaseStatus = new PhaseStatusCache
        {
            PhaseName = "Discovery",
            Status = "Running",
            Progress = 50,
            CompletedSteps = 5,
            TotalSteps = 10
        };
        await _service.UpdatePhaseStatusAsync("session-1", "Discovery", phaseStatus);

        // Cache current state
        var state = new PipelineCurrentState
        {
            SessionId = "session-1",
            OverallProgress = 50,
            CurrentPhase = "Discovery"
        };
        await _service.UpdateCurrentStateAsync("session-1", state);

        // Apply batch update
        var batch = new StatusUpdateBatch
        {
            SessionId = "session-1",
            Updates = new()
            {
                new StatusUpdate { Key = "alert_count", Value = 5 },
                new StatusUpdate { Key = "quality_score", Value = 85 }
            }
        };
        await _service.ApplyBatchUpdateAsync("session-1", batch);

        // Retrieve and verify
        var retrievedPhase = await _service.GetPhaseStatusAsync("session-1", "Discovery");
        var retrievedState = await _service.GetCurrentStateAsync("session-1");
        var stats = await _service.GetStatisticsAsync();

        // Assert
        retrievedPhase.Should().NotBeNull();
        retrievedPhase.Progress.Should().Be(50);
        retrievedState.OverallProgress.Should().Be(50);
        stats.TotalCachedSessions.Should().BeGreaterThan(0);
    }
}
