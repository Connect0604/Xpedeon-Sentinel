namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for metrics calculation service
/// </summary>
public class MetricsServiceTests
{
    private readonly IMetricsService _service = new MetricsService();

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new MetricsService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeMetricsAsync_ShouldCreateMetrics()
    {
        // Act
        var metrics = await _service.InitializeMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
        metrics.SessionId.Should().Be("session-1");
        metrics.SchemaMetrics.Should().NotBeNull();
        metrics.DataMetrics.Should().NotBeNull();
        metrics.DiscrepancyMetrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var metrics = await _service.GetMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
        metrics.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldInitializeIfNotExists()
    {
        // Act
        var metrics = await _service.GetMetricsAsync("nonexistent");

        // Assert
        metrics.Should().NotBeNull();
        metrics.SessionId.Should().Be("nonexistent");
    }

    [Fact]
    public async Task CalculateSchemaMetricsAsync_ShouldCalculateMetrics()
    {
        // Arrange
        var analysis = new SchemaAnalysisResult
        {
            IsCompatible = true,
            SchemaMapping = new SchemaMapping
            {
                SourceTables = new List<TableInfo> { new() { TableName = "Table1" } },
                MappedTables = new List<TableMapping> { new() { SourceTable = "Table1", TargetTable = "Table1" } }
            }
        };

        // Act
        var metrics = await _service.CalculateSchemaMetricsAsync("session-1", analysis);

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalTableCount.Should().Be(1);
        metrics.MatchedTableCount.Should().Be(1);
        metrics.TableMatchPercentage.Should().Be(100);
    }

    [Fact]
    public async Task CalculateDataMetricsAsync_ShouldCalculateMetrics()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            SourceTable = "Users",
            TargetTable = "Users",
            SourceRowCount = 1000,
            TargetRowCount = 1000,
            MatchedRows = new() { new RowComparison() },
            MismatchedRows = new() { new RowComparison() }
        };

        // Act
        var metrics = await _service.CalculateDataMetricsAsync("session-1", comparison);

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalRowsLegacy.Should().Be(1000);
        metrics.TotalRowsBlazor.Should().Be(1000);
        metrics.MatchedRowCount.Should().Be(1);
        metrics.MismatchedRowCount.Should().Be(1);
    }

    [Fact]
    public async Task AggregateDataMetricsAsync_ShouldAggregateMultipleTables()
    {
        // Arrange
        var comparisons = new List<TableComparisonResult>
        {
            new() { SourceTable = "Table1", SourceRowCount = 500, TargetRowCount = 500, MatchedRows = new() { new RowComparison() } },
            new() { SourceTable = "Table2", SourceRowCount = 500, TargetRowCount = 500, MatchedRows = new() { new RowComparison() } }
        };

        // Act
        var metrics = await _service.AggregateDataMetricsAsync("session-1", comparisons);

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalRowsLegacy.Should().Be(1000);
        metrics.TotalRowsBlazor.Should().Be(1000);
    }

    [Fact]
    public async Task CalculateDiscrepancyMetricsAsync_ShouldCountBySeverity()
    {
        // Arrange
        var analysis = new DiscrepancyAnalysisResult
        {
            Discrepancies = new()
            {
                new DetailedDiscrepancy { Severity = DiscrepancySeverity.Critical },
                new DetailedDiscrepancy { Severity = DiscrepancySeverity.High },
                new DetailedDiscrepancy { Severity = DiscrepancySeverity.Medium }
            }
        };

        // Act
        var metrics = await _service.CalculateDiscrepancyMetricsAsync("session-1", analysis);

        // Assert
        metrics.TotalDiscrepancies.Should().Be(3);
        metrics.CriticalCount.Should().Be(1);
        metrics.HighCount.Should().Be(1);
        metrics.MediumCount.Should().Be(1);
    }

    [Fact]
    public async Task CalculateDiscrepancyMetricsAsync_ShouldCategorizeIssues()
    {
        // Arrange
        var analysis = new DiscrepancyAnalysisResult
        {
            Discrepancies = new()
            {
                new DetailedDiscrepancy { Category = DiscrepancyCategory.DataTypeIssue },
                new DetailedDiscrepancy { Category = DiscrepancyCategory.DataLoss }
            }
        };

        // Act
        var metrics = await _service.CalculateDiscrepancyMetricsAsync("session-1", analysis);

        // Assert
        metrics.DataTypeIssues.Should().Be(1);
        metrics.DataLossIssues.Should().Be(1);
    }

    [Fact]
    public async Task RecordPhasePerformanceAsync_ShouldRecordDuration()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        await _service.RecordPhasePerformanceAsync("session-1", "Discovery", 5000, 100);
        var metrics = await _service.GetMetricsAsync("session-1");

        // Assert
        metrics.PerformanceMetrics.DiscoveryDurationMs.Should().Be(5000);
    }

    [Fact]
    public async Task CalculatePerformanceMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        await _service.RecordPhasePerformanceAsync("session-1", "Discovery", 5000, 50);

        // Act
        var metrics = await _service.CalculatePerformanceMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
        metrics.DiscoveryDurationMs.Should().Be(5000);
    }

    [Fact]
    public async Task CalculateQualityMetricsAsync_ShouldCalculateScores()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var metrics = await _service.CalculateQualityMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
        metrics.OverallAccuracy.Should().BeGreaterThanOrEqualTo(0);
        metrics.OverallAccuracy.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task GetModuleQualityAsync_ShouldReturnModuleMetrics()
    {
        // Act
        var modules = await _service.GetModuleQualityAsync("session-1");

        // Assert
        modules.Should().NotBeNull();
        modules.Should().HaveCountGreaterThan(0);
        modules.ForEach(m =>
        {
            m.ModuleName.Should().NotBeEmpty();
            m.OverallQualityScore.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task CalculateRiskMetricsAsync_ShouldCalculateRiskScores()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 35,
            Confidence = 0.85,
            CriticalRisks = new() { new CriticalRiskItem() }
        };

        // Act
        var metrics = await _service.CalculateRiskMetricsAsync("session-1", assessment);

        // Assert
        metrics.Should().NotBeNull();
        metrics.OverallRiskScore.Should().Be(35);
        metrics.AssessmentConfidenceLevel.Should().Be(0.85);
    }

    [Fact]
    public async Task CalculateReadinessAsync_ShouldReturnReadinessMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var readiness = await _service.CalculateReadinessAsync("session-1");

        // Assert
        readiness.Should().NotBeNull();
        readiness.OverallReadiness.Should().BeGreaterThanOrEqualTo(0);
        readiness.RecommendedDecision.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateReadinessAsync_ShouldMakeGoDecisionForLowRisk()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        var assessment = new MigrationRiskAssessment { OverallRiskScore = 20 };
        await _service.CalculateRiskMetricsAsync("session-1", assessment);

        // Act
        var readiness = await _service.CalculateReadinessAsync("session-1");

        // Assert
        readiness.RecommendedDecision.Should().Be("Go");
    }

    [Fact]
    public async Task CalculateComparativeMetricsAsync_ShouldReturnComparisons()
    {
        // Act
        var comparatives = await _service.CalculateComparativeMetricsAsync(
            "session-1",
            new List<string> { "DataMatch", "SchemaMatch" });

        // Assert
        comparatives.Should().NotBeNull();
        comparatives.Should().HaveCount(2);
    }

    [Fact]
    public async Task CompareMetricAsync_ShouldCalculateDifference()
    {
        // Act
        var result = await _service.CompareMetricAsync("session-1", "Performance", 100, 102);

        // Assert
        result.Should().NotBeNull();
        result.LegacyValue.Should().Be(100);
        result.BlazorValue.Should().Be(102);
        result.DifferenceAbsolute.Should().Be(2);
    }

    [Fact]
    public async Task RecordSnapshotAsync_ShouldRecordSnapshot()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var snapshot = await _service.RecordSnapshotAsync("session-1", "Discovery");

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.SessionId.Should().Be("session-1");
        snapshot.PhaseName.Should().Be("Discovery");
    }

    [Fact]
    public async Task GetSnapshotsAsync_ShouldReturnSnapshots()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        await _service.RecordSnapshotAsync("session-1", "Discovery");
        await _service.RecordSnapshotAsync("session-1", "TestGen");

        // Act
        var snapshots = await _service.GetSnapshotsAsync("session-1");

        // Assert
        snapshots.Should().NotBeNull();
        snapshots.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSnapshotsAsync_ShouldRespectMaxCount()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        for (int i = 0; i < 5; i++)
        {
            await _service.RecordSnapshotAsync("session-1", "Test");
        }

        // Act
        var snapshots = await _service.GetSnapshotsAsync("session-1", 2);

        // Assert
        snapshots.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task AnalyzeTrendAsync_ShouldAnalyzeTrend()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        await _service.RecordSnapshotAsync("session-1", "Discovery");
        await Task.Delay(100);
        await _service.RecordSnapshotAsync("session-1", "TestGen");

        // Act
        var trend = await _service.AnalyzeTrendAsync("session-1", "Quality");

        // Assert
        trend.Should().NotBeNull();
        trend.MetricName.Should().Be("Quality");
    }

    [Fact]
    public async Task CalculateHealthAsync_ShouldCalculateOverallHealth()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var health = await _service.CalculateHealthAsync("session-1");

        // Assert
        health.Should().NotBeNull();
        health.HealthScore.Should().BeGreaterThanOrEqualTo(0);
        health.HealthScore.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CalculateHealthAsync_ShouldIndicateHealthStatus()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var health = await _service.CalculateHealthAsync("session-1");

        // Assert
        health.HealthStatus.Should().BeOneOf("Healthy", "Warning", "Critical");
    }

    [Fact]
    public async Task GetHealthIssuesAsync_ShouldReturnIssues()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var issues = await _service.GetHealthIssuesAsync("session-1");

        // Assert
        issues.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateModuleMetricsAsync_ShouldCalculateForModule()
    {
        // Act
        var metrics = await _service.CalculateModuleMetricsAsync("session-1", "Accounts");

        // Assert
        metrics.Should().NotBeNull();
        metrics.ModuleName.Should().Be("Accounts");
        metrics.OverallQualityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllModuleMetricsAsync_ShouldReturnAllModules()
    {
        // Act
        var modules = await _service.GetAllModuleMetricsAsync("session-1");

        // Assert
        modules.Should().NotBeNull();
        modules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task RankModulesByRiskAsync_ShouldRankModules()
    {
        // Arrange
        var modules = await _service.GetAllModuleMetricsAsync("session-1");

        // Act
        var ranked = await _service.RankModulesByRiskAsync("session-1");

        // Assert
        ranked.Should().NotBeNull();
        ranked.Should().HaveCount(modules.Count);
    }

    [Fact]
    public async Task AggregateAllMetricsAsync_ShouldAggregateMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var metrics = await _service.AggregateAllMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
        metrics.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task RecalculateAllMetricsAsync_ShouldRecalculate()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var metrics = await _service.RecalculateAllMetricsAsync("session-1");

        // Assert
        metrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPhaseMetricsAsync_ShouldReturnPhaseMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        await _service.RecordPhasePerformanceAsync("session-1", "Discovery", 5000, 100);

        // Act
        var metrics = await _service.GetPhaseMetricsAsync("session-1", "Discovery");

        // Assert
        metrics.Should().NotBeNull();
        metrics.PhaseName.Should().Be("Discovery");
    }

    [Fact]
    public async Task AnalyzeScenarioAsync_ShouldAnalyzeScenario()
    {
        // Act
        var analysis = await _service.AnalyzeScenarioAsync(
            "session-1",
            "extended-timeline",
            new Dictionary<string, object> { { "days", 7 } });

        // Assert
        analysis.Should().NotBeNull();
        analysis.Scenario.Should().Be("extended-timeline");
    }

    [Fact]
    public async Task GetRecommendationsAsync_ShouldReturnRecommendations()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var recommendations = await _service.GetRecommendationsAsync("session-1");

        // Assert
        recommendations.Should().NotBeNull();
    }

    [Fact]
    public async Task IdentifyImprovementAreasAsync_ShouldIdentifyAreas()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var areas = await _service.IdentifyImprovementAreasAsync("session-1");

        // Assert
        areas.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportMetricsAsync_ShouldExportJson()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var export = await _service.ExportMetricsAsync("session-1", "json");

        // Assert
        export.Should().NotBeNullOrEmpty();
        export.Should().Contain("session-1");
    }

    [Fact]
    public async Task ExportMetricsAsync_ShouldExportCsv()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var export = await _service.ExportMetricsAsync("session-1", "csv");

        // Assert
        export.Should().NotBeNullOrEmpty();
        export.Should().Contain("SessionId");
    }

    [Fact]
    public async Task ExportMetricsAsync_ShouldExportMarkdown()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var export = await _service.ExportMetricsAsync("session-1", "markdown");

        // Assert
        export.Should().NotBeNullOrEmpty();
        export.Should().Contain("Metrics Report");
    }

    [Fact]
    public async Task CompareSessionsAsync_ShouldCompare()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");
        await _service.InitializeMetricsAsync("session-2");

        // Act
        var comparison = await _service.CompareSessionsAsync("session-1", "session-2");

        // Assert
        comparison.Should().NotBeNull();
        comparison.Session1Id.Should().Be("session-1");
        comparison.Session2Id.Should().Be("session-2");
    }

    [Fact]
    public async Task ArchiveMetricsAsync_ShouldArchive()
    {
        // Act
        await _service.ArchiveMetricsAsync("session-1", 30);

        // Assert
        // Should complete without error
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new MetricsCalculationConfig { UpdateIntervalMs = 2000 };

        // Act
        _service.Configure(config);
        var retrieved = _service.GetConfiguration();

        // Assert
        retrieved.UpdateIntervalMs.Should().Be(2000);
    }

    [Fact]
    public void GetConfiguration_ShouldReturnCurrentConfiguration()
    {
        // Act
        var config = _service.GetConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.EnableAutoCalculation.Should().BeTrue();
    }

    [Fact]
    public void SubscribeToMetricsUpdates_ShouldAddSubscriber()
    {
        // Arrange
        var called = false;
        async Task Callback(ValidationMetrics m)
        {
            called = true;
        }

        // Act
        _service.SubscribeToMetricsUpdates("session-1", Callback);

        // Assert
        // Should complete without error
    }

    [Fact]
    public void UnsubscribeFromMetricsUpdates_ShouldRemoveSubscriber()
    {
        // Act
        _service.UnsubscribeFromMetricsUpdates("session-1");

        // Assert
        // Should complete without error
    }

    [Fact]
    public async Task BroadcastMetricsUpdateAsync_ShouldBroadcastToSubscribers()
    {
        // Arrange
        var received = false;
        async Task Callback(ValidationMetrics m)
        {
            received = true;
        }
        _service.SubscribeToMetricsUpdates("session-1", Callback);

        // Act
        var metrics = new ValidationMetrics { SessionId = "session-1" };
        await _service.BroadcastMetricsUpdateAsync(metrics);
        await Task.Delay(100);

        // Assert
        received.Should().BeTrue();
    }

    [Fact]
    public async Task GetMetricsStreamAsync_ShouldStreamMetrics()
    {
        // Arrange
        await _service.InitializeMetricsAsync("session-1");

        // Act
        var stream = _service.GetMetricsStreamAsync("session-1");
        var metrics = new List<ValidationMetrics>();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        try
        {
            await foreach (var m in stream.WithCancellation(cts.Token))
            {
                metrics.Add(m);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        metrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MetricsCalculation_IntegrationScenario()
    {
        // Arrange - Initialize
        await _service.InitializeMetricsAsync("session-1");

        // Act - Record data
        var comparison = new TableComparisonResult
        {
            SourceTable = "Users",
            TargetTable = "Users",
            SourceRowCount = 10000,
            TargetRowCount = 9950,
            MatchedRows = Enumerable.Range(0, 9900).Select(_ => new RowComparison()).ToList(),
            MismatchedRows = Enumerable.Range(0, 50).Select(_ => new RowComparison()).ToList()
        };

        var dataMetrics = await _service.CalculateDataMetricsAsync("session-1", comparison);
        var qualityMetrics = await _service.CalculateQualityMetricsAsync("session-1");

        var assessment = new MigrationRiskAssessment { OverallRiskScore = 25 };
        var riskMetrics = await _service.CalculateRiskMetricsAsync("session-1", assessment);

        var readiness = await _service.CalculateReadinessAsync("session-1");
        var health = await _service.CalculateHealthAsync("session-1");

        // Assert
        dataMetrics.DataMatchPercentage.Should().BeLessThan(100);
        qualityMetrics.OverallAccuracy.Should().BeGreaterThan(80);
        readiness.RecommendedDecision.Should().Be("Go");
        health.HealthStatus.Should().NotBeNullOrEmpty();
    }
}
