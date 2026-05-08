namespace ValidationOrchestrator.Models;

/// <summary>
/// A single executable test case targeting one business logic item or validation matrix entry
/// </summary>
public class TestCase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string SubModule { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;      // e.g. "FinancialAccuracy"
    public TestCaseType Type { get; set; }
    public TestCasePriority Priority { get; set; }
    public string Description { get; set; } = string.Empty;

    // What to execute
    public string? SqlQuery { get; set; }                      // SQL to run on both DBs
    public string? StoredProcedure { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public string? BusinessLogicItemId { get; set; }
    public string? MatrixEntryId { get; set; }

    // Expected outcome
    public SuccessCriteria SuccessCriteria { get; set; } = new();
    public Dictionary<string, object?> ExpectedOutput { get; set; } = new();
    public string? ExpectedBehavior { get; set; }

    // Execution context
    public string? SetupScript { get; set; }                   // SQL to run before test
    public string? TeardownScript { get; set; }                // SQL to run after test
    public List<string> Tags { get; set; } = new();
    public bool RequiresTransaction { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    // Metadata
    public string? GeneratedFrom { get; set; }                 // "AI" | "Pattern" | "Manual"
    public string? GenerationRationale { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? DataSetId { get; set; }                     // linked TestDataSet
}

/// <summary>
/// Category of test case
/// </summary>
public enum TestCaseType
{
    HappyPath,          // Normal expected flow
    EdgeCase,           // Boundary / corner case
    NegativeCase,       // Invalid input / error condition
    Performance,        // Throughput / response time
    Concurrent,         // Multi-user / race condition
    DataIntegrity,      // Referential integrity, precision
    Regression          // Known past bugs
}

/// <summary>
/// Priority level for test execution ordering
/// </summary>
public enum TestCasePriority
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3
}

/// <summary>
/// Defines what "pass" means for a test case
/// </summary>
public class SuccessCriteria
{
    /// <summary>Require exact match between legacy and Blazor results</summary>
    public bool RequireExactMatch { get; set; } = true;

    /// <summary>Tolerance for floating-point / decimal comparisons (0 = exact)</summary>
    public decimal NumericTolerance { get; set; } = 0m;

    /// <summary>Allowed performance degradation vs legacy baseline (0.20 = 20%)</summary>
    public double MaxPerformanceDegradation { get; set; } = 0.20;

    /// <summary>Max response time in ms (0 = no limit)</summary>
    public int MaxResponseTimeMs { get; set; } = 0;

    /// <summary>Fields to ignore in comparison (e.g. timestamp, auto-increment ID)</summary>
    public List<string> IgnoredFields { get; set; } = new();

    /// <summary>Fields that must match exactly regardless of NumericTolerance</summary>
    public List<string> ExactMatchFields { get; set; } = new();

    /// <summary>Human-readable acceptance statement</summary>
    public string? AcceptanceStatement { get; set; }

    /// <summary>Whether row count must match exactly</summary>
    public bool RequireRowCountMatch { get; set; } = true;

    /// <summary>Column-level tolerance overrides: column → tolerance</summary>
    public Dictionary<string, decimal> ColumnTolerances { get; set; } = new();
}

/// <summary>
/// A logical group of test cases forming a test scenario (e.g. "Invoice Creation Flow")
/// </summary>
public class TestScenario
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ScenarioType Type { get; set; }
    public List<TestCase> TestCases { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();    // other scenario IDs
    public bool RunInOrder { get; set; } = true;
    public string? BusinessProcess { get; set; }
    public TestCasePriority Priority { get; set; }
    public int TotalTestCases => TestCases.Count;
    public int CriticalTestCases => TestCases.Count(t => t.Priority == TestCasePriority.Critical);
}

/// <summary>
/// Type of test scenario
/// </summary>
public enum ScenarioType
{
    EndToEnd,           // Full business process flow
    Integration,        // Cross-module interaction
    Unit,               // Single logic item
    Performance,        // Load / stress
    Security            // Access control
}

/// <summary>
/// A synthetic dataset for test execution
/// </summary>
public class TestDataSet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestDataCategory Category { get; set; }

    /// <summary>Table → list of row dictionaries</summary>
    public Dictionary<string, List<Dictionary<string, object?>>> TableData { get; set; } = new();

    /// <summary>SQL INSERT scripts for legacy DB seeding</summary>
    public List<string> LegacySeedScripts { get; set; } = new();

    /// <summary>SQL INSERT scripts for Blazor DB seeding</summary>
    public List<string> BlazonSeedScripts { get; set; } = new();

    /// <summary>SQL cleanup scripts (teardown)</summary>
    public List<string> CleanupScripts { get; set; } = new();

    public int TotalRows => TableData.Values.Sum(rows => rows.Count);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? GeneratedBy { get; set; }    // "AI" | "Template" | "Manual"
}

/// <summary>
/// Category of test data
/// </summary>
public enum TestDataCategory
{
    Normal,             // Typical production-like data
    EdgeCase,           // Boundary values, extremes
    LargeVolume,        // Performance / stress testing
    MultiSite,          // Multi-location scenarios
    Historical,         // Aged data (fiscal year boundaries)
    Corrupt             // Intentionally malformed (negative testing)
}

/// <summary>
/// The complete Phase 2 deliverable — assembled test suite
/// </summary>
public class TestSuite
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";

    // Content
    public List<TestCase> TestCases { get; set; } = new();
    public List<TestScenario> Scenarios { get; set; } = new();
    public List<TestDataSet> DataSets { get; set; } = new();
    public List<SuccessCriteria> GlobalCriteria { get; set; } = new();

    // Metrics
    public int TotalTestCases => TestCases.Count;
    public int CriticalTestCases => TestCases.Count(t => t.Priority == TestCasePriority.Critical);
    public int TotalScenarios => Scenarios.Count;
    public int EstimatedExecutionMinutes { get; set; }

    // Coverage
    public double MatrixCoverage { get; set; }           // % of matrix entries covered
    public List<string> CoveredModules { get; set; } = new();
    public List<string> CoveredDimensions { get; set; } = new();
    public List<string> UncoveredAreas { get; set; } = new();

    // Status
    public TestSuiteStatus Status { get; set; } = TestSuiteStatus.Building;
    public string? ValidationNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAt { get; set; }
    public long GenerationDurationMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Lifecycle status of a test suite
/// </summary>
public enum TestSuiteStatus
{
    Building,       // Still being assembled
    Ready,          // Complete, ready for execution
    InExecution,    // Currently running
    Executed,       // Run complete, awaiting comparison
    Archived        // Historical
}

/// <summary>
/// Configuration for Phase 2 test generation
/// </summary>
public class TestGenerationConfig
{
    /// <summary>Generate happy-path test cases</summary>
    public bool GenerateHappyPaths { get; set; } = true;

    /// <summary>Generate edge case test cases</summary>
    public bool GenerateEdgeCases { get; set; } = true;

    /// <summary>Generate negative / error test cases</summary>
    public bool GenerateNegativeCases { get; set; } = true;

    /// <summary>Generate performance test cases</summary>
    public bool GeneratePerformanceCases { get; set; } = true;

    /// <summary>Generate concurrent user test cases</summary>
    public bool GenerateConcurrentCases { get; set; } = false;

    /// <summary>Target test cases per Critical logic item</summary>
    public int TestCasesPerCriticalItem { get; set; } = 10;

    /// <summary>Target test cases per High risk item</summary>
    public int TestCasesPerHighItem { get; set; } = 5;

    /// <summary>Target test cases per Medium risk item</summary>
    public int TestCasesPerMediumItem { get; set; } = 3;

    /// <summary>Use Claude AI for intelligent test generation</summary>
    public bool UseAiGeneration { get; set; } = true;

    /// <summary>Default numeric tolerance for financial fields (0 = exact)</summary>
    public decimal DefaultFinancialTolerance { get; set; } = 0m;

    /// <summary>Default numeric tolerance for non-financial numeric fields</summary>
    public decimal DefaultNumericTolerance { get; set; } = 0.001m;

    /// <summary>Max performance degradation allowed (0.20 = 20%)</summary>
    public double MaxPerformanceDegradation { get; set; } = 0.20;

    /// <summary>Row count for large-volume performance datasets</summary>
    public int LargeVolumeRowCount { get; set; } = 10000;

    /// <summary>Number of sites to simulate in multi-site datasets</summary>
    public int MultiSiteCount { get; set; } = 3;

    /// <summary>Fields always excluded from comparison</summary>
    public List<string> GlobalIgnoredFields { get; set; } = new()
    {
        "CreatedAt", "UpdatedAt", "ModifiedAt", "LastModified",
        "RowVersion", "Timestamp", "ETag"
    };
}
