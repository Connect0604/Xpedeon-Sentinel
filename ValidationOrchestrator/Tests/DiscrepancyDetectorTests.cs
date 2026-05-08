namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for discrepancy detector (Form 6)
/// </summary>
public class DiscrepancyDetectorTests
{
    private readonly IDiscrepancyDetector _detector = new DiscrepancyDetector();

    [Fact]
    public void Constructor_ShouldInitializeDetector()
    {
        // Act
        var detector = new DiscrepancyDetector();

        // Assert
        detector.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectFromTableComparisonAsync_WithIdenticalData_ShouldReturnNoDiscrepancies()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            TableName = "Invoices",
            LegacyRecordCount = 100,
            BlazonRecordCount = 100,
            MatchedRecords = 100,
            MissingRecords = 0,
            ExtraRecords = 0,
            ModifiedRecords = 0,
            MatchPercentage = 100,
            Status = ComparisonStatus.Identical,
            ColumnComparisons = new List<ColumnComparison>()
        };

        // Act
        var result = await _detector.DetectFromTableComparisonAsync(comparison, "session1", "client1");

        // Assert
        result.Discrepancies.Should().BeEmpty();
        result.Summary.TotalDiscrepancies.Should().Be(0);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DetectFromTableComparisonAsync_WithDataMismatch_ShouldDetectDiscrepancy()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            TableName = "Invoices",
            LegacyRecordCount = 100,
            BlazonRecordCount = 100,
            MatchedRecords = 95,
            MissingRecords = 0,
            ExtraRecords = 0,
            ModifiedRecords = 5,
            MatchPercentage = 95,
            Status = ComparisonStatus.Compatible,
            ColumnComparisons = new List<ColumnComparison>()
        };

        // Act
        var result = await _detector.DetectFromTableComparisonAsync(comparison, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].Category.Should().Be("DataValue");
        result.Summary.TotalDiscrepancies.Should().Be(1);
    }

    [Fact]
    public async Task DetectFromTableComparisonAsync_WithMissingRows_ShouldDetectMissingRecords()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            TableName = "Invoices",
            LegacyRecordCount = 100,
            BlazonRecordCount = 95,
            MatchedRecords = 95,
            MissingRecords = 5,
            ExtraRecords = 0,
            ModifiedRecords = 0,
            MatchPercentage = 100,
            Status = ComparisonStatus.Identical,
            ColumnComparisons = new List<ColumnComparison>()
        };

        // Act
        var result = await _detector.DetectFromTableComparisonAsync(comparison, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].Category.Should().Be("Completeness");
        result.Discrepancies[0].AffectedRecordCount.Should().Be(5);
    }

    [Fact]
    public async Task DetectFromTableComparisonAsync_WithExtraRows_ShouldDetectExtraRecords()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            TableName = "Invoices",
            LegacyRecordCount = 100,
            BlazonRecordCount = 105,
            MatchedRecords = 100,
            MissingRecords = 0,
            ExtraRecords = 5,
            ModifiedRecords = 0,
            MatchPercentage = 100,
            Status = ComparisonStatus.Identical,
            ColumnComparisons = new List<ColumnComparison>()
        };

        // Act
        var result = await _detector.DetectFromTableComparisonAsync(comparison, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].Category.Should().Be("Consistency");
        result.Discrepancies[0].AffectedRecordCount.Should().Be(5);
    }

    [Fact]
    public async Task DetectFromTableComparisonAsync_WithColumnMismatch_ShouldDetectColumnDiscrepancy()
    {
        // Arrange
        var comparison = new TableComparisonResult
        {
            TableName = "Invoices",
            LegacyRecordCount = 100,
            BlazonRecordCount = 100,
            MatchedRecords = 100,
            MissingRecords = 0,
            ExtraRecords = 0,
            ModifiedRecords = 0,
            MatchPercentage = 100,
            Status = ComparisonStatus.Identical,
            ColumnComparisons = new List<ColumnComparison>
            {
                new ColumnComparison
                {
                    ColumnName = "Amount",
                    TotalValues = 100,
                    MatchedValues = 90,
                    DifferentValues = 10,
                    MatchPercentage = 90,
                    DataType = "decimal",
                    SampleDifferences = new List<ColumnValueDifference>
                    {
                        new ColumnValueDifference
                        {
                            ColumnName = "Amount",
                            LegacyValue = "100.00",
                            BlazonValue = "99.50"
                        }
                    }
                }
            }
        };

        // Act
        var result = await _detector.DetectFromTableComparisonAsync(comparison, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].Column.Should().Be("Amount");
    }

    [Fact]
    public async Task DetectFromSchemaAnalysisAsync_WithDifferences_ShouldDetectDiscrepancies()
    {
        // Arrange
        var schemaAnalysis = new SchemaAnalysisResult
        {
            Success = true,
            Compatibility = SchemaAnalysisResult.CompatibilityLevel.Compatible,
            Differences = new List<SchemaDifference>
            {
                new SchemaDifference
                {
                    DifferenceType = "ColumnMissing",
                    Table = "Users",
                    Column = "CustomField",
                    LegacyValue = "Present",
                    BlazonValue = "Missing",
                    Severity = "High"
                }
            }
        };

        // Act
        var result = await _detector.DetectFromSchemaAnalysisAsync(schemaAnalysis, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].Category.Should().Be("SchemaStructure");
        result.Discrepancies[0].Severity.Should().Be(DiscrepancySeverity.High);
    }

    [Fact]
    public async Task DetectFromSchemaAnalysisAsync_WithIncompatibleSchema_ShouldMarkAsBlocker()
    {
        // Arrange
        var schemaAnalysis = new SchemaAnalysisResult
        {
            Success = true,
            Compatibility = SchemaAnalysisResult.CompatibilityLevel.Incompatible,
            Differences = new List<SchemaDifference>()
        };

        // Act
        var result = await _detector.DetectFromSchemaAnalysisAsync(schemaAnalysis, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1);
        result.Discrepancies[0].IsBlocker.Should().BeTrue();
        result.Discrepancies[0].Severity.Should().Be(DiscrepancySeverity.Critical);
    }

    [Fact]
    public async Task AnalyzeAllComparisonsAsync_ShouldCombineSchemaAndTableResults()
    {
        // Arrange
        var schemaAnalysis = new SchemaAnalysisResult
        {
            Success = true,
            Compatibility = SchemaAnalysisResult.CompatibilityLevel.Compatible,
            Differences = new List<SchemaDifference>()
        };

        var comparisons = new List<TableComparisonResult>
        {
            new TableComparisonResult
            {
                TableName = "Users",
                LegacyRecordCount = 100,
                BlazonRecordCount = 100,
                MatchedRecords = 95,
                MatchPercentage = 95,
                Status = ComparisonStatus.Compatible,
                ColumnComparisons = new List<ColumnComparison>()
            },
            new TableComparisonResult
            {
                TableName = "Projects",
                LegacyRecordCount = 50,
                BlazonRecordCount = 50,
                MatchedRecords = 50,
                MatchPercentage = 100,
                Status = ComparisonStatus.Identical,
                ColumnComparisons = new List<ColumnComparison>()
            }
        };

        // Act
        var result = await _detector.AnalyzeAllComparisonsAsync(comparisons, schemaAnalysis, "session1", "client1");

        // Assert
        result.Discrepancies.Should().HaveCount(1); // Only Users has a discrepancy
        result.ExecutionTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateDataDiscrepancy_ShouldCreateDetailedDiscrepancy()
    {
        // Act
        var discrepancy = _detector.CreateDataDiscrepancy(
            module: "DataComparison",
            table: "Invoices",
            category: "DataValue",
            description: "Amount mismatch",
            severity: DiscrepancySeverity.High,
            affectedRecordCount: 10,
            impactPercentage: 10.5);

        // Assert
        discrepancy.Module.Should().Be("DataComparison");
        discrepancy.Table.Should().Be("Invoices");
        discrepancy.Category.Should().Be("DataValue");
        discrepancy.Severity.Should().Be(DiscrepancySeverity.High);
        discrepancy.AffectedRecordCount.Should().Be(10);
    }

    [Fact]
    public void CreateSchemaDiscrepancy_ShouldCreateSchemaDiscrepancy()
    {
        // Act
        var discrepancy = _detector.CreateSchemaDiscrepancy(
            module: "SchemaAnalysis",
            table: "Users",
            column: "CustomField",
            description: "Column missing in Blazor",
            severity: DiscrepancySeverity.Critical,
            isBlocker: true);

        // Assert
        discrepancy.Category.Should().Be("SchemaStructure");
        discrepancy.IsBlocker.Should().BeTrue();
        discrepancy.Column.Should().Be("CustomField");
    }

    [Fact]
    public void DetermineSeverity_WithHighImpact_ShouldReturnCritical()
    {
        // Act
        var severity = _detector.DetermineSeverity(impactPercentage: 25, affectedRecordCount: 100);

        // Assert
        severity.Should().Be(DiscrepancySeverity.Critical);
    }

    [Fact]
    public void DetermineSeverity_WithMediumImpact_ShouldReturnMedium()
    {
        // Act
        var severity = _detector.DetermineSeverity(impactPercentage: 7, affectedRecordCount: 50);

        // Assert
        severity.Should().Be(DiscrepancySeverity.Medium);
    }

    [Fact]
    public void DetermineSeverity_WithLowImpact_ShouldReturnLow()
    {
        // Act
        var severity = _detector.DetermineSeverity(impactPercentage: 2, affectedRecordCount: 5);

        // Assert
        severity.Should().Be(DiscrepancySeverity.Low);
    }

    [Fact]
    public void CalculateSeverityScore_ShouldCalculateNumericScore()
    {
        // Arrange
        var discrepancy = new DetailedDiscrepancy
        {
            Severity = DiscrepancySeverity.Critical,
            ImpactPercentage = 15,
            IsBlocker = true
        };

        // Act
        var score = _detector.CalculateSeverityScore(discrepancy);

        // Assert
        score.Should().BeGreaterThan(0);
        score.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void GroupDiscrepancies_ShouldGroupByModuleAndTable()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy
            {
                Module = "DataComparison",
                Table = "Users",
                Category = "DataValue",
                Severity = DiscrepancySeverity.High
            },
            new DetailedDiscrepancy
            {
                Module = "DataComparison",
                Table = "Users",
                Category = "DataValue",
                Severity = DiscrepancySeverity.Medium
            },
            new DetailedDiscrepancy
            {
                Module = "DataComparison",
                Table = "Projects",
                Category = "SchemaStructure",
                Severity = DiscrepancySeverity.High
            }
        };

        // Act
        var groups = _detector.GroupDiscrepancies(discrepancies);

        // Assert
        groups.Should().NotBeEmpty();
        groups.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void FindRelatedDiscrepancies_ShouldFindSimilarIssues()
    {
        // Arrange
        var targetDiscrepancy = new DetailedDiscrepancy
        {
            Id = "1",
            Module = "DataComparison",
            Table = "Users",
            Category = "DataValue",
            Severity = DiscrepancySeverity.High
        };

        var allDiscrepancies = new List<DetailedDiscrepancy>
        {
            targetDiscrepancy,
            new DetailedDiscrepancy
            {
                Id = "2",
                Module = "DataComparison",
                Table = "Users",
                Category = "DataValue",
                Severity = DiscrepancySeverity.High
            },
            new DetailedDiscrepancy
            {
                Id = "3",
                Module = "DataComparison",
                Table = "Projects",
                Category = "DataValue",
                Severity = DiscrepancySeverity.High
            }
        };

        // Act
        var related = _detector.FindRelatedDiscrepancies(targetDiscrepancy, allDiscrepancies);

        // Assert
        related.Should().HaveCount(1);
        related[0].Id.Should().Be("2");
    }

    [Fact]
    public void AssessModuleImpacts_ShouldCalculateImpactPerModule()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy
            {
                Module = "SchemaAnalysis",
                Table = "Users",
                Severity = DiscrepancySeverity.Critical,
                ImpactPercentage = 15,
                IsBlocker = true,
                IsResolvable = false
            },
            new DetailedDiscrepancy
            {
                Module = "DataComparison",
                Table = "Invoices",
                Severity = DiscrepancySeverity.Medium,
                ImpactPercentage = 5,
                IsBlocker = false,
                IsResolvable = true
            }
        };

        // Act
        var impacts = _detector.AssessModuleImpacts(discrepancies);

        // Assert
        impacts.Should().HaveCount(2);
        impacts["SchemaAnalysis"].IsBlocking.Should().BeTrue();
        impacts["DataComparison"].IsBlocking.Should().BeFalse();
    }

    [Fact]
    public void CalculateOverallImpact_ShouldAggregateImpactScores()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Critical, ImpactPercentage = 20 },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.High, ImpactPercentage = 15 },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Medium, ImpactPercentage = 5 }
        };

        // Act
        var overallImpact = _detector.CalculateOverallImpact(discrepancies);

        // Assert
        overallImpact.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FindBlockingIssues_ShouldReturnOnlyBlockers()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { IsBlocker = true, Severity = DiscrepancySeverity.Critical },
            new DetailedDiscrepancy { IsBlocker = false, Severity = DiscrepancySeverity.High },
            new DetailedDiscrepancy { IsBlocker = true, Severity = DiscrepancySeverity.Critical }
        };

        // Act
        var blockers = _detector.FindBlockingIssues(discrepancies);

        // Assert
        blockers.Should().HaveCount(2);
        blockers.Should().AllSatisfy(b => b.IsBlocker.Should().BeTrue());
    }

    [Fact]
    public void GenerateSummary_ShouldCalculateStatistics()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy
            {
                Module = "Module1",
                Category = "DataValue",
                Severity = DiscrepancySeverity.Critical,
                ImpactPercentage = 20,
                IsBlocker = true,
                IsResolvable = false
            },
            new DetailedDiscrepancy
            {
                Module = "Module1",
                Category = "SchemaStructure",
                Severity = DiscrepancySeverity.High,
                ImpactPercentage = 10,
                IsBlocker = false,
                IsResolvable = true
            }
        };

        // Act
        var summary = _detector.GenerateSummary(discrepancies);

        // Assert
        summary.TotalDiscrepancies.Should().Be(2);
        summary.CriticalCount.Should().Be(1);
        summary.HighCount.Should().Be(1);
        summary.BlockerCount.Should().Be(1);
        summary.ResolvableCount.Should().Be(1);
        summary.DiscrepanciesByModule.Should().ContainKey("Module1");
    }

    [Fact]
    public void GenerateMetrics_ShouldProduceMetricsFromSummary()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Critical },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.High }
        };
        var summary = _detector.GenerateSummary(discrepancies);

        // Act
        var metrics = _detector.GenerateMetrics(discrepancies, summary);

        // Assert
        metrics.TotalDiscrepancies.Should().Be(2);
        metrics.SeverityDistribution_Critical.Should().Be(1);
        metrics.SeverityDistribution_High.Should().Be(1);
    }

    [Fact]
    public void IdentifyRootCause_ShouldReturnCauseBasedOnCategory()
    {
        // Arrange
        var discrepancy = new DetailedDiscrepancy
        {
            Category = "DataType",
            Severity = DiscrepancySeverity.High
        };

        // Act
        var cause = _detector.IdentifyRootCause(discrepancy);

        // Assert
        cause.Should().NotBeNullOrEmpty();
        cause.Should().Contain("Type");
    }

    [Fact]
    public void GenerateRecommendations_ShouldCreateRecommendations()
    {
        // Arrange
        var discrepancy = new DetailedDiscrepancy
        {
            Category = "DataValue",
            Severity = DiscrepancySeverity.High,
            AffectedRecordCount = 50
        };

        // Act
        var recommendations = _detector.GenerateRecommendations(discrepancy);

        // Assert
        recommendations.Should().NotBeEmpty();
        recommendations.Should().AllSatisfy(r => r.Title.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void AnalyzePatterns_ShouldIdentifyCommonPatterns()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Table = "Users", Category = "DataValue" },
            new DetailedDiscrepancy { Table = "Users", Category = "DataValue" },
            new DetailedDiscrepancy { Table = "Projects", Category = "DataValue" }
        };

        // Act
        var patterns = _detector.AnalyzePatterns(discrepancies);

        // Assert
        patterns.Should().NotBeEmpty();
    }

    [Fact]
    public void CollectEvidence_ShouldCompileEvidence()
    {
        // Arrange
        var discrepancy = new DetailedDiscrepancy
        {
            AffectedRecordCount = 100,
            ImpactPercentage = 15,
            Evidence = new List<string> { "Initial evidence" }
        };

        // Act
        var evidence = _detector.CollectEvidence(discrepancy);

        // Assert
        evidence.Should().NotBeEmpty();
        evidence.Should().Contain("Initial evidence");
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new DiscrepancyDetectionConfig
        {
            MinimumImpactPercentage = 0.5,
            EnableGrouping = true
        };

        // Act
        _detector.Configure(config);

        // Assert - No exception should occur
    }

    [Fact]
    public void EnablePatternMatching_ShouldActivatePatterns()
    {
        // Arrange
        var patterns = new List<DiscrepancyPattern>
        {
            new DiscrepancyPattern
            {
                Name = "Test Pattern",
                Category = DiscrepancyCategory.DataValue
            }
        };

        // Act
        _detector.EnablePatternMatching(patterns);

        // Assert - No exception should occur
    }

    [Fact]
    public void AddCustomPattern_ShouldAddPatternToDetector()
    {
        // Arrange
        var pattern = new DiscrepancyPattern
        {
            Name = "Custom Test Pattern",
            Description = "For testing",
            Category = DiscrepancyCategory.DataValue
        };

        // Act
        _detector.AddCustomPattern(pattern);

        // Assert - No exception should occur
    }
}
