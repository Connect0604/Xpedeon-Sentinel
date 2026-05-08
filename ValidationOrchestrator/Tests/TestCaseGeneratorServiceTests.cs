namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for TestCaseGeneratorService (Phase 2)
/// </summary>
public class TestCaseGeneratorServiceTests
{
    private static TestGenerationConfig DefaultConfig() => new()
    {
        UseAiGeneration = false,    // disable AI in unit tests
        GenerateHappyPaths = true,
        GenerateEdgeCases = true,
        GenerateNegativeCases = true,
        GeneratePerformanceCases = true,
        TestCasesPerCriticalItem = 5,
        TestCasesPerHighItem = 3,
        TestCasesPerMediumItem = 2
    };

    private static BusinessLogicItem MakeItem(
        string module = "Finance",
        BusinessLogicType type = BusinessLogicType.Calculation,
        BusinessLogicRisk risk = BusinessLogicRisk.Critical,
        bool tribal = false) => new()
    {
        Module = module,
        Name = "Tax calculation",
        Description = "Calculates GST on invoice amount",
        Type = type,
        Risk = risk,
        IsTribalKnowledge = tribal
    };

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var service = new TestCaseGeneratorService();
        service.Should().NotBeNull();
    }

    [Fact]
    public void GenerateHappyPaths_ShouldReturnCases()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem();

        var cases = service.GenerateHappyPaths(item, DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().OnlyContain(c => c.Type == TestCaseType.HappyPath);
        cases.Should().OnlyContain(c => c.Module == "Finance");
    }

    [Fact]
    public void GenerateEdgeCases_ShouldReturnCasesForCalculation()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem(type: BusinessLogicType.Calculation);

        var cases = service.GenerateEdgeCases(item, DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().OnlyContain(c => c.Type == TestCaseType.EdgeCase);
    }

    [Fact]
    public void GenerateEdgeCases_ShouldIncludeTribalKnowledgeCase()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem(tribal: true);

        var cases = service.GenerateEdgeCases(item, DefaultConfig());

        cases.Should().Contain(c => c.Tags.Contains("TribalKnowledgeScenario"));
    }

    [Fact]
    public void GenerateEdgeCases_ShouldReturnWorkflowCases()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem(type: BusinessLogicType.Workflow, risk: BusinessLogicRisk.High);

        var cases = service.GenerateEdgeCases(item, DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().Contain(c => c.Tags.Contains("AllApprovalLevels"));
    }

    [Fact]
    public void GenerateNegativeCases_ShouldReturnErrorConditions()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem();

        var cases = service.GenerateNegativeCases(item, DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().OnlyContain(c => c.Type == TestCaseType.NegativeCase);
    }

    [Fact]
    public void GeneratePerformanceCases_ShouldReturnPerformanceCase()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem();

        var cases = service.GeneratePerformanceCases(item, DefaultConfig());

        cases.Should().HaveCount(1);
        cases.First().Type.Should().Be(TestCaseType.Performance);
        cases.First().Priority.Should().Be(TestCasePriority.Medium);
    }

    [Fact]
    public async Task GenerateForLogicItemAsync_ShouldReturnAllTypes()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem();

        var cases = await service.GenerateForLogicItemAsync(item, DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().Contain(c => c.Type == TestCaseType.HappyPath);
        cases.Should().Contain(c => c.Type == TestCaseType.EdgeCase);
        cases.Should().Contain(c => c.Type == TestCaseType.NegativeCase);
        cases.Should().Contain(c => c.Type == TestCaseType.Performance);
    }

    [Fact]
    public async Task GenerateForLogicItemAsync_ShouldTagWithItemId()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem();

        var cases = await service.GenerateForLogicItemAsync(item, DefaultConfig());

        cases.Should().OnlyContain(c => c.BusinessLogicItemId == item.Id);
    }

    [Fact]
    public void BuildSuccessCriteria_FinanceModule_ShouldRequireZeroTolerance()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem(module: "Finance", type: BusinessLogicType.Calculation);

        var criteria = service.BuildSuccessCriteria(item, "FinancialAccuracy", DefaultConfig());

        criteria.NumericTolerance.Should().Be(0m);
        criteria.RequireExactMatch.Should().BeTrue();
        criteria.ExactMatchFields.Should().Contain("Amount");
    }

    [Fact]
    public void BuildSuccessCriteria_Performance_ShouldAllowDegradation()
    {
        var service = new TestCaseGeneratorService();
        var item = MakeItem(module: "Inventory", type: BusinessLogicType.BusinessRule, risk: BusinessLogicRisk.Medium);

        var criteria = service.BuildSuccessCriteria(item, "Performance", DefaultConfig());

        criteria.RequireExactMatch.Should().BeFalse();
        criteria.MaxPerformanceDegradation.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildGlobalCriteria_ShouldReturnAtLeastTwoCriteria()
    {
        var service = new TestCaseGeneratorService();
        var items = new List<BusinessLogicItem> { MakeItem(), MakeItem(module: "HR", risk: BusinessLogicRisk.High) };

        var criteria = service.BuildGlobalCriteria(items, DefaultConfig());

        criteria.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void DeduplicateCases_ShouldRemoveDuplicates()
    {
        var service = new TestCaseGeneratorService();
        var cases = new List<TestCase>
        {
            new() { Name = "[HAPPY] Finance — Tax calc — scenario 1", Module = "Finance", Type = TestCaseType.HappyPath },
            new() { Name = "[HAPPY] Finance — Tax calc — scenario 1", Module = "Finance", Type = TestCaseType.HappyPath },
            new() { Name = "[EDGE] Finance — Tax calc — ZeroValue", Module = "Finance", Type = TestCaseType.EdgeCase }
        };

        var deduped = service.DeduplicateCases(cases);

        deduped.Should().HaveCountLessThan(cases.Count);
    }

    [Fact]
    public void PrioritizeCases_ShouldReturnCriticalFirst()
    {
        var service = new TestCaseGeneratorService();
        var cases = new List<TestCase>
        {
            new() { Priority = TestCasePriority.Low, Module = "HR", Type = TestCaseType.HappyPath },
            new() { Priority = TestCasePriority.Critical, Module = "Finance", Type = TestCaseType.HappyPath },
            new() { Priority = TestCasePriority.High, Module = "Procurement", Type = TestCaseType.EdgeCase }
        };

        var prioritized = service.PrioritizeCases(cases);

        prioritized.First().Priority.Should().Be(TestCasePriority.Critical);
        prioritized.Last().Priority.Should().Be(TestCasePriority.Low);
    }

    [Fact]
    public void AssembleScenarios_ShouldGroupByModule()
    {
        var service = new TestCaseGeneratorService();
        var cases = new List<TestCase>
        {
            new() { Module = "Finance", Type = TestCaseType.HappyPath, Priority = TestCasePriority.Critical, Name = "F1" },
            new() { Module = "Finance", Type = TestCaseType.EdgeCase, Priority = TestCasePriority.High, Name = "F2" },
            new() { Module = "Inventory", Type = TestCaseType.HappyPath, Priority = TestCasePriority.High, Name = "I1" }
        };

        var scenarios = service.AssembleScenarios(cases, new());

        scenarios.Should().NotBeEmpty();
        scenarios.Should().Contain(s => s.Module == "Finance");
        scenarios.Should().Contain(s => s.Module == "Inventory");
    }

    [Fact]
    public async Task GenerateForModuleAsync_ShouldReturnCasesForAllItems()
    {
        var service = new TestCaseGeneratorService();
        var items = new List<BusinessLogicItem>
        {
            MakeItem("Procurement", BusinessLogicType.Workflow, BusinessLogicRisk.High),
            MakeItem("Procurement", BusinessLogicType.BusinessRule, BusinessLogicRisk.Medium)
        };

        var cases = await service.GenerateForModuleAsync("Procurement", items, new(), DefaultConfig());

        cases.Should().NotBeEmpty();
        cases.Should().OnlyContain(c => c.Module == "Procurement");
    }

    [Fact]
    public void Configure_ShouldNotThrow()
    {
        var service = new TestCaseGeneratorService();
        var act = () => service.Configure(new TestGenerationConfig { UseAiGeneration = false });
        act.Should().NotThrow();
    }
}
