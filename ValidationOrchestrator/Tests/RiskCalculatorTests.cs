namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for risk calculator (Form 7)
/// </summary>
public class RiskCalculatorTests
{
    private readonly IRiskCalculator _calculator = new RiskCalculator();

    private DiscrepancyAnalysisResult CreateTestAnalysisResult(
        int totalDiscrepancies = 5,
        int criticalCount = 1,
        int highCount = 2,
        int blockingCount = 1,
        double avgImpactPercentage = 10)
    {
        var discrepancies = new List<DetailedDiscrepancy>();

        for (int i = 0; i < criticalCount; i++)
        {
            discrepancies.Add(new DetailedDiscrepancy
            {
                Module = "TestModule",
                Category = "DataValue",
                Severity = DiscrepancySeverity.Critical,
                ImpactPercentage = avgImpactPercentage,
                IsBlocker = true,
                AffectedRecordCount = 100
            });
        }

        for (int i = 0; i < highCount; i++)
        {
            discrepancies.Add(new DetailedDiscrepancy
            {
                Module = "TestModule",
                Category = "SchemaStructure",
                Severity = DiscrepancySeverity.High,
                ImpactPercentage = avgImpactPercentage,
                IsBlocker = false,
                AffectedRecordCount = 50
            });
        }

        for (int i = 0; i < totalDiscrepancies - criticalCount - highCount; i++)
        {
            discrepancies.Add(new DetailedDiscrepancy
            {
                Module = "TestModule",
                Category = "DataType",
                Severity = DiscrepancySeverity.Medium,
                ImpactPercentage = avgImpactPercentage / 2,
                IsBlocker = false,
                AffectedRecordCount = 10
            });
        }

        return new DiscrepancyAnalysisResult
        {
            Discrepancies = discrepancies,
            Summary = new DiscrepancySummary
            {
                TotalDiscrepancies = totalDiscrepancies,
                CriticalCount = criticalCount,
                HighCount = highCount,
                BlockerCount = blockingCount
            },
            ModuleImpacts = new Dictionary<string, ImpactAssessment>
            {
                { "TestModule", new ImpactAssessment { ModuleName = "TestModule", ImpactPercentage = avgImpactPercentage } }
            }
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeCalculator()
    {
        // Act
        var calculator = new RiskCalculator();

        // Assert
        calculator.Should().NotBeNull();
    }

    [Fact]
    public async Task AssessMigrationRiskAsync_WithNoDiscrepancies_ShouldReturnLowRisk()
    {
        // Arrange
        var analysis = new DiscrepancyAnalysisResult
        {
            Discrepancies = new(),
            Summary = new DiscrepancySummary { TotalDiscrepancies = 0 },
            ModuleImpacts = new()
        };

        // Act
        var result = await _calculator.AssessMigrationRiskAsync(analysis, "session1", "client1");

        // Assert
        result.OverallRiskScore.Should().Be(0);
        result.OverallReadiness.Should().Be(MigrationReadiness.Ready);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AssessMigrationRiskAsync_WithCriticalDiscrepancies_ShouldReturnHighRisk()
    {
        // Arrange
        var analysis = CreateTestAnalysisResult(totalDiscrepancies: 10, criticalCount: 3, highCount: 5, blockingCount: 2, avgImpactPercentage: 25);

        // Act
        var result = await _calculator.AssessMigrationRiskAsync(analysis, "session1", "client1");

        // Assert
        result.OverallRiskScore.Should().BeGreaterThan(50);
        result.OverallReadiness.Should().Be(MigrationReadiness.NotReady);
        result.CriticalItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssessMigrationRiskAsync_ShouldCalculateModuleRisks()
    {
        // Arrange
        var analysis = CreateTestAnalysisResult(totalDiscrepancies: 5);

        // Act
        var result = await _calculator.AssessMigrationRiskAsync(analysis, "session1", "client1");

        // Assert
        result.ModuleRisks.Should().NotBeEmpty();
        result.ModuleRisks[0].ModuleName.Should().Be("TestModule");
        result.ModuleRisks[0].RiskScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AssessModuleRiskAsync_ShouldReturnModuleRisk()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy
            {
                Module = "Users",
                Category = "DataValue",
                Severity = DiscrepancySeverity.High,
                ImpactPercentage = 15,
                AffectedRecordCount = 100
            }
        };

        // Act
        var result = await _calculator.AssessModuleRiskAsync("Users", discrepancies);

        // Assert
        result.ModuleName.Should().Be("Users");
        result.RiskScore.Should().BeGreaterThan(0);
        result.Readiness.Should().NotBeNull();
    }

    [Fact]
    public void CalculateOverallRiskScore_WithNoDiscrepancies_ShouldReturnZero()
    {
        // Act
        var score = _calculator.CalculateOverallRiskScore(
            new List<DetailedDiscrepancy>(),
            new DiscrepancySummary());

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateOverallRiskScore_WithDiscrepancies_ShouldReturnScore()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Critical, ImpactPercentage = 20 },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.High, ImpactPercentage = 10 }
        };
        var summary = new DiscrepancySummary
        {
            TotalDiscrepancies = 2,
            CriticalCount = 1,
            HighCount = 1,
            BlockerCount = 1
        };

        // Act
        var score = _calculator.CalculateOverallRiskScore(discrepancies, summary);

        // Assert
        score.Should().BeGreaterThan(0);
        score.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void CalculateHealthScore_ShouldBeComplementToRiskScore()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.High, ImpactPercentage = 10 }
        };
        var summary = new DiscrepancySummary { TotalDiscrepancies = 1, HighCount = 1 };

        // Act
        var riskScore = _calculator.CalculateOverallRiskScore(discrepancies, summary);
        var healthScore = _calculator.CalculateHealthScore(discrepancies, summary);

        // Assert
        (riskScore + healthScore).Should().BeApproximately(100, 0.1);
    }

    [Fact]
    public void CalculateModuleRiskScore_WithEmptyList_ShouldReturnZero()
    {
        // Act
        var score = _calculator.CalculateModuleRiskScore(new List<DetailedDiscrepancy>());

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateModuleRiskScore_WithDiscrepancies_ShouldReturnScore()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy
            {
                Severity = DiscrepancySeverity.Critical,
                ImpactPercentage = 25,
                IsBlocker = true
            }
        };

        // Act
        var score = _calculator.CalculateModuleRiskScore(discrepancies);

        // Assert
        score.Should().BeGreaterThan(0);
        score.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void CalculateRiskComponents_ShouldIdentifyComponentRisks()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Category = "DataValue", Severity = DiscrepancySeverity.High },
            new DetailedDiscrepancy { Category = "SchemaStructure", Severity = DiscrepancySeverity.High },
            new DetailedDiscrepancy { Category = "BusinessLogic", Severity = DiscrepancySeverity.Medium }
        };

        // Act
        var components = _calculator.CalculateRiskComponents(discrepancies);

        // Assert
        components.Should().NotBeEmpty();
        components.Should().Contain(c => c.Category == "Data");
        components.Should().Contain(c => c.Category == "Schema");
    }

    [Fact]
    public void MakeRecommendation_WithHighRisk_ShouldRecommendNoGo()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 80,
            CriticalItems = new List<CriticalRiskItem>
            {
                new CriticalRiskItem { Title = "Critical Issue", Priority = RiskPriority.Critical }
            },
            GoBlockers = new List<string> { "Blocker 1" }
        };

        // Act
        var recommendation = _calculator.MakeRecommendation(assessment);

        // Assert
        recommendation.Decision.Should().Be(MigrationDecision.NoGo);
        recommendation.Rationale.Should().Contain("Critical");
    }

    [Fact]
    public void MakeRecommendation_WithLowRisk_ShouldRecommendGo()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 10,
            CriticalItems = new(),
            GoBlockers = new(),
            ModuleRisks = new()
        };

        // Act
        var recommendation = _calculator.MakeRecommendation(assessment);

        // Assert
        recommendation.Decision.Should().Be(MigrationDecision.Go);
    }

    [Fact]
    public void DetermineMigrationReadiness_WithLowRisk_ShouldReturnReady()
    {
        // Act
        var readiness = _calculator.DetermineMigrationReadiness(20);

        // Assert
        readiness.Should().Be(MigrationReadiness.Ready);
    }

    [Fact]
    public void DetermineMigrationReadiness_WithHighRisk_ShouldReturnNotReady()
    {
        // Act
        var readiness = _calculator.DetermineMigrationReadiness(80);

        // Assert
        readiness.Should().Be(MigrationReadiness.NotReady);
    }

    [Fact]
    public void IdentifyGoBlockers_ShouldReturnCriticalUnresolvableIssues()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            CriticalItems = new List<CriticalRiskItem>
            {
                new CriticalRiskItem { Title = "Issue 1", Priority = RiskPriority.Critical, CanBeFixedPostLaunch = false },
                new CriticalRiskItem { Title = "Issue 2", Priority = RiskPriority.High, CanBeFixedPostLaunch = true }
            },
            ModuleRisks = new()
        };

        // Act
        var blockers = _calculator.IdentifyGoBlockers(assessment);

        // Assert
        blockers.Should().NotBeEmpty();
        blockers.Should().Contain(b => b.Contains("Issue 1"));
    }

    [Fact]
    public void IdentifyWarnings_ShouldReturnNonBlockingConcerns()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            CriticalItems = new List<CriticalRiskItem>
            {
                new CriticalRiskItem { Title = "Warning Issue", Priority = RiskPriority.High }
            },
            ModuleRisks = new List<ModuleRiskAssessment>
            {
                new ModuleRiskAssessment { ModuleName = "Module1", RiskScore = 60 }
            }
        };

        // Act
        var warnings = _calculator.IdentifyWarnings(assessment);

        // Assert
        warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void ExtractCriticalItems_ShouldExtractHighSeverityDiscrepancies()
    {
        // Arrange
        var discrepancies = new List<DetailedDiscrepancy>
        {
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Critical, Description = "Critical Issue" },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.High, Description = "High Issue" },
            new DetailedDiscrepancy { Severity = DiscrepancySeverity.Low, Description = "Low Issue" }
        };

        // Act
        var items = _calculator.ExtractCriticalItems(discrepancies);

        // Assert
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(i => i.Priority.Should().BeGreaterThanOrEqualTo(RiskPriority.High));
    }

    [Fact]
    public void PrioritizeCriticalItems_ShouldSortByPriority()
    {
        // Arrange
        var items = new List<CriticalRiskItem>
        {
            new CriticalRiskItem { Title = "Low", Priority = RiskPriority.Low, ImpactPercentage = 5 },
            new CriticalRiskItem { Title = "Critical", Priority = RiskPriority.Critical, ImpactPercentage = 20 },
            new CriticalRiskItem { Title = "High", Priority = RiskPriority.High, ImpactPercentage = 15 }
        };

        // Act
        var prioritized = _calculator.PrioritizeCriticalItems(items);

        // Assert
        prioritized[0].Title.Should().Be("Critical");
        prioritized[1].Title.Should().Be("High");
        prioritized[2].Title.Should().Be("Low");
    }

    [Fact]
    public void AnalyzeDependencies_ShouldLinkRelatedItems()
    {
        // Arrange
        var items = new List<CriticalRiskItem>
        {
            new CriticalRiskItem { Id = "1", Table = "Users" },
            new CriticalRiskItem { Id = "2", Table = "Users" },
            new CriticalRiskItem { Id = "3", Table = "Projects" }
        };

        // Act
        _calculator.AnalyzeDependencies(items);

        // Assert
        items[0].DependentOn.Should().Contain("2");
        items[1].DependentOn.Should().Contain("1");
        items[2].DependentOn.Should().BeEmpty();
    }

    [Fact]
    public void GenerateMitigationStrategies_ShouldCreateStrategies()
    {
        // Arrange
        var items = new List<CriticalRiskItem>
        {
            new CriticalRiskItem
            {
                Title = "Critical Issue",
                Priority = RiskPriority.Critical,
                EstimatedFixHours = 16
            }
        };

        // Act
        var strategies = _calculator.GenerateMitigationStrategies(items, new());

        // Assert
        strategies.Should().NotBeEmpty();
        strategies[0].Actions.Should().NotBeEmpty();
    }

    [Fact]
    public void EstimateDaysToReadiness_WithNoCriticalItems_ShouldReturnZero()
    {
        // Act
        var days = _calculator.EstimateDaysToReadiness(new List<CriticalRiskItem>());

        // Assert
        days.Should().Be(0);
    }

    [Fact]
    public void EstimateDaysToReadiness_ShouldEstimateTime()
    {
        // Arrange
        var items = new List<CriticalRiskItem>
        {
            new CriticalRiskItem { EstimatedFixHours = 8, CanBeFixedPostLaunch = false },
            new CriticalRiskItem { EstimatedFixHours = 16, CanBeFixedPostLaunch = true }
        };

        // Act
        var days = _calculator.EstimateDaysToReadiness(items);

        // Assert
        days.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateReadinessChecklist_ShouldCreateChecklist()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            CriticalItems = new(),
            ModuleRisks = new List<ModuleRiskAssessment>
            {
                new ModuleRiskAssessment { DataCompleteness = 99, DataAccuracy = 98, FunctionalCoverage = 95, RiskScore = 20 }
            }
        };

        // Act
        var checklist = _calculator.CreateReadinessChecklist(assessment);

        // Assert
        checklist.TotalItems.Should().Be(10);
        checklist.CompletedItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AnalyzeTrends_WithInsufficientData_ShouldReturnEmpty()
    {
        // Arrange
        var assessments = new List<MigrationRiskAssessment>
        {
            new MigrationRiskAssessment { OverallRiskScore = 50 }
        };

        // Act
        var trends = _calculator.AnalyzeTrends(assessments);

        // Assert
        trends.Trend.Should().Contain("Insufficient");
    }

    [Fact]
    public void AnalyzeTrends_ShouldAnalyzeRiskTrend()
    {
        // Arrange
        var assessments = new List<MigrationRiskAssessment>
        {
            new MigrationRiskAssessment { AssessedAt = DateTime.UtcNow.AddDays(-7), OverallRiskScore = 80 },
            new MigrationRiskAssessment { AssessedAt = DateTime.UtcNow, OverallRiskScore = 40 }
        };

        // Act
        var trends = _calculator.AnalyzeTrends(assessments);

        // Assert
        trends.Snapshots.Should().HaveCount(2);
        trends.Trend.Should().Be("Improving");
    }

    [Fact]
    public void ProjectReadinessDate_ShouldProjectDate()
    {
        // Arrange
        var trends = new RiskTrendAnalysis
        {
            Snapshots = new List<RiskSnapshot>
            {
                new RiskSnapshot { Timestamp = DateTime.UtcNow.AddDays(-7), OverallRiskScore = 80 },
                new RiskSnapshot { Timestamp = DateTime.UtcNow, OverallRiskScore = 40 }
            },
            ImprovementRate = 5.7
        };

        // Act
        var projectedDate = _calculator.ProjectReadinessDate(trends);

        // Assert
        projectedDate.Should().NotBeNull();
        projectedDate.Should().BeGreaterThan(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateMetrics_ShouldProduceMetrics()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 45,
            OverallHealthScore = 55,
            CriticalItems = new List<CriticalRiskItem>
            {
                new CriticalRiskItem { Priority = RiskPriority.Critical, EstimatedFixHours = 16 }
            },
            ModuleRisks = new List<ModuleRiskAssessment>
            {
                new ModuleRiskAssessment { RiskScore = 50, DataCompleteness = 95, DataAccuracy = 94 }
            },
            GoBlockers = new()
        };

        // Act
        var metrics = _calculator.GenerateMetrics(assessment);

        // Assert
        metrics.OverallRiskScore.Should().Be(45);
        metrics.CriticalRiskItems.Should().Be(1);
        metrics.EstimatedTotalFixHours.Should().Be(16);
    }

    [Fact]
    public void CalculateConfidenceLevel_WithNoBlockers_ShouldBeHigh()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 20,
            GoBlockers = new(),
            ModuleRisks = new List<ModuleRiskAssessment>
            {
                new ModuleRiskAssessment { Readiness = MigrationReadiness.Ready }
            }
        };

        // Act
        var confidence = _calculator.CalculateConfidenceLevel(assessment);

        // Assert
        confidence.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public void CalculateConfidenceLevel_WithBlockers_ShouldBeLow()
    {
        // Arrange
        var assessment = new MigrationRiskAssessment
        {
            OverallRiskScore = 80,
            GoBlockers = new List<string> { "Blocker 1", "Blocker 2" },
            ModuleRisks = new()
        };

        // Act
        var confidence = _calculator.CalculateConfidenceLevel(assessment);

        // Assert
        confidence.Should().BeLessThan(0.6);
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new RiskCalculationConfig
        {
            CriticalThreshold = 20,
            EnableTrendAnalysis = false
        };

        // Act
        _calculator.Configure(config);
        var retrieved = _calculator.GetConfiguration();

        // Assert
        retrieved.CriticalThreshold.Should().Be(20);
        retrieved.EnableTrendAnalysis.Should().BeFalse();
    }

    [Fact]
    public void SetScoringWeights_ShouldUpdateWeights()
    {
        // Arrange
        var weights = new RiskScoringWeights
        {
            CriticalDiscrepancyWeight = 2.0,
            BlockingIssueWeight = 10.0
        };

        // Act
        _calculator.SetScoringWeights(weights);

        // Assert - No exception should occur
    }
}
