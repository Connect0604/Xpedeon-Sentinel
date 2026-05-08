namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for generating validation reports in markdown format
/// Synthesizes all validation phases into comprehensive stakeholder-ready reports
/// </summary>
public interface IReportGenerator
{
    // Report Generation
    /// <summary>
    /// Generate complete validation report
    /// </summary>
    Task<ValidationReport> GenerateFullReportAsync(
        string sessionId,
        string clientId,
        DiscoveryFindings? discoveries = null,
        ExecutionResults? executionResults = null,
        DiscrepancyAnalysisResult? discrepancies = null,
        MigrationRiskAssessment? riskAssessment = null);

    /// <summary>
    /// Generate executive summary report (1-2 pages)
    /// </summary>
    Task<ValidationReport> GenerateExecutiveSummaryAsync(
        string sessionId,
        string clientId,
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Generate technical report with detailed findings
    /// </summary>
    Task<ValidationReport> GenerateTechnicalReportAsync(
        string sessionId,
        string clientId,
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    // Section Generation
    /// <summary>
    /// Generate executive summary section
    /// </summary>
    ExecutiveSummary GenerateExecutiveSummary(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Generate discovery section
    /// </summary>
    DiscoverySummaryReport GenerateDiscoverySection(DiscoveryFindings? discoveries);

    /// <summary>
    /// Generate execution summary section
    /// </summary>
    ExecutionSummaryReport GenerateExecutionSection(ExecutionResults? results);

    /// <summary>
    /// Generate comparison section
    /// </summary>
    ComparisonSummaryReport GenerateComparisonSection(List<TableComparisonResult>? comparisons);

    /// <summary>
    /// Generate discrepancies section
    /// </summary>
    DiscrepancySummaryReport GenerateDiscrepanciesSection(DiscrepancyAnalysisResult discrepancies);

    /// <summary>
    /// Generate risk assessment section
    /// </summary>
    RiskSummaryReport GenerateRiskSection(MigrationRiskAssessment riskAssessment);

    // Action and Recommendation Items
    /// <summary>
    /// Generate action items from discrepancies and risk assessment
    /// </summary>
    List<ActionItem> GenerateActionItems(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Generate recommendations for stakeholders
    /// </summary>
    List<RecommendationItem> GenerateRecommendations(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Create prioritized action plan
    /// </summary>
    List<ActionItem> CreateActionPlan(
        List<CriticalRiskItem> criticalItems,
        List<RemediationRecommendation> remediations);

    // Markdown Generation
    /// <summary>
    /// Convert report to markdown string
    /// </summary>
    string ConvertToMarkdown(ValidationReport report);

    /// <summary>
    /// Generate markdown for specific section
    /// </summary>
    string GenerateMarkdownSection(
        string title,
        int level,
        object content);

    /// <summary>
    /// Generate markdown table
    /// </summary>
    string GenerateMarkdownTable(ReportTable table);

    /// <summary>
    /// Generate table of contents
    /// </summary>
    string GenerateTableOfContents(List<string> sections);

    // Formatting
    /// <summary>
    /// Format status with emoji
    /// </summary>
    string FormatStatus(string status);

    /// <summary>
    /// Format severity level
    /// </summary>
    string FormatSeverity(string severity);

    /// <summary>
    /// Format percentage
    /// </summary>
    string FormatPercentage(double value);

    /// <summary>
    /// Format number with thousand separators
    /// </summary>
    string FormatNumber(int value);

    // Metrics and Statistics
    /// <summary>
    /// Extract key metrics for report
    /// </summary>
    List<ReportMetrics> ExtractKeyMetrics(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Generate summary statistics
    /// </summary>
    Dictionary<string, string> GenerateSummaryStats(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment);

    /// <summary>
    /// Generate charts/diagrams in markdown (using text-based representation)
    /// </summary>
    string GenerateDistributionChart(
        Dictionary<string, int> data,
        string title);

    // Export
    /// <summary>
    /// Export report to file
    /// </summary>
    Task<bool> ExportToFileAsync(
        ValidationReport report,
        string filePath);

    /// <summary>
    /// Export report to multiple formats
    /// </summary>
    Task<Dictionary<string, string>> ExportMultipleFormatsAsync(
        ValidationReport report,
        string outputDirectory);

    // Configuration
    /// <summary>
    /// Configure report generation
    /// </summary>
    void Configure(ReportGenerationConfig config);

    /// <summary>
    /// Get current configuration
    /// </summary>
    ReportGenerationConfig GetConfiguration();
}
