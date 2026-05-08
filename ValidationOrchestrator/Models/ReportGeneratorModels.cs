namespace ValidationOrchestrator.Models;

/// <summary>
/// Complete validation report
/// </summary>
public class ValidationReport
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public ReportType Type { get; set; } = ReportType.Full;

    public ExecutiveSummary ExecutiveSummary { get; set; } = new();
    public List<string> Contents { get; set; } = new(); // Table of contents

    public DiscoverySummaryReport Discovery { get; set; } = new();
    public TestGenerationSummaryReport TestGeneration { get; set; } = new();
    public ExecutionSummaryReport Execution { get; set; } = new();
    public ComparisonSummaryReport Comparison { get; set; } = new();
    public DiscrepancySummaryReport Discrepancies { get; set; } = new();
    public RiskSummaryReport RiskAssessment { get; set; } = new();

    public List<ActionItem> ActionItems { get; set; } = new();
    public List<RecommendationItem> Recommendations { get; set; } = new();
    public List<string> Appendices { get; set; } = new();

    public string MarkdownContent { get; set; } = string.Empty;
    public long GenerationTimeMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Executive summary for stakeholders
/// </summary>
public class ExecutiveSummary
{
    public string Decision { get; set; } = string.Empty; // Go/No-Go
    public string Summary { get; set; } = string.Empty;
    public double OverallRiskScore { get; set; }
    public int CriticalFindingsCount { get; set; }
    public int TotalDiscrepancies { get; set; }
    public int BlockingIssuesCount { get; set; }
    public string PrimaryRisks { get; set; } = string.Empty;
    public string RecommendedActions { get; set; } = string.Empty;
    public int EstimatedDaysToReadiness { get; set; }
    public double ConfidenceLevel { get; set; }
    public List<string> KeyMetrics { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public DateTime? ProjectedReadyDate { get; set; }
    public List<string> GoBlockers { get; set; } = new();
}

/// <summary>
/// Discovery phase summary
/// </summary>
public class DiscoverySummaryReport
{
    public int TablesAnalyzed { get; set; }
    public int ColumnsAnalyzed { get; set; }
    public int StoredProceduresFound { get; set; }
    public int BusinessRulesIdentified { get; set; }
    public int DataQualityIssuesFound { get; set; }
    public List<string> KeyFindings { get; set; } = new();
    public Dictionary<string, int> ModuleBreakdown { get; set; } = new();
    public string NotablePatterns { get; set; } = string.Empty;
}

/// <summary>
/// Test generation summary
/// </summary>
public class TestGenerationSummaryReport
{
    public int TestsGenerated { get; set; }
    public int TestCategoriesCreated { get; set; }
    public List<TestCategoryBreakdown> TestsByCategory { get; set; } = new();
    public int DataValidationTests { get; set; }
    public int SchemaComparisonTests { get; set; }
    public int PerformanceTests { get; set; }
    public int BusinessLogicTests { get; set; }
    public string CoverageNotes { get; set; } = string.Empty;
}

/// <summary>
/// Test category breakdown
/// </summary>
public class TestCategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public double PassRate => Count > 0 ? (double)Passed / Count * 100 : 0;
}

/// <summary>
/// Execution phase summary
/// </summary>
public class ExecutionSummaryReport
{
    public int TotalTestsRun { get; set; }
    public int LegacyPassedTests { get; set; }
    public int BlazonPassedTests { get; set; }
    public int FailedTests { get; set; }
    public double OverallPassRate { get; set; }
    public DateTime ExecutionStartTime { get; set; }
    public DateTime ExecutionEndTime { get; set; }
    public long ExecutionDurationMs { get; set; }
    public List<string> FailedTestDetails { get; set; } = new();
    public Dictionary<string, int> ResultsByModule { get; set; } = new();
    public string PerformanceObservations { get; set; } = string.Empty;
}

/// <summary>
/// Comparison phase summary
/// </summary>
public class ComparisonSummaryReport
{
    public int TablesCompared { get; set; }
    public int TablesMatched { get; set; }
    public int TablesWithDifferences { get; set; }
    public double OverallMatchPercentage { get; set; }
    public int TotalRecordsCompared { get; set; }
    public int MatchedRecords { get; set; }
    public int MissingRecords { get; set; }
    public int ExtraRecords { get; set; }
    public int ModifiedRecords { get; set; }
    public Dictionary<string, TableComparisonMetrics> TableMetrics { get; set; } = new();
    public List<string> TablesWithHighestDifference { get; set; } = new();
}

/// <summary>
/// Per-table comparison metrics
/// </summary>
public class TableComparisonMetrics
{
    public string TableName { get; set; } = string.Empty;
    public int LegacyRecordCount { get; set; }
    public int BlazonRecordCount { get; set; }
    public int MatchedRecords { get; set; }
    public double MatchPercentage { get; set; }
    public int ModifiedRecords { get; set; }
    public int MissingRecords { get; set; }
    public int ExtraRecords { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Discrepancy summary report
/// </summary>
public class DiscrepancySummaryReport
{
    public int TotalDiscrepancies { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int BlockingIssuesCount { get; set; }
    public int ResolvableCount { get; set; }
    public double OverallImpact { get; set; }
    public List<string> TopCriticalIssues { get; set; } = new();
    public List<string> AffectedModules { get; set; } = new();
    public Dictionary<string, int> DiscrepanciesByCategory { get; set; } = new();
    public Dictionary<string, int> DiscrepanciesByModule { get; set; } = new();
    public List<string> CommonRootCauses { get; set; } = new();
}

/// <summary>
/// Risk assessment summary report
/// </summary>
public class RiskSummaryReport
{
    public string OverallDecision { get; set; } = string.Empty;
    public double OverallRiskScore { get; set; }
    public double OverallHealthScore { get; set; }
    public string Readiness { get; set; } = string.Empty;
    public int CriticalRiskItems { get; set; }
    public int BlockingIssuesCount { get; set; }
    public int GoBlockersCount { get; set; }
    public int WarningsCount { get; set; }
    public List<string> GoBlockers { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, double> ModuleRiskScores { get; set; } = new();
    public int EstimatedDaysToReadiness { get; set; }
    public double ConfidenceLevel { get; set; }
    public List<string> MitigationStrategies { get; set; } = new();
}

/// <summary>
/// Action item for remediation
/// </summary>
public class ActionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // Critical, High, Medium, Low
    public string Owner { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int EstimatedHours { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Blocked
    public List<string> Steps { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public double RiskReduction { get; set; }
    public bool IsPreLaunch { get; set; }
}

/// <summary>
/// Recommendation item for stakeholders
/// </summary>
public class RecommendationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Technical, Process, Organizational
    public string Impact { get; set; } = "Medium"; // High, Medium, Low
    public string Timeframe { get; set; } = string.Empty; // Immediate, Short-term, Long-term
    public List<string> Benefits { get; set; } = new();
    public string Implementation { get; set; } = string.Empty;
}

/// <summary>
/// Report type
/// </summary>
public enum ReportType
{
    ExecutiveSummary = 0,  // 1-2 pages for C-level
    Technical = 1,        // Detailed technical findings
    Detailed = 2,         // Complete with all appendices
    Full = 3               // Everything including historical data
}

/// <summary>
/// Report section
/// </summary>
public class ReportSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Level { get; set; } = 1; // Heading level (1-6)
    public List<ReportSection> Subsections { get; set; } = new();
}

/// <summary>
/// Report table for markdown
/// </summary>
public class ReportTable
{
    public string Title { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();

    public string ToMarkdown()
    {
        if (Headers.Count == 0 || Rows.Count == 0)
            return string.Empty;

        var lines = new List<string>();

        if (!string.IsNullOrEmpty(Title))
            lines.Add($"**{Title}**\n");

        // Header
        lines.Add("| " + string.Join(" | ", Headers) + " |");
        lines.Add("|" + string.Join("|", Headers.Select(_ => " --- ")) + "|");

        // Rows
        foreach (var row in Rows)
        {
            lines.Add("| " + string.Join(" | ", row) + " |");
        }

        return string.Join("\n", lines);
    }
}

/// <summary>
/// Report metrics table
/// </summary>
public class ReportMetrics
{
    public string MetricName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Good, Warning, Critical
    public string? Target { get; set; }
}

/// <summary>
/// Report configuration
/// </summary>
public class ReportGenerationConfig
{
    /// <summary>Include discovery findings</summary>
    public bool IncludeDiscovery { get; set; } = true;

    /// <summary>Include test generation details</summary>
    public bool IncludeTestGeneration { get; set; } = true;

    /// <summary>Include execution results</summary>
    public bool IncludeExecution { get; set; } = true;

    /// <summary>Include comparison metrics</summary>
    public bool IncludeComparison { get; set; } = true;

    /// <summary>Include discrepancies</summary>
    public bool IncludeDiscrepancies { get; set; } = true;

    /// <summary>Include risk assessment</summary>
    public bool IncludeRiskAssessment { get; set; } = true;

    /// <summary>Include action items</summary>
    public bool IncludeActionItems { get; set; } = true;

    /// <summary>Include recommendations</summary>
    public bool IncludeRecommendations { get; set; } = true;

    /// <summary>Maximum issues to list in report</summary>
    public int MaxIssuesToList { get; set; } = 20;

    /// <summary>Include detailed tables</summary>
    public bool IncludeDetailedTables { get; set; } = true;

    /// <summary>Generate table of contents</summary>
    public bool GenerateTOC { get; set; } = true;

    /// <summary>Include executive summary</summary>
    public bool IncludeExecutiveSummary { get; set; } = true;

    /// <summary>Include appendices</summary>
    public bool IncludeAppendices { get; set; } = true;

    /// <summary>Format for timestamps</summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
}
