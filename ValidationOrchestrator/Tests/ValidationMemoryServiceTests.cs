namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using Moq;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for validation memory service (Form 3)
/// </summary>
public class ValidationMemoryServiceTests
{
    private const string TestClientId = "test-client-123";
    private const string TestSessionId = "test-session-456";

    private Mock<ICavemanCompressionService> CreateMockCompressionService()
    {
        var mock = new Mock<ICavemanCompressionService>();

        mock.Setup(m => m.CompressAsync(It.IsAny<string>(), It.IsAny<CompressionLevel>()))
            .ReturnsAsync((string content, CompressionLevel level) => new CompressionResult
            {
                Success = true,
                CompressedContent = content.Substring(0, Math.Min(content.Length / 2, 50)),
                OriginalContent = content,
                OriginalLength = content.Length,
                EstimatedReduction = 50
            });

        mock.Setup(m => m.DecompressAsync(It.IsAny<string>()))
            .ReturnsAsync((string compressed) => new DecompressionResult
            {
                Success = true,
                DecompressedContent = compressed + " [decompressed]",
                CompressedContent = compressed
            });

        return mock;
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();

        // Act
        var service = new ValidationMemoryService(mockCompression.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateNewSession()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        var session = await service.CreateSessionAsync(TestClientId, "Test Client");

        // Assert
        session.Should().NotBeNull();
        session.Id.Should().NotBeNullOrEmpty();
        session.ClientId.Should().Be(TestClientId);
        session.ClientName.Should().Be("Test Client");
        session.CurrentPhase.Should().Be(ValidationPhase.Initialize);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldInitializePhaseStatuses()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        var session = await service.CreateSessionAsync(TestClientId);

        // Assert
        session.PhaseStatuses.Should().HaveCount(Enum.GetValues(typeof(ValidationPhase)).Length);
        session.PhaseStatuses[ValidationPhase.Initialize].State.Should().Be(PhaseState.InProgress);
        session.PhaseStatuses[ValidationPhase.Discovery].State.Should().Be(PhaseState.Pending);
    }

    [Fact]
    public async Task GetSessionAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var created = await service.CreateSessionAsync(TestClientId);

        // Act
        var retrieved = await service.GetSessionAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(created.Id);
        retrieved.ClientId.Should().Be(TestClientId);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        var result = await service.GetSessionAsync("nonexistent-session");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldUpdateLastModified()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);
        var originalTime = session.LastUpdated;

        // Act
        await Task.Delay(10); // Ensure time difference
        await service.UpdateSessionAsync(session);

        // Assert
        session.LastUpdated.Should().BeGreaterThan(originalTime);
    }

    [Fact]
    public async Task TransitionPhaseAsync_ShouldChangePhases()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        // Act
        await service.TransitionPhaseAsync(session.Id, ValidationPhase.Discovery);
        var updated = await service.GetSessionAsync(session.Id);

        // Assert
        updated!.CurrentPhase.Should().Be(ValidationPhase.Discovery);
        updated.PhaseStatuses[ValidationPhase.Initialize].State.Should().Be(PhaseState.Completed);
        updated.PhaseStatuses[ValidationPhase.Discovery].State.Should().Be(PhaseState.InProgress);
    }

    [Fact]
    public async Task GetPhaseStatusAsync_ShouldReturnPhaseStatus()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        // Act
        var status = await service.GetSessionPhaseStatusAsync(session.Id, ValidationPhase.Discovery);

        // Assert
        status.Should().NotBeNull();
        status!.Phase.Should().Be(ValidationPhase.Discovery);
        status.State.Should().Be(PhaseState.Pending);
    }

    [Fact]
    public async Task StoreArtifactAsync_ShouldStoreCompressedArtifact()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var artifact = new CompressedArtifact
        {
            Phase = "Discovery",
            ArtifactType = "BusinessLogic",
            OriginalContent = "test content",
            CompressedContent = "compressed"
        };

        // Act
        await service.StoreArtifactAsync(session.Id, artifact);
        var retrieved = await service.GetArtifactAsync(session.Id, artifact.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.ArtifactType.Should().Be("BusinessLogic");
    }

    [Fact]
    public async Task GetPhaseArtifactsAsync_ShouldReturnAllArtifactsFromPhase()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var artifact1 = new CompressedArtifact { Phase = "Discovery", ArtifactType = "Type1" };
        var artifact2 = new CompressedArtifact { Phase = "Discovery", ArtifactType = "Type2" };

        // Act
        await service.StoreArtifactAsync(session.Id, artifact1);
        await service.StoreArtifactAsync(session.Id, artifact2);
        var artifacts = await service.GetPhaseArtifactsAsync(session.Id, ValidationPhase.Discovery);

        // Assert
        artifacts.Should().HaveCount(2);
    }

    [Fact]
    public async Task StoreDiscoveryFindingsAsync_ShouldStoreFindings()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var findings = new DiscoveryFindings
        {
            TotalBusinessLogicItems = 50,
            UndocumentedLogicItems = 10,
            CriticalModules = new List<string> { "Accounting", "Procurement" }
        };

        // Act
        await service.StoreDiscoveryFindingsAsync(session.Id, findings);
        var retrieved = await service.GetDiscoveryFindingsAsync(session.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.TotalBusinessLogicItems.Should().Be(50);
        retrieved.UndocumentedLogicItems.Should().Be(10);
    }

    [Fact]
    public async Task StoreExecutionResultsAsync_ShouldStoreResults()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var results = new ExecutionResults
        {
            TotalTestsRun = 100,
            LegacyPassedTests = 95,
            BlazonPassedTests = 92
        };

        // Act
        await service.StoreExecutionResultsAsync(session.Id, results);
        var retrieved = await service.GetExecutionResultsAsync(session.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.TotalTestsRun.Should().Be(100);
    }

    [Fact]
    public async Task StoreDiscrepanciesAsync_ShouldStoreDiscrepancies()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var discrepancies = new List<Discrepancy>
        {
            new Discrepancy { Module = "Accounting", Severity = DiscrepancySeverity.Critical },
            new Discrepancy { Module = "Procurement", Severity = DiscrepancySeverity.High }
        };

        // Act
        await service.StoreDiscrepanciesAsync(session.Id, discrepancies);
        var retrieved = await service.GetDiscrepanciesAsync(session.Id);

        // Assert
        retrieved.Should().HaveCount(2);
        retrieved.Count(d => d.Severity == DiscrepancySeverity.Critical).Should().Be(1);
    }

    [Fact]
    public async Task StoreRiskAssessmentAsync_ShouldStoreAssessment()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var assessment = new RiskAssessment
        {
            OverallRiskScore = 25.5,
            ValidationCompleteness = 98,
            GoNoGo = true
        };

        // Act
        await service.StoreRiskAssessmentAsync(session.Id, assessment);
        var retrieved = await service.GetRiskAssessmentAsync(session.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.OverallRiskScore.Should().Be(25.5);
        retrieved.GoNoGo.Should().BeTrue();
    }

    [Fact]
    public async Task GetMemoryStatistics_ShouldReturnValidStatistics()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var artifact = new CompressedArtifact
        {
            Phase = "Discovery",
            ArtifactType = "BusinessLogic",
            OriginalContent = "test content with longer length",
            CompressedContent = "compressed"
        };

        // Act
        await service.StoreArtifactAsync(session.Id, artifact);
        var stats = service.GetMemoryStatistics(session.Id);

        // Assert
        stats.Should().NotBeNull();
        stats.TotalArtifacts.Should().Be(1);
        stats.TotalOriginalSize.Should().BeGreaterThan(0);
        stats.TotalCompressedSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListSessionsAsync_ShouldReturnAllSessions()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        var session1 = await service.CreateSessionAsync("client-1");
        var session2 = await service.CreateSessionAsync("client-2");
        var sessions = await service.ListSessionsAsync();

        // Assert
        sessions.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ListSessionsAsync_WithClientId_ShouldFilterByClient()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        await service.CreateSessionAsync("client-1");
        await service.CreateSessionAsync("client-2");
        var sessions = await service.ListSessionsAsync("client-1");

        // Assert
        sessions.Should().AllSatisfy(s => s.ClientId.Should().Be("client-1"));
    }

    [Fact]
    public async Task ArchiveSessionAsync_ShouldMarkAsArchived()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        // Act
        await service.ArchiveSessionAsync(session.Id, "Test archive");
        var retrieved = await service.GetSessionAsync(session.Id);

        // Assert
        retrieved!.IsArchived.Should().BeTrue();
        retrieved.ArchiveReason.Should().Be("Test archive");
        retrieved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void GetCacheStatistics_ShouldReturnStats()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        var stats = service.GetCacheStatistics();

        // Assert
        stats.Should().ContainKey("TotalCacheItems");
        stats.Should().ContainKey("CacheMemoryUsage");
        stats.Should().ContainKey("SessionsInMemory");
    }

    [Fact]
    public void ClearCache_ShouldRemoveAllCachedItems()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);

        // Act
        service.ClearCache();
        var stats = service.GetCacheStatistics();

        // Assert
        stats["TotalCacheItems"].Should().Be(0);
    }

    [Fact]
    public async Task ClearSessionMemoryAsync_ShouldRemoveSession()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        // Act
        await service.ClearSessionMemoryAsync(session.Id);
        var retrieved = await service.GetSessionAsync(session.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePhaseStatusAsync_ShouldUpdateProgress()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var status = new SessionPhaseStatus
        {
            Phase = ValidationPhase.Discovery,
            State = PhaseState.InProgress,
            ItemsProcessed = 50,
            ProgressPercentage = 75
        };

        // Act
        await service.UpdateSessionPhaseStatusAsync(session.Id, status);
        var retrieved = await service.GetSessionPhaseStatusAsync(session.Id, ValidationPhase.Discovery);

        // Assert
        retrieved!.ItemsProcessed.Should().Be(50);
        retrieved.ProgressPercentage.Should().Be(75);
    }

    [Fact]
    public async Task GetContextForPhaseAsync_ShouldReturnPhaseContext()
    {
        // Arrange
        var mockCompression = CreateMockCompressionService();
        var service = new ValidationMemoryService(mockCompression.Object);
        var session = await service.CreateSessionAsync(TestClientId);

        var artifact = new CompressedArtifact
        {
            Phase = "Discovery",
            ArtifactType = "BusinessLogic",
            OriginalContent = "test content",
            CompressedContent = "compressed"
        };

        // Act
        await service.StoreArtifactAsync(session.Id, artifact);
        var context = await service.GetContextForPhaseAsync(session.Id, ValidationPhase.Discovery);

        // Assert
        context.Should().NotBeNullOrEmpty();
        context.Should().Contain("BusinessLogic");
    }
}
