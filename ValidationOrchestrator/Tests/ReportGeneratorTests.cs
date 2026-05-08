namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for report generator (Form 8)
/// </summary>
public class ReportGeneratorTests
{
    private readonly IReportGenerator _generator = new ReportGenerator();

    private MigrationRiskAssessment CreateTestRiskAssessment()
    {
        return new MigrationRiskAssessment
        {
            SessionId = "session-1",
            ClientId = "client-1",
            OverallRiskScore = 45,
            OverallHealthScore = 55,
            OverallReadiness = MigrationReadiness.AlmostReady,
            Recommendation = new MigrationRecommendation
            {
                Decision = MigrationDecision.GoWithRisks,
                Summary = "System ready with known risks",
                Rationale = "Some issues require mitigation",
                ConfidenceLevel = 0.8,
                MinimumDaysToReady = 5
            },
            CriticalItems = new List<CriticalRiskItem>
            {
                new CriticalRiskItem
                {
                    Title = "Data Mismatch in Invoices",
                    Priority = RiskPriority.Critical,
                    ImpactPercentage = 15,
                    EstimatedFixHours = 16
                }
            },
            GoBlockers = new List<string> { "None" },
            ModuleRisks = new List<ModuleRiskAssessment>
            {
                new ModuleRiskAssessment { ModuleName = "Module1", Readiness = MigrationReadiness.Ready, RiskScore = 20 }
            }
        };
    }

    private DiscrepancyAnalysisResult CreateTestDiscrepancies()
    {
        return new DiscrepancyAnalysisResult
        {
            Discrepancies = new List<DetailedDiscrepancy>
            {
                new DetailedDiscrepancy
                {
                    Module = "Module1",
                    Category = "DataValue",
                    Severity = DiscrepancySeverity.High,
                    ImpactPercentage = 10,
                    IsBlocker = false
                }
            },
            Summary = new DiscrepancySummary
            {
                TotalDiscrepancies = 1,
                HighCount = 1,
                BlockerCount = 0,
                DiscrepanciesByModule = new Dictionary<string, int> { { "Module1", 1 } }
            },
            AffectedModules = new List<string> { "Module1" }
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeGenerator()
    {
        // Act
        var generator = new ReportGenerator();

        // Assert
        generator.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateFullReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var report = await _generator.GenerateFullReportAsync("session-1", "client-1", null, null, discrepancies, risk);

        // Assert
        report.Should().NotBeNull();
        report.Success.Should().BeTrue();
        report.MarkdownContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateExecutiveSummaryAsync_ShouldGenerateSummary()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var report = await _generator.GenerateExecutiveSummaryAsync("session-1", "client-1", discrepancies, risk);

        // Assert
        report.Type.Should().Be(ReportType.ExecutiveSummary);
        report.MarkdownContent.Should().NotBeNullOrEmpty();
        report.ExecutiveSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateTechnicalReportAsync_ShouldGenerateTechnicalReport()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var report = await _generator.GenerateTechnicalReportAsync("session-1", "client-1", discrepancies, risk);

        // Assert
        report.Type.Should().Be(ReportType.Technical);
        report.MarkdownContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateExecutiveSummary_ShouldCreateExecutiveSummary()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var summary = _generator.GenerateExecutiveSummary(discrepancies, risk);

        // Assert
        summary.Should().NotBeNull();
        summary.Decision.Should().NotBeNullOrEmpty();
        summary.KeyMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateDiscoverySection_ShouldGenerateDiscoverySection()
    {
        // Arrange
        var discoveries = new DiscoveryFindings
        {
            TablesAnalyzed = 50,
            ColumnsAnalyzed = 500,
            StoredProceduresFound = 25,
            RulesIdentified = 100
        };

        // Act
        var section = _generator.GenerateDiscoverySection(discoveries);

        // Assert
        section.TablesAnalyzed.Should().Be(50);
        section.ColumnsAnalyzed.Should().Be(500);
    }

    [Fact]
    public void GenerateExecutionSection_ShouldGenerateExecutionSection()
    {
        // Arrange
        var results = new ExecutionResults
        {
            TotalTestsRun = 100,
            LegacyPassedTests = 95,
            BlazonPassedTests = 95
        };

        // Act
        var section = _generator.GenerateExecutionSection(results);

        // Assert
        section.TotalTestsRun.Should().Be(100);
        section.LegacyPassedTests.Should().Be(95);
    }

    [Fact]
    public void GenerateComparisonSection_ShouldGenerateComparisonSection()
    {
        // Arrange
        var comparisons = new List<TableComparisonResult>
        {
            new TableComparisonResult
            {
                TableName = "Test",
                LegacyRecordCount = 100,
                BlazonRecordCount = 100,
                MatchedRecords = 95,
                MatchPercentage = 95,
                Status = ComparisonStatus.Compatible
            }
        };

        // Act
        var section = _generator.GenerateComparisonSection(comparisons);

        // Assert
        section.TablesCompared.Should().Be(1);
        section.OverallMatchPercentage.Should().Be(95);
    }

    [Fact]
    public void GenerateDiscrepanciesSection_ShouldGenerateDiscrepanciesSection()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();

        // Act
        var section = _generator.GenerateDiscrepanciesSection(discrepancies);

        // Assert
        section.TotalDiscrepancies.Should().Be(1);
        section.HighCount.Should().Be(1);
    }

    [Fact]
    public void GenerateRiskSection_ShouldGenerateRiskSection()
    {
        // Arrange
        var risk = CreateTestRiskAssessment();

        // Act
        var section = _generator.GenerateRiskSection(risk);

        // Assert
        section.OverallRiskScore.Should().Be(45);
        section.OverallDecision.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateActionItems_ShouldGenerateActionItems()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var items = _generator.GenerateActionItems(discrepancies, risk);

        // Assert
        items.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateRecommendations_ShouldGenerateRecommendations()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var recommendations = _generator.GenerateRecommendations(discrepancies, risk);

        // Assert
        recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateActionPlan_ShouldCreatePlan()
    {
        // Arrange
        var items = new List<CriticalRiskItem>
        {
            new CriticalRiskItem
            {
                Title = "Fix Issue",
                Priority = RiskPriority.Critical,
                EstimatedFixHours = 16
            }
        };

        // Act
        var plan = _generator.CreateActionPlan(items, new());

        // Assert
        plan.Should().NotBeEmpty();
        plan[0].Title.Should().Contain("Fix Issue");
    }

    [Fact]
    public void ConvertToMarkdown_ShouldGenerateMarkdown()
    {
        // Arrange
        var report = new ValidationReport
        {
            Title = "Test Report",
            SessionId = "session-1",
            ClientId = "client-1",
            ExecutiveSummary = new ExecutiveSummary { Decision = "Go" }
        };

        // Act
        var markdown = _generator.ConvertToMarkdown(report);

        // Assert
        markdown.Should().Contain("# Test Report");
        markdown.Should().Contain("session-1");
    }

    [Fact]
    public void GenerateMarkdownSection_ShouldGenerateSection()
    {
        // Act
        var section = _generator.GenerateMarkdownSection("Test", 1, "Content");

        // Assert
        section.Should().Contain("# Test");
        section.Should().Contain("Content");
    }

    [Fact]
    public void GenerateMarkdownTable_ShouldGenerateTable()
    {
        // Arrange
        var table = new ReportTable
        {
            Title = "Test Table",
            Headers = new List<string> { "Col1", "Col2" },
            Rows = new List<List<string>>
            {
                new List<string> { "Val1", "Val2" }
            }
        };

        // Act
        var markdown = _generator.GenerateMarkdownTable(table);

        // Assert
        markdown.Should().Contain("Test Table");
        markdown.Should().Contain("Col1");
        markdown.Should().Contain("Val1");
    }

    [Fact]
    public void GenerateTableOfContents_ShouldGenerateTOC()
    {
        // Arrange
        var sections = new List<string> { "Section 1", "Section 2" };

        // Act
        var toc = _generator.GenerateTableOfContents(sections);

        // Assert
        toc.Should().Contain("Table of Contents");
        toc.Should().Contain("Section 1");
    }

    [Theory]
    [InlineData("Go", "✅")]
    [InlineData("NoGo", "🚫")]
    [InlineData("Delay", "🔄")]
    public void FormatStatus_ShouldFormatCorrectly(string status, string expected)
    {
        // Act
        var formatted = _generator.FormatStatus(status);

        // Assert
        formatted.Should().Be(expected);
    }

    [Theory]
    [InlineData("Critical", "🔴")]
    [InlineData("High", "🟠")]
    [InlineData("Medium", "🟡")]
    [InlineData("Low", "🟢")]
    public void FormatSeverity_ShouldFormatCorrectly(string severity, string expected)
    {
        // Act
        var formatted = _generator.FormatSeverity(severity);

        // Assert
        formatted.Should().Be(expected);
    }

    [Fact]
    public void FormatPercentage_ShouldFormatCorrectly()
    {
        // Act
        var formatted = _generator.FormatPercentage(95.5);

        // Assert
        formatted.Should().Be("95.5%");
    }

    [Fact]
    public void FormatNumber_ShouldFormatCorrectly()
    {
        // Act
        var formatted = _generator.FormatNumber(1000);

        // Assert
        formatted.Should().Be("1,000");
    }

    [Fact]
    public void ExtractKeyMetrics_ShouldExtractMetrics()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var metrics = _generator.ExtractKeyMetrics(discrepancies, risk);

        // Assert
        metrics.Should().NotBeEmpty();
        metrics.Should().Contain(m => m.MetricName == "Risk Score");
    }

    [Fact]
    public void GenerateSummaryStats_ShouldGenerateStats()
    {
        // Arrange
        var discrepancies = CreateTestDiscrepancies();
        var risk = CreateTestRiskAssessment();

        // Act
        var stats = _generator.GenerateSummaryStats(discrepancies, risk);

        // Assert
        stats.Should().NotBeEmpty();
        stats.Should().ContainKey("Overall Risk Score");
    }

    [Fact]
    public void GenerateDistributionChart_ShouldGenerateChart()
    {
        // Arrange
        var data = new Dictionary<string, int>
        {
            { "Category1", 10 },
            { "Category2", 20 }
        };

        // Act
        var chart = _generator.GenerateDistributionChart(data, "Test Chart");

        // Assert
        chart.Should().Contain("Test Chart");
        chart.Should().Contain("Category1");
    }

    [Fact]
    public async Task ExportToFileAsync_ShouldExportFile()
    {
        // Arrange
        var report = new ValidationReport
        {
            SessionId = "session-1",
            MarkdownContent = "Test content"
        };
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var success = await _generator.ExportToFileAsync(report, tempFile);

            // Assert
            success.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExportMultipleFormatsAsync_ShouldExportMultipleFormats()
    {
        // Arrange
        var report = new ValidationReport
        {
            SessionId = "session-1",
            MarkdownContent = "Test content"
        };
        var tempDir = Path.GetTempPath();

        // Act
        var results = await _generator.ExportMultipleFormatsAsync(report, tempDir);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().ContainKey("Markdown");
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var config = new ReportGenerationConfig
        {
            IncludeDiscrepancies = false,
            MaxIssuesToList = 10
        };

        // Act
        _generator.Configure(config);
        var retrieved = _generator.GetConfiguration();

        // Assert
        retrieved.IncludeDiscrepancies.Should().BeFalse();
        retrieved.MaxIssuesToList.Should().Be(10);
    }
}
