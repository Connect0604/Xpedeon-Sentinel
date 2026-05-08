namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Orchestrates Phase 2 — Test Case Generation.
/// Assembles the complete test suite from generated test cases, datasets, and success criteria,
/// then persists it to ValidationMemoryService as the phase deliverable.
/// </summary>
public interface ITestSuiteService
{
    // Phase Execution
    /// <summary>
    /// Run the full Phase 2 pipeline: generate cases, data, criteria, and assemble the suite
    /// </summary>
    Task<TestSuite> RunTestGenerationAsync(
        string sessionId,
        string clientId,
        ValidationMatrix matrix,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    // Suite Management
    /// <summary>Store the assembled test suite for a session</summary>
    Task StoreSuiteAsync(string sessionId, string clientId, TestSuite suite);

    /// <summary>Retrieve the current test suite for a session</summary>
    Task<TestSuite?> GetSuiteAsync(string sessionId, string clientId);

    /// <summary>Add test cases to an existing suite</summary>
    Task AddTestCasesAsync(string sessionId, string clientId, List<TestCase> cases);

    /// <summary>Add datasets to an existing suite</summary>
    Task AddDataSetsAsync(string sessionId, string clientId, List<TestDataSet> dataSets);

    /// <summary>Mark the suite as finalized (ready for Phase 3 execution)</summary>
    Task<TestSuite> FinalizeSuiteAsync(string sessionId, string clientId);

    // Coverage & Metrics
    /// <summary>Calculate what percentage of the validation matrix is covered by the suite</summary>
    double CalculateMatrixCoverage(TestSuite suite, ValidationMatrix matrix);

    /// <summary>Identify matrix entries with no test cases</summary>
    List<ValidationMatrixEntry> FindUncoveredEntries(TestSuite suite, ValidationMatrix matrix);

    /// <summary>Estimate total execution time in minutes</summary>
    int EstimateExecutionTime(TestSuite suite);

    // Reporting
    /// <summary>Generate a markdown summary report for the test suite</summary>
    string GenerateSuiteReport(TestSuite suite);

    /// <summary>Return current generation progress (0-100)</summary>
    Task<double> GetProgressAsync(string sessionId, string clientId);

    // Configuration
    /// <summary>Configure the service</summary>
    void Configure(TestSuiteServiceConfig config);
}

/// <summary>
/// Configuration for the test suite service
/// </summary>
public class TestSuiteServiceConfig
{
    /// <summary>Minimum matrix coverage % before suite is considered ready</summary>
    public double MinimumCoverageThreshold { get; set; } = 70.0;

    /// <summary>Whether to auto-finalize when coverage threshold is met</summary>
    public bool AutoFinalize { get; set; } = false;

    /// <summary>Compress the suite artifacts when storing</summary>
    public bool CompressSuite { get; set; } = true;
}
