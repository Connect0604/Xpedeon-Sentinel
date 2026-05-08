namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for detecting and analyzing discrepancies between legacy and Blazor systems
/// Synthesizes findings from schema and table comparisons into actionable insights
/// </summary>
public interface IDiscrepancyDetector
{
    // Core Analysis
    /// <summary>
    /// Detect discrepancies from table comparison results
    /// </summary>
    Task<DiscrepancyAnalysisResult> DetectFromTableComparisonAsync(
        TableComparisonResult comparisonResult,
        string sessionId,
        string clientId);

    /// <summary>
    /// Detect discrepancies from schema analysis results
    /// </summary>
    Task<DiscrepancyAnalysisResult> DetectFromSchemaAnalysisAsync(
        SchemaAnalysisResult schemaAnalysis,
        string sessionId,
        string clientId);

    /// <summary>
    /// Analyze multiple comparison results and produce comprehensive discrepancy report
    /// </summary>
    Task<DiscrepancyAnalysisResult> AnalyzeAllComparisonsAsync(
        List<TableComparisonResult> tableComparisons,
        SchemaAnalysisResult schemaAnalysis,
        string sessionId,
        string clientId);

    // Discrepancy Creation
    /// <summary>
    /// Create a discrepancy from table comparison data
    /// </summary>
    DetailedDiscrepancy CreateDataDiscrepancy(
        string module,
        string table,
        string category,
        string description,
        DiscrepancySeverity severity,
        int affectedRecordCount,
        double impactPercentage);

    /// <summary>
    /// Create a discrepancy from schema analysis data
    /// </summary>
    DetailedDiscrepancy CreateSchemaDiscrepancy(
        string module,
        string table,
        string? column,
        string description,
        DiscrepancySeverity severity,
        bool isBlocker = false);

    // Grouping
    /// <summary>
    /// Group related discrepancies by module and pattern
    /// </summary>
    List<DiscrepancyGroup> GroupDiscrepancies(
        List<DetailedDiscrepancy> discrepancies);

    /// <summary>
    /// Find discrepancies with related root causes
    /// </summary>
    List<DetailedDiscrepancy> FindRelatedDiscrepancies(
        DetailedDiscrepancy discrepancy,
        List<DetailedDiscrepancy> allDiscrepancies);

    // Severity Assessment
    /// <summary>
    /// Determine severity based on impact percentage and affected records
    /// </summary>
    DiscrepancySeverity DetermineSeverity(
        double impactPercentage,
        int affectedRecordCount);

    /// <summary>
    /// Calculate severity score for prioritization
    /// </summary>
    double CalculateSeverityScore(DetailedDiscrepancy discrepancy);

    // Impact Analysis
    /// <summary>
    /// Assess module-level impact
    /// </summary>
    Dictionary<string, ImpactAssessment> AssessModuleImpacts(
        List<DetailedDiscrepancy> discrepancies);

    /// <summary>
    /// Calculate overall impact percentage
    /// </summary>
    double CalculateOverallImpact(List<DetailedDiscrepancy> discrepancies);

    /// <summary>
    /// Identify blocking issues that prevent migration
    /// </summary>
    List<DetailedDiscrepancy> FindBlockingIssues(
        List<DetailedDiscrepancy> discrepancies);

    // Summary Generation
    /// <summary>
    /// Generate summary statistics for discrepancies
    /// </summary>
    DiscrepancySummary GenerateSummary(List<DetailedDiscrepancy> discrepancies);

    /// <summary>
    /// Generate metrics for analysis reporting
    /// </summary>
    DiscrepancyMetrics GenerateMetrics(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary);

    // Root Cause Analysis
    /// <summary>
    /// Identify most likely root cause for discrepancy
    /// </summary>
    string IdentifyRootCause(
        DetailedDiscrepancy discrepancy,
        SchemaAnalysisResult? schemaAnalysis = null);

    /// <summary>
    /// Generate remediation recommendations
    /// </summary>
    List<RemediationRecommendation> GenerateRecommendations(
        DetailedDiscrepancy discrepancy);

    /// <summary>
    /// Analyze patterns in discrepancies to find common issues
    /// </summary>
    List<DiscrepancyPattern> AnalyzePatterns(
        List<DetailedDiscrepancy> discrepancies);

    // Evidence Collection
    /// <summary>
    /// Collect evidence for a discrepancy
    /// </summary>
    List<string> CollectEvidence(
        DetailedDiscrepancy discrepancy,
        TableComparisonResult? comparisonResult = null);

    // Configuration
    /// <summary>
    /// Configure detection parameters
    /// </summary>
    void Configure(DiscrepancyDetectionConfig config);

    /// <summary>
    /// Enable pattern-based detection
    /// </summary>
    void EnablePatternMatching(List<DiscrepancyPattern> patterns);

    /// <summary>
    /// Add custom pattern for detection
    /// </summary>
    void AddCustomPattern(DiscrepancyPattern pattern);
}
