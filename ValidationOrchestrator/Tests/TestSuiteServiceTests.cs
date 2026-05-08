namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using Moq;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for TestSuiteService (Phase 2 orchestrator)
/// </summary>
public class TestSuiteServiceTests
{
    private const string TestSessionId = "test-session-tss";
    private const string TestClientId = "test-client-tss";

    private static TestSuiteService CreateService(
        Mock<ITestCaseGeneratorService>? generatorMock = null,
        Mock<ITestDataService>? dataMock = null,
        Mock<IValidationMemoryService>? memoryMock = null)
    {
        generatorMock ??= CreateDefaultGeneratorMock();
        dataMock ??= CreateDefaultDataMock();
        memoryMock ??= CreateDefaultMemoryMock();

        return new TestSuiteService(
            generatorMock.Object,
            dataMock.Object,
            memoryMock.Object);
    }

    private static Mock<ITestCaseGeneratorService> CreateDefaultGeneratorMock()
    {
        var mock = new Mock<ITestCaseGeneratorService>();

        mock.Setup(m => m.GenerateForMatrixAsync(
                It.IsAny<ValidationMatrix>(),
                It.IsAny<List<BusinessLogicItem>>(),
                It.IsAny<TestGenerationConfig>(),
                It.IsAny<IProgress<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestCase>
            {
                new() { Module = "Finance", Dimension = "FinancialAccuracy", Type = TestCaseType.HappyPath, Priority = TestCasePriority.Critical, Name = "TC-1", MatrixEntryId = "entry-1" },
                new() { Module = "Finance", Dimension = "DataIntegrity", Type = TestCaseType.EdgeCase, Priority = TestCasePriority.High, Name = "TC-2", MatrixEntryId = "entry-2" },
                new() { Module = "Inventory", Dimension = "InventoryConsistency", Type = TestCaseType.HappyPath, Priority = TestCasePriority.Medium, Name = "TC-3", MatrixEntryId = "entry-3" }
            });

        mock.Setup(m => m.AssembleScenarios(It.IsAny<List<TestCase>>(), It.IsAny<List<BusinessLogicItem>>()))
            .Returns(new List<TestScenario>
            {
                new() { Module = "Finance", Name = "Finance E2E", Type = ScenarioType.EndToEnd, Priority = TestCasePriority.Critical }
            });

        mock.Setup(m => m.BuildGlobalCriteria(It.IsAny<List<BusinessLogicItem>>(), It.IsAny<TestGenerationConfig>()))
            .Returns(new List<SuccessCriteria>
            {
                new() { RequireExactMatch = true, NumericTolerance = 0m }
            });

        mock.Setup(m => m.Configure(It.IsAny<TestGenerationConfig>()));
        return mock;
    }

    private static Mock<ITestDataService> CreateDefaultDataMock()
    {
        var mock = new Mock<ITestDataService>();

        mock.Setup(m => m.GenerateAllDatasetsAsync(
                It.IsAny<TestSuite>(),
                It.IsAny<List<BusinessLogicItem>>(),
                It.IsAny<TestGenerationConfig>(),
                It.IsAny<IProgress<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestDataSet>
            {
                new() { Module = "Finance", Category = TestDataCategory.Normal, Name = "Finance data" },
                new() { Module = "Inventory", Category = TestDataCategory.Normal, Name = "Inventory data" }
            });

        mock.Setup(m => m.Configure(It.IsAny<TestGenerationConfig>()));
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

    private static ValidationMatrix MakeMatrix() => new()
    {
        SessionId = TestSessionId,
        ClientId = TestClientId,
        Modules = new() { "Finance", "Inventory" },
        Dimensions = new() { "FinancialAccuracy", "DataIntegrity", "InventoryConsistency" },
        Entries = new()
        {
            new() { Id = "entry-1", Module = "Finance", Dimension = "FinancialAccuracy", Priority = MatrixPriority.Critical, EstimatedTestCases = 10 },
            new() { Id = "entry-2", Module = "Finance", Dimension = "DataIntegrity", Priority = MatrixPriority.High, EstimatedTestCases = 5 },
            new() { Id = "entry-3", Module = "Inventory", Dimension = "InventoryConsistency", Priority = MatrixPriority.Medium, EstimatedTestCases = 3 }
        }
    };

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var service = CreateService();
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task StoreSuiteAsync_AndGetSuiteAsync_ShouldRoundTrip()
    {
        var service = CreateService();
        var suite = new TestSuite
        {
            SessionId = TestSessionId,
            ClientId = TestClientId,
            Name = "Test Suite",
            Status = TestSuiteStatus.Ready
        };

        await service.StoreSuiteAsync(TestSessionId, TestClientId, suite);
        var retrieved = await service.GetSuiteAsync(TestSessionId, TestClientId);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Suite");
        retrieved.Status.Should().Be(TestSuiteStatus.Ready);
    }

    [Fact]
    public async Task GetSuiteAsync_ShouldReturnNullWhenNotStored()
    {
        var service = CreateService();

        var suite = await service.GetSuiteAsync("unknown-session", "unknown-client");

        suite.Should().BeNull();
    }

    [Fact]
    public async Task AddTestCasesAsync_ShouldAppendCases()
    {
        var service = CreateService();
        var initial = new TestSuite { SessionId = TestSessionId, ClientId = TestClientId };
        await service.StoreSuiteAsync(TestSessionId, TestClientId, initial);

        var newCases = new List<TestCase>
        {
            new() { Module = "Finance", Name = "TC-NEW", Type = TestCaseType.HappyPath }
        };

        await service.AddTestCasesAsync(TestSessionId, TestClientId, newCases);
        var retrieved = await service.GetSuiteAsync(TestSessionId, TestClientId);

        retrieved!.TestCases.Should().HaveCount(1);
        retrieved.TestCases.First().Name.Should().Be("TC-NEW");
    }

    [Fact]
    public async Task AddDataSetsAsync_ShouldAppendDataSets()
    {
        var service = CreateService();
        var initial = new TestSuite { SessionId = TestSessionId, ClientId = TestClientId };
        await service.StoreSuiteAsync(TestSessionId, TestClientId, initial);

        await service.AddDataSetsAsync(TestSessionId, TestClientId, new()
        {
            new() { Module = "Finance", Name = "Finance DS" }
        });

        var retrieved = await service.GetSuiteAsync(TestSessionId, TestClientId);
        retrieved!.DataSets.Should().HaveCount(1);
    }

    [Fact]
    public async Task FinalizeSuiteAsync_ShouldSetStatusReady()
    {
        var service = CreateService();
        var suite = new TestSuite { SessionId = TestSessionId, ClientId = TestClientId, Status = TestSuiteStatus.Building };
        await service.StoreSuiteAsync(TestSessionId, TestClientId, suite);

        var finalized = await service.FinalizeSuiteAsync(TestSessionId, TestClientId);

        finalized.Status.Should().Be(TestSuiteStatus.Ready);
        finalized.FinalizedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FinalizeSuiteAsync_ShouldThrowWhenNoSuiteFound()
    {
        var service = CreateService();

        var act = async () => await service.FinalizeSuiteAsync("no-session", "no-client");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void CalculateMatrixCoverage_ShouldReturnHighCoverageWhenAllEntriesCovered()
    {
        var service = CreateService();
        var matrix = MakeMatrix();

        var suite = new TestSuite
        {
            SessionId = TestSessionId,
            CoveredModules = new() { "Finance", "Inventory" },
            CoveredDimensions = new() { "FinancialAccuracy", "DataIntegrity", "InventoryConsistency" },
            TestCases = matrix.Entries.Select(e => new TestCase { MatrixEntryId = e.Id, Module = e.Module, Dimension = e.Dimension }).ToList()
        };

        var coverage = service.CalculateMatrixCoverage(suite, matrix);

        coverage.Should().Be(100.0);
    }

    [Fact]
    public void CalculateMatrixCoverage_ShouldReturnZeroForEmptySuite()
    {
        var service = CreateService();
        var matrix = MakeMatrix();
        var suite = new TestSuite { SessionId = TestSessionId, TestCases = new() };

        var coverage = service.CalculateMatrixCoverage(suite, matrix);

        coverage.Should().Be(0);
    }

    [Fact]
    public void FindUncoveredEntries_ShouldReturnEntriesWithoutTestCases()
    {
        var service = CreateService();
        var matrix = MakeMatrix();
        var suite = new TestSuite
        {
            TestCases = new() { new() { MatrixEntryId = "entry-1", Module = "Finance", Dimension = "FinancialAccuracy" } },
            CoveredModules = new() { "Finance" },
            CoveredDimensions = new() { "FinancialAccuracy" }
        };

        var uncovered = service.FindUncoveredEntries(suite, matrix);

        uncovered.Should().NotBeEmpty();
        uncovered.Should().NotContain(e => e.Id == "entry-1");
    }

    [Fact]
    public void EstimateExecutionTime_ShouldReturnPositiveValue()
    {
        var service = CreateService();
        var suite = new TestSuite
        {
            TestCases = new()
            {
                new() { Priority = TestCasePriority.Critical },
                new() { Priority = TestCasePriority.High },
                new() { Priority = TestCasePriority.Medium }
            }
        };

        var estimate = service.EstimateExecutionTime(suite);

        estimate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSuiteReport_ShouldContainKeyMetrics()
    {
        var service = CreateService();
        var suite = new TestSuite
        {
            SessionId = TestSessionId,
            TestCases = new()
            {
                new() { Module = "Finance", Priority = TestCasePriority.Critical, Type = TestCaseType.HappyPath, Name = "TC-1" }
            },
            MatrixCoverage = 85.0,
            Status = TestSuiteStatus.Ready
        };

        var report = service.GenerateSuiteReport(suite);

        report.Should().Contain("Test Suite Report");
        report.Should().Contain("Finance");
        report.Should().Contain("85");
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnZeroInitially()
    {
        var service = CreateService();

        var progress = await service.GetProgressAsync(TestSessionId, TestClientId);

        progress.Should().Be(0);
    }

    [Fact]
    public void Configure_ShouldNotThrow()
    {
        var service = CreateService();
        var act = () => service.Configure(new TestSuiteServiceConfig { MinimumCoverageThreshold = 80 });
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RunTestGenerationAsync_ShouldReturnSuccessfulSuite()
    {
        var service = CreateService();
        var matrix = MakeMatrix();
        var items = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Risk = BusinessLogicRisk.Critical, Name = "Tax calc", Type = BusinessLogicType.Calculation }
        };

        var suite = await service.RunTestGenerationAsync(
            TestSessionId, TestClientId, matrix, items, new TestGenerationConfig { UseAiGeneration = false });

        suite.Should().NotBeNull();
        suite.Success.Should().BeTrue();
        suite.TotalTestCases.Should().BeGreaterThan(0);
        suite.DataSets.Should().NotBeEmpty();
    }
}
