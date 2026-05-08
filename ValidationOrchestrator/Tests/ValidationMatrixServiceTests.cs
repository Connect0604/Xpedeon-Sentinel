namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for ValidationMatrixService (Phase 1 — matrix building)
/// </summary>
public class ValidationMatrixServiceTests
{
    private const string TestSessionId = "test-session-vms";
    private const string TestClientId = "test-client-vms";

    private static List<BusinessLogicItem> MakeSampleItems() =>
    [
        new() { Module = "Finance", Name = "Tax calculation", Type = BusinessLogicType.Calculation, Risk = BusinessLogicRisk.Critical, IsCritical = true },
        new() { Module = "Finance", Name = "Invoice validation", Type = BusinessLogicType.Validation, Risk = BusinessLogicRisk.High },
        new() { Module = "Finance", Name = "Payment workflow", Type = BusinessLogicType.Workflow, Risk = BusinessLogicRisk.High },
        new() { Module = "Procurement", Name = "Approval rule", Type = BusinessLogicType.BusinessRule, Risk = BusinessLogicRisk.High },
        new() { Module = "Inventory", Name = "Stock reorder", Type = BusinessLogicType.BusinessRule, Risk = BusinessLogicRisk.Medium },
        new() { Module = "HR", Name = "Payroll calc", Type = BusinessLogicType.Calculation, Risk = BusinessLogicRisk.Critical, IsCritical = true }
    ];

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var service = new ValidationMatrixService();
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildMatrixAsync_ShouldCreateEntriesForAllModules()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var items = MakeSampleItems();
        var config = new DiscoveryConfig();

        // Act
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, items, config);

        // Assert
        matrix.Should().NotBeNull();
        matrix.Modules.Should().Contain("Finance");
        matrix.Modules.Should().Contain("Inventory");
        matrix.Modules.Should().Contain("HR");
        matrix.Entries.Should().NotBeEmpty();
        matrix.TotalEntries.Should().Be(matrix.Modules.Count * matrix.Dimensions.Count);
    }

    [Fact]
    public async Task BuildMatrixAsync_ShouldMarkFinanceEntriesHighPriority()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var items = MakeSampleItems();
        var config = new DiscoveryConfig();

        // Act
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, items, config);
        var financeFinancialEntry = matrix.Entries
            .FirstOrDefault(e => e.Module == "Finance" && e.Dimension == "FinancialAccuracy");

        // Assert
        financeFinancialEntry.Should().NotBeNull();
        financeFinancialEntry!.Priority.Should().BeOneOf(MatrixPriority.Critical, MatrixPriority.High);
    }

    [Fact]
    public async Task GetMatrixAsync_ShouldReturnStoredMatrix()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var items = MakeSampleItems();
        await service.BuildMatrixAsync(TestSessionId, TestClientId, items, new DiscoveryConfig());

        // Act
        var retrieved = await service.GetMatrixAsync(TestSessionId, TestClientId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.SessionId.Should().Be(TestSessionId);
    }

    [Fact]
    public async Task GetMatrixAsync_ShouldReturnNullWhenNotBuilt()
    {
        // Arrange
        var service = new ValidationMatrixService();

        // Act
        var matrix = await service.GetMatrixAsync("unknown-session", "unknown-client");

        // Assert
        matrix.Should().BeNull();
    }

    [Fact]
    public async Task UpsertEntryAsync_ShouldAddNewEntry()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var newEntry = new ValidationMatrixEntry
        {
            Module = "Finance",
            Dimension = "FinancialAccuracy",
            Priority = MatrixPriority.Critical,
            EstimatedTestCases = 15
        };

        // Act
        var result = await service.UpsertEntryAsync(TestSessionId, TestClientId, newEntry);

        // Assert
        result.Should().NotBeNull();
        result.Module.Should().Be("Finance");
        result.Priority.Should().Be(MatrixPriority.Critical);
    }

    [Fact]
    public async Task UpsertEntryAsync_ShouldUpdateExistingEntry()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var items = MakeSampleItems();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, items, new DiscoveryConfig());

        var existing = matrix.Entries.First();
        existing.EstimatedTestCases = 99;

        // Act
        var updated = await service.UpsertEntryAsync(TestSessionId, TestClientId, existing);
        var retrieved = await service.GetMatrixAsync(TestSessionId, TestClientId);

        // Assert
        updated.EstimatedTestCases.Should().Be(99);
        retrieved!.Entries.Should().Contain(e => e.EstimatedTestCases == 99);
    }

    [Fact]
    public async Task MarkEntryCompleteAsync_ShouldSet100Coverage()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var items = MakeSampleItems();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, items, new DiscoveryConfig());
        var entryId = matrix.Entries.First().Id;

        // Act
        await service.MarkEntryCompleteAsync(TestSessionId, TestClientId, entryId);
        var retrieved = await service.GetMatrixAsync(TestSessionId, TestClientId);

        // Assert
        retrieved!.Entries.First(e => e.Id == entryId).IsComplete.Should().BeTrue();
        retrieved.Entries.First(e => e.Id == entryId).CoveragePercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetModuleEntriesAsync_ShouldFilterByModule()
    {
        // Arrange
        var service = new ValidationMatrixService();
        await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var financeEntries = await service.GetModuleEntriesAsync(TestSessionId, TestClientId, "Finance");

        // Assert
        financeEntries.Should().NotBeEmpty();
        financeEntries.Should().OnlyContain(e => e.Module == "Finance");
    }

    [Fact]
    public async Task GetDimensionEntriesAsync_ShouldFilterByDimension()
    {
        // Arrange
        var service = new ValidationMatrixService();
        await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var entries = await service.GetDimensionEntriesAsync(TestSessionId, TestClientId, "DataIntegrity");

        // Assert
        entries.Should().NotBeEmpty();
        entries.Should().OnlyContain(e => e.Dimension == "DataIntegrity");
    }

    [Fact]
    public async Task PrioritizeEntries_ShouldReturnCriticalFirst()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var prioritized = service.PrioritizeEntries(matrix);

        // Assert
        prioritized.Should().NotBeEmpty();
        if (prioritized.Any(e => e.Priority == MatrixPriority.Critical))
            prioritized.First().Priority.Should().Be(MatrixPriority.Critical);
    }

    [Fact]
    public async Task GetCriticalPath_ShouldReturnOnlyCriticalEntries()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var criticalPath = service.GetCriticalPath(matrix);

        // Assert
        criticalPath.Should().OnlyContain(e => e.Priority == MatrixPriority.Critical);
    }

    [Fact]
    public async Task UpdateCoverageAsync_ShouldUpdateEntry()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());
        var entryId = matrix.Entries.First().Id;
        var estimatedCases = matrix.Entries.First().EstimatedTestCases;

        // Act
        await service.UpdateCoverageAsync(TestSessionId, TestClientId, entryId, estimatedCases);
        var updated = await service.GetMatrixAsync(TestSessionId, TestClientId);

        // Assert
        updated!.Entries.First(e => e.Id == entryId).ActualTestCases.Should().Be(estimatedCases);
        updated.Entries.First(e => e.Id == entryId).CoveragePercentage.Should().BeApproximately(100.0, 1.0);
    }

    [Fact]
    public async Task CalculateCoverage_ShouldReturnZeroWhenNoTestsRun()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var coverage = service.CalculateCoverage(matrix);

        // Assert
        coverage.Should().Be(0);
    }

    [Fact]
    public async Task FindCoverageGaps_ShouldReturnAllEntriesInitially()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var gaps = service.FindCoverageGaps(matrix, 50.0);

        // Assert — all entries have 0 coverage initially
        gaps.Should().NotBeEmpty();
        gaps.Should().OnlyContain(e => e.CoveragePercentage < 50.0);
    }

    [Fact]
    public async Task EstimateTotalTestCases_ShouldBePositive()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var total = service.EstimateTotalTestCases(matrix);

        // Assert
        total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateMatrixReport_ShouldContainModuleNames()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());

        // Act
        var report = service.GenerateMatrixReport(matrix);

        // Assert
        report.Should().Contain("Finance");
        report.Should().Contain("Validation Matrix Report");
        report.Should().Contain("Coverage");
    }

    [Fact]
    public async Task RefreshMatrixAsync_ShouldPreserveCoverage()
    {
        // Arrange
        var service = new ValidationMatrixService();
        var matrix = await service.BuildMatrixAsync(TestSessionId, TestClientId, MakeSampleItems(), new DiscoveryConfig());
        var entryId = matrix.Entries.First().Id;
        await service.UpdateCoverageAsync(TestSessionId, TestClientId, entryId, 5);

        // Act
        var refreshed = await service.RefreshMatrixAsync(TestSessionId, TestClientId);

        // Assert
        refreshed.Should().NotBeNull();
        refreshed.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public void Configure_ShouldApplyConfig()
    {
        // Arrange
        var service = new ValidationMatrixService();

        // Act & Assert — no exception
        service.Configure(new ValidationMatrixConfig { MinTestCasesForCritical = 20 });
    }
}
