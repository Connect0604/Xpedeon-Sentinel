namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using Moq;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for DiscoveryService (Phase 1 orchestration)
/// </summary>
public class DiscoveryServiceTests
{
    private const string TestSessionId = "test-session-ds";
    private const string TestClientId = "test-client-ds";

    private static DiscoveryService CreateService(
        Mock<IBusinessLogicExtractor>? extractorMock = null,
        Mock<ISchemaAnalyzer>? schemaMock = null,
        Mock<IValidationMemoryService>? memoryMock = null)
    {
        extractorMock ??= CreateDefaultExtractorMock();
        schemaMock ??= new Mock<ISchemaAnalyzer>();
        memoryMock ??= CreateDefaultMemoryMock();

        return new DiscoveryService(
            extractorMock.Object,
            schemaMock.Object,
            memoryMock.Object);
    }

    private static Mock<IBusinessLogicExtractor> CreateDefaultExtractorMock()
    {
        var mock = new Mock<IBusinessLogicExtractor>();

        mock.Setup(m => m.DiscoverCodeFilesAsync(It.IsAny<string>(), It.IsAny<BusinessLogicExtractionConfig>()))
            .ReturnsAsync(new List<CodeFile>
            {
                new() { FileName = "InvoiceCalc.cs", Module = "Finance", Content = "decimal tax = amount * 0.18m;" },
                new() { FileName = "StockManager.cs", Module = "Inventory", Content = "if (stock < minLevel) Alert();" }
            });

        mock.Setup(m => m.ExtractFromCodebaseAsync(
                It.IsAny<List<CodeFile>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IProgress<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLogicItem>
            {
                new() { Module = "Finance", Name = "Tax calculation", Type = BusinessLogicType.Calculation, Risk = BusinessLogicRisk.Critical, IsCritical = true },
                new() { Module = "Inventory", Name = "Min stock alert", Type = BusinessLogicType.BusinessRule, Risk = BusinessLogicRisk.High }
            });

        mock.Setup(m => m.Configure(It.IsAny<BusinessLogicExtractionConfig>()));

        return mock;
    }

    private static Mock<IValidationMemoryService> CreateDefaultMemoryMock()
    {
        var mock = new Mock<IValidationMemoryService>();

        mock.Setup(m => m.UpdateSessionPhaseStatusAsync(
                It.IsAny<string>(),
                It.IsAny<SessionPhaseStatus>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var service = CreateService();
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StoreAndRetrieveFindings_ShouldRoundTrip()
    {
        // Arrange
        var service = CreateService();
        var items = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Name = "Tax calc", Risk = BusinessLogicRisk.Critical },
            new() { Module = "HR", Name = "Payroll rule", Risk = BusinessLogicRisk.High }
        };

        // Act
        await service.StoreDiscoveryFindingsAsync(TestSessionId, TestClientId, items);
        var retrieved = await service.GetDiscoveryFindingsAsync(TestSessionId, TestClientId);

        // Assert
        retrieved.Should().HaveCount(2);
        retrieved.Should().Contain(i => i.Name == "Tax calc");
    }

    [Fact]
    public async Task AddFinding_ShouldAppendToExistingList()
    {
        // Arrange
        var service = CreateService();
        var initial = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Name = "Item 1", Risk = BusinessLogicRisk.High }
        };
        await service.StoreDiscoveryFindingsAsync(TestSessionId, TestClientId, initial);

        var newItem = new BusinessLogicItem { Module = "Finance", Name = "Item 2", Risk = BusinessLogicRisk.Critical };

        // Act
        await service.AddFindingAsync(TestSessionId, TestClientId, newItem);
        var all = await service.GetDiscoveryFindingsAsync(TestSessionId, TestClientId);

        // Assert
        all.Should().HaveCount(2);
        all.Should().Contain(i => i.Name == "Item 2");
    }

    [Fact]
    public async Task RecordExpertInterview_ShouldStoreAndReturn()
    {
        // Arrange
        var service = CreateService();
        await service.StoreDiscoveryFindingsAsync(TestSessionId, TestClientId, new());

        var findings = new List<ExpertFinding>
        {
            new() { Module = "Finance", FindingType = "TribalKnowledge", Description = "Hidden rounding logic", Risk = BusinessLogicRisk.Critical, RequiresTestCase = true }
        };

        // Act
        var record = await service.RecordExpertInterviewAsync(
            TestSessionId, TestClientId,
            "Jane Finance", "Finance Lead",
            findings, "Raw notes from interview");

        // Assert
        record.Should().NotBeNull();
        record.ExpertName.Should().Be("Jane Finance");
        record.ExpertRole.Should().Be("Finance Lead");
        record.Findings.Should().HaveCount(1);

        var interviews = await service.GetExpertInterviewsAsync(TestSessionId, TestClientId);
        interviews.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApplyExpertFeedback_ShouldAddTribalKnowledgeItems()
    {
        // Arrange
        var service = CreateService();
        await service.StoreDiscoveryFindingsAsync(TestSessionId, TestClientId, new());

        var interview = new ExpertInterviewRecord
        {
            SessionId = TestSessionId,
            ExpertName = "Bob Ops",
            ExpertRole = "Operations",
            Findings = new List<ExpertFinding>
            {
                new() { Module = "Inventory", FindingType = "TribalKnowledge", Description = "Undocumented FIFO override", Risk = BusinessLogicRisk.Critical }
            }
        };

        // Act
        await service.ApplyExpertFeedbackAsync(TestSessionId, TestClientId, interview);
        var items = await service.GetDiscoveryFindingsAsync(TestSessionId, TestClientId);

        // Assert
        items.Should().Contain(i => i.IsTribalKnowledge && i.Module == "Inventory");
    }

    [Fact]
    public void ComputeModuleRisks_ShouldRankByCriticalCount()
    {
        // Arrange
        var service = CreateService();
        var items = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Risk = BusinessLogicRisk.Critical },
            new() { Module = "Finance", Risk = BusinessLogicRisk.Critical },
            new() { Module = "HR", Risk = BusinessLogicRisk.Low },
        };

        // Act
        var risks = service.ComputeModuleRisks(items);

        // Assert
        risks.Should().NotBeEmpty();
        risks.First().Module.Should().Be("Finance"); // highest risk first
        risks.First().CriticalItems.Should().Be(2);
    }

    [Fact]
    public void ComputeModuleRisks_ShouldSetCorrectRiskLevel()
    {
        // Arrange
        var service = CreateService();
        var items = Enumerable.Range(0, 5)
            .Select(_ => new BusinessLogicItem { Module = "Finance", Risk = BusinessLogicRisk.Critical })
            .ToList();

        // Act
        var risks = service.ComputeModuleRisks(items);
        var financeRisk = risks.First(r => r.Module == "Finance");

        // Assert
        financeRisk.RiskLevel.Should().BeOneOf("Critical", "High");
        financeRisk.RiskScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDiscoveryProgress_ShouldReturnZeroInitially()
    {
        // Arrange
        var service = CreateService();

        // Act
        var progress = await service.GetDiscoveryProgressAsync(TestSessionId, TestClientId);

        // Assert
        progress.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDiscoveryReport_ShouldIncludeAllFindings()
    {
        // Arrange
        var service = CreateService();
        var items = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Name = "Tax calc", Risk = BusinessLogicRisk.Critical, IsCritical = true },
            new() { Module = "Finance", Name = "Invoice rule", Risk = BusinessLogicRisk.High },
            new() { Module = "Inventory", Name = "Stock rule", Risk = BusinessLogicRisk.Medium }
        };
        await service.StoreDiscoveryFindingsAsync(TestSessionId, TestClientId, items);

        // Act
        var report = await service.GenerateDiscoveryReportAsync(TestSessionId, TestClientId);

        // Assert
        report.Should().NotBeNull();
        report.TotalLogicItems.Should().Be(3);
        report.CriticalItems.Should().Be(1);
        report.ModulesDiscovered.Should().Be(2);
        report.ModuleRisks.Should().NotBeEmpty();
        report.Recommendations.Should().NotBeEmpty();
        report.ExecutiveSummary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configure_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.Configure(new DiscoveryServiceConfig { CompressFindings = false });

        // Assert
        act.Should().NotThrow();
    }
}
