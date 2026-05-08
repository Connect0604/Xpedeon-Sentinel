namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Generates and manages synthetic test datasets for Phase 2 execution.
/// Produces production-like data, edge-case datasets, large-volume sets for performance,
/// and multi-site datasets covering the Xpedeon construction ERP domain.
/// </summary>
public interface ITestDataService
{
    // Dataset Generation
    /// <summary>
    /// Generate a standard production-like dataset for a module
    /// </summary>
    Task<TestDataSet> GenerateModuleDataAsync(
        string module,
        string sessionId,
        string clientId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an edge-case dataset (boundary values, fiscal year boundaries, etc.)
    /// </summary>
    Task<TestDataSet> GenerateEdgeCaseDataAsync(
        string module,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a large-volume dataset for performance testing
    /// </summary>
    Task<TestDataSet> GenerateLargeVolumeDataAsync(
        string module,
        int rowCount,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a multi-site dataset spanning multiple construction sites / branches
    /// </summary>
    Task<TestDataSet> GenerateMultiSiteDataAsync(
        int siteCount,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate datasets for all modules and categories needed by a test suite
    /// </summary>
    Task<List<TestDataSet>> GenerateAllDatasetsAsync(
        TestSuite suite,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    // Domain-specific Generators
    /// <summary>Generate Finance/Accounting test data (invoices, payments, tax records)</summary>
    TestDataSet GenerateFinanceData(string sessionId, TestGenerationConfig config);

    /// <summary>Generate Inventory test data (stock, materials, movements)</summary>
    TestDataSet GenerateInventoryData(string sessionId, TestGenerationConfig config);

    /// <summary>Generate Procurement test data (POs, vendors, approvals)</summary>
    TestDataSet GenerateProcurementData(string sessionId, TestGenerationConfig config);

    /// <summary>Generate HR/Payroll test data (employees, salaries, deductions)</summary>
    TestDataSet GenerateHrPayrollData(string sessionId, TestGenerationConfig config);

    /// <summary>Generate Plant Management test data (equipment, hire, maintenance)</summary>
    TestDataSet GeneratePlantData(string sessionId, TestGenerationConfig config);

    // Seed Script Generation
    /// <summary>
    /// Build SQL INSERT scripts for seeding a dataset into the legacy database
    /// </summary>
    List<string> BuildLegacySeedScripts(TestDataSet dataSet);

    /// <summary>
    /// Build SQL INSERT scripts for seeding a dataset into the Blazor database
    /// </summary>
    List<string> BuildBlazonSeedScripts(TestDataSet dataSet);

    /// <summary>
    /// Build SQL DELETE/TRUNCATE cleanup scripts
    /// </summary>
    List<string> BuildCleanupScripts(TestDataSet dataSet);

    // Management
    /// <summary>Store a dataset for a session</summary>
    Task StoreDataSetAsync(string sessionId, string clientId, TestDataSet dataSet);

    /// <summary>Retrieve all datasets for a session</summary>
    Task<List<TestDataSet>> GetDataSetsAsync(string sessionId, string clientId);

    /// <summary>Link a dataset to a test case</summary>
    Task LinkDataSetToTestCaseAsync(string sessionId, string dataSetId, string testCaseId);

    // Configuration
    /// <summary>Configure the data service</summary>
    void Configure(TestGenerationConfig config);
}
