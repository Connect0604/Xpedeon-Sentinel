namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Builds and manages the validation matrix that maps every ERP module
/// against each validation dimension, prioritizes entries by risk, and
/// tracks test coverage across the pipeline.
/// </summary>
public interface IValidationMatrixService
{
    // Matrix Construction
    /// <summary>
    /// Build a full validation matrix from discovered business logic items
    /// </summary>
    Task<ValidationMatrix> BuildMatrixAsync(
        string sessionId,
        string clientId,
        List<BusinessLogicItem> items,
        DiscoveryConfig config);

    /// <summary>
    /// Rebuild matrix after new findings (expert interviews, additional files)
    /// </summary>
    Task<ValidationMatrix> RefreshMatrixAsync(
        string sessionId,
        string clientId);

    /// <summary>
    /// Retrieve the current matrix for a session
    /// </summary>
    Task<ValidationMatrix?> GetMatrixAsync(string sessionId, string clientId);

    // Entry Management
    /// <summary>
    /// Add or update a single module × dimension entry
    /// </summary>
    Task<ValidationMatrixEntry> UpsertEntryAsync(
        string sessionId,
        string clientId,
        ValidationMatrixEntry entry);

    /// <summary>
    /// Mark an entry as complete (all test cases generated and executed)
    /// </summary>
    Task MarkEntryCompleteAsync(
        string sessionId,
        string clientId,
        string entryId);

    /// <summary>
    /// Get all entries for a specific module
    /// </summary>
    Task<List<ValidationMatrixEntry>> GetModuleEntriesAsync(
        string sessionId,
        string clientId,
        string module);

    /// <summary>
    /// Get all entries for a specific dimension
    /// </summary>
    Task<List<ValidationMatrixEntry>> GetDimensionEntriesAsync(
        string sessionId,
        string clientId,
        string dimension);

    // Prioritization
    /// <summary>
    /// Return entries ordered by priority and estimated effort
    /// </summary>
    List<ValidationMatrixEntry> PrioritizeEntries(ValidationMatrix matrix);

    /// <summary>
    /// Identify the minimum set of entries that covers all Critical risks
    /// </summary>
    List<ValidationMatrixEntry> GetCriticalPath(ValidationMatrix matrix);

    /// <summary>
    /// Estimate total test cases needed across all entries
    /// </summary>
    int EstimateTotalTestCases(ValidationMatrix matrix);

    // Coverage Tracking
    /// <summary>
    /// Update test case coverage for an entry
    /// </summary>
    Task UpdateCoverageAsync(
        string sessionId,
        string clientId,
        string entryId,
        int actualTestCases);

    /// <summary>
    /// Calculate overall matrix coverage percentage (0-100)
    /// </summary>
    double CalculateCoverage(ValidationMatrix matrix);

    /// <summary>
    /// Find entries with zero or low test coverage
    /// </summary>
    List<ValidationMatrixEntry> FindCoverageGaps(ValidationMatrix matrix, double threshold = 50.0);

    // Reporting
    /// <summary>
    /// Generate a markdown summary of the validation matrix
    /// </summary>
    string GenerateMatrixReport(ValidationMatrix matrix);

    // Configuration
    /// <summary>Configure the matrix service</summary>
    void Configure(ValidationMatrixConfig config);
}

/// <summary>
/// Configuration for the validation matrix service
/// </summary>
public class ValidationMatrixConfig
{
    /// <summary>Dimensions to include in every matrix</summary>
    public List<string> Dimensions { get; set; } = new()
    {
        "FinancialAccuracy",
        "InventoryConsistency",
        "WorkflowIntegrity",
        "DataIntegrity",
        "SecurityAccessControl",
        "Performance",
        "Reporting",
        "IntegrationPoints",
        "BusinessRules",
        "UserBehavior"
    };

    /// <summary>Minimum test cases per Critical entry</summary>
    public int MinTestCasesForCritical { get; set; } = 10;

    /// <summary>Minimum test cases per High entry</summary>
    public int MinTestCasesForHigh { get; set; } = 5;

    /// <summary>Minimum test cases per Medium entry</summary>
    public int MinTestCasesForMedium { get; set; } = 3;

    /// <summary>Minimum test cases per Low entry</summary>
    public int MinTestCasesForLow { get; set; } = 1;

    /// <summary>Coverage percentage considered acceptable per entry</summary>
    public double AcceptableCoverageThreshold { get; set; } = 80.0;

    /// <summary>Module → default dimension priority overrides</summary>
    public Dictionary<string, List<string>> ModulePriorityDimensions { get; set; } = new()
    {
        { "Finance", new() { "FinancialAccuracy", "BusinessRules", "DataIntegrity" } },
        { "Accounting", new() { "FinancialAccuracy", "Reporting", "DataIntegrity" } },
        { "Inventory", new() { "InventoryConsistency", "DataIntegrity", "BusinessRules" } },
        { "Procurement", new() { "WorkflowIntegrity", "BusinessRules", "DataIntegrity" } },
        { "HR", new() { "DataIntegrity", "BusinessRules", "SecurityAccessControl" } },
        { "Payroll", new() { "FinancialAccuracy", "DataIntegrity", "BusinessRules" } }
    };
}
