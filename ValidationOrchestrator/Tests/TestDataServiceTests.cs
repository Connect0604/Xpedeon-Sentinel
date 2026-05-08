namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for TestDataService (Phase 2 — synthetic data generation)
/// </summary>
public class TestDataServiceTests
{
    private const string TestSessionId = "test-session-tds";
    private const string TestClientId = "test-client-tds";

    private static TestGenerationConfig DefaultConfig() => new()
    {
        LargeVolumeRowCount = 100,  // small count for tests
        MultiSiteCount = 2
    };

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var service = new TestDataService();
        service.Should().NotBeNull();
    }

    [Fact]
    public void GenerateFinanceData_ShouldContainInvoicesAndPayments()
    {
        var service = new TestDataService();

        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());

        ds.Should().NotBeNull();
        ds.Module.Should().Be("Finance");
        ds.Category.Should().Be(TestDataCategory.Normal);
        ds.TableData.Should().ContainKey("Invoices");
        ds.TableData.Should().ContainKey("Payments");
        ds.TableData["Invoices"].Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateFinanceData_InvoiceShouldHaveTaxAmounts()
    {
        var service = new TestDataService();

        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());
        var invoice = ds.TableData["Invoices"].First();

        invoice.Should().ContainKey("Amount");
        invoice.Should().ContainKey("TaxAmount");
        invoice.Should().ContainKey("TaxRate");
        invoice["Amount"].Should().NotBeNull();
    }

    [Fact]
    public void GenerateInventoryData_ShouldContainStockAndMovements()
    {
        var service = new TestDataService();

        var ds = service.GenerateInventoryData(TestSessionId, DefaultConfig());

        ds.Module.Should().Be("Inventory");
        ds.TableData.Should().ContainKey("StockLevels");
        ds.TableData.Should().ContainKey("MaterialMovements");
        ds.TableData["StockLevels"].Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateProcurementData_ShouldContainPurchaseOrders()
    {
        var service = new TestDataService();

        var ds = service.GenerateProcurementData(TestSessionId, DefaultConfig());

        ds.Module.Should().Be("Procurement");
        ds.TableData.Should().ContainKey("PurchaseOrders");
        ds.TableData["PurchaseOrders"].First().Should().ContainKey("ApprovalStatus");
    }

    [Fact]
    public void GenerateHrPayrollData_ShouldContainEmployeesAndPayroll()
    {
        var service = new TestDataService();

        var ds = service.GenerateHrPayrollData(TestSessionId, DefaultConfig());

        ds.Module.Should().Be("HR");
        ds.TableData.Should().ContainKey("Employees");
        ds.TableData.Should().ContainKey("PayrollRuns");
    }

    [Fact]
    public void GeneratePlantData_ShouldContainEquipmentAndHire()
    {
        var service = new TestDataService();

        var ds = service.GeneratePlantData(TestSessionId, DefaultConfig());

        ds.Module.Should().Be("PlantManagement");
        ds.TableData.Should().ContainKey("Equipment");
        ds.TableData.Should().ContainKey("HireRecords");
        ds.TableData["Equipment"].First().Should().ContainKey("HireRatePerDay");
    }

    [Fact]
    public async Task GenerateEdgeCaseDataAsync_ShouldContainBoundaryValues()
    {
        var service = new TestDataService();

        var ds = await service.GenerateEdgeCaseDataAsync("Finance", TestSessionId, DefaultConfig());

        ds.Category.Should().Be(TestDataCategory.EdgeCase);
        ds.TableData.Should().ContainKey("Invoices");
        ds.TableData["Invoices"].Should().Contain(r =>
            r.ContainsKey("Description") && r["Description"]!.ToString()!.Contains("Zero"));
    }

    [Fact]
    public async Task GenerateEdgeCaseDataAsync_ShouldContainDateBoundaries()
    {
        var service = new TestDataService();

        var ds = await service.GenerateEdgeCaseDataAsync("Finance", TestSessionId, DefaultConfig());

        ds.TableData.Should().ContainKey("DateBoundaries");
        ds.TableData["DateBoundaries"].Should().Contain(r =>
            r.ContainsKey("Description") && r["Description"]!.ToString()!.Contains("Fiscal year start"));
    }

    [Fact]
    public async Task GenerateLargeVolumeDataAsync_ShouldGenerateCorrectRowCount()
    {
        var service = new TestDataService();

        var ds = await service.GenerateLargeVolumeDataAsync("Finance", 100, TestSessionId, DefaultConfig());

        ds.Category.Should().Be(TestDataCategory.LargeVolume);
        ds.TotalRows.Should().Be(100);
    }

    [Fact]
    public async Task GenerateLargeVolumeDataAsync_ShouldHaveConsistentAmounts()
    {
        var service = new TestDataService();

        var ds = await service.GenerateLargeVolumeDataAsync("Finance", 50, TestSessionId, DefaultConfig());
        var rows = ds.TableData.Values.First();

        rows.Should().OnlyContain(r => r.ContainsKey("Amount") && r.ContainsKey("TaxAmount"));
    }

    [Fact]
    public async Task GenerateMultiSiteDataAsync_ShouldContainCorrectSiteCount()
    {
        var service = new TestDataService();

        var ds = await service.GenerateMultiSiteDataAsync(3, TestSessionId, DefaultConfig());

        ds.Category.Should().Be(TestDataCategory.MultiSite);
        ds.TableData.Should().ContainKey("Sites");
        ds.TableData["Sites"].Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateMultiSiteDataAsync_ShouldContainCrossSiteTransfers()
    {
        var service = new TestDataService();

        var ds = await service.GenerateMultiSiteDataAsync(2, TestSessionId, DefaultConfig());

        ds.TableData.Should().ContainKey("CrossSiteTransfers");
        ds.TableData["CrossSiteTransfers"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateModuleDataAsync_Finance_ShouldDelegateToFinanceGenerator()
    {
        var service = new TestDataService();

        var ds = await service.GenerateModuleDataAsync("Finance", TestSessionId, TestClientId, DefaultConfig());

        ds.Module.Should().Be("Finance");
        ds.TableData.Should().ContainKey("Invoices");
    }

    [Fact]
    public async Task StoreAndRetrieveDataSets_ShouldRoundTrip()
    {
        var service = new TestDataService();
        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());

        await service.StoreDataSetAsync(TestSessionId, TestClientId, ds);
        var retrieved = await service.GetDataSetsAsync(TestSessionId, TestClientId);

        retrieved.Should().HaveCount(1);
        retrieved.First().Module.Should().Be("Finance");
    }

    [Fact]
    public void BuildLegacySeedScripts_ShouldProduceInsertStatements()
    {
        var service = new TestDataService();
        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());

        var scripts = service.BuildLegacySeedScripts(ds);

        scripts.Should().NotBeEmpty();
        scripts.Should().Contain(s => s.Contains("INSERT INTO"));
    }

    [Fact]
    public void BuildBlazonSeedScripts_ShouldDifferFromLegacy()
    {
        var service = new TestDataService();
        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());

        var legacyScripts = service.BuildLegacySeedScripts(ds);
        var blazonScripts = service.BuildBlazonSeedScripts(ds);

        // Blazor scripts omit [dbo] schema prefix
        legacyScripts.Should().Contain(s => s.Contains("[dbo]"));
        blazonScripts.Should().NotContain(s => s.Contains("[dbo]"));
    }

    [Fact]
    public void BuildCleanupScripts_ShouldProduceDeleteStatements()
    {
        var service = new TestDataService();
        var ds = service.GenerateFinanceData(TestSessionId, DefaultConfig());

        var scripts = service.BuildCleanupScripts(ds);

        scripts.Should().NotBeEmpty();
        scripts.Should().Contain(s => s.Contains("DELETE FROM"));
    }

    [Fact]
    public async Task LinkDataSetToTestCase_ShouldSucceed()
    {
        var service = new TestDataService();

        var act = async () => await service.LinkDataSetToTestCaseAsync(TestSessionId, "ds-001", "tc-001");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Configure_ShouldNotThrow()
    {
        var service = new TestDataService();
        var act = () => service.Configure(new TestGenerationConfig { LargeVolumeRowCount = 500 });
        act.Should().NotThrow();
    }
}
