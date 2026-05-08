namespace ValidationOrchestrator.Models;

/// <summary>
/// Comprehensive discrepancy analysis result
/// </summary>
public class DiscrepancyAnalysisResult
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public List<DetailedDiscrepancy> Discrepancies { get; set; } = new();
    public List<DiscrepancyGroup> GroupedDiscrepancies { get; set; } = new();
    public DiscrepancySummary Summary { get; set; } = new();
    public List<string> AffectedModules { get; set; } = new();
    public Dictionary<string, ImpactAssessment> ModuleImpacts { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Detailed discrepancy with full context
/// </summary>
public class DetailedDiscrepancy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Module { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string? Column { get; set; }
    public string Category { get; set; } = string.Empty; // SchemaStructure, DataValue, Performance, etc.
    public string Description { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public DiscrepancySeverity Severity { get; set; }
    public int AffectedRecordCount { get; set; }
    public double ImpactPercentage { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public bool IsBlocker { get; set; } // Prevents migration
    public bool IsResolvable { get; set; }
    public string? ResolutionPath { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? RelatedDiscrepancyId { get; set; }
}

/// <summary>
/// Grouped related discrepancies
/// </summary>
public class DiscrepancyGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<DetailedDiscrepancy> Discrepancies { get; set; } = new();
    public DiscrepancySeverity MaxSeverity { get; set; }
    public string Module { get; set; } = string.Empty;
    public int TotalAffected => Discrepancies.Count;
    public int BlockerCount => Discrepancies.Count(d => d.IsBlocker);
    public double CombinedImpact { get; set; }
}

/// <summary>
/// Summary statistics for all discrepancies
/// </summary>
public class DiscrepancySummary
{
    public int TotalDiscrepancies { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int BlockerCount { get; set; }
    public int ResolvableCount { get; set; }
    public List<string> CriticalFindings { get; set; } = new();
    public Dictionary<string, int> DiscrepanciesByModule { get; set; } = new();
    public Dictionary<string, int> DiscrepanciesByCategory { get; set; } = new();
    public double OverallImpactPercentage { get; set; }
    public bool HasBlockingIssues => BlockerCount > 0;
}

/// <summary>
/// Impact assessment for a module
/// </summary>
public class ImpactAssessment
{
    public string ModuleName { get; set; } = string.Empty;
    public int DiscrepancyCount { get; set; }
    public double ImpactPercentage { get; set; }
    public DiscrepancySeverity MaxSeverity { get; set; }
    public bool IsBlocking { get; set; }
    public List<string> AffectedFeatures { get; set; } = new();
    public List<string> RequiredFixes { get; set; } = new();
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
}

/// <summary>
/// Discrepancy category
/// </summary>
public enum DiscrepancyCategory
{
    SchemaStructure,      // Missing tables/columns
    DataValue,            // Values don't match
    DataType,             // Type mismatches
    DataIntegrity,        // Foreign keys, constraints
    Performance,          // Query performance
    Completeness,         // Missing data
    Consistency,          // Inconsistent states
    BusinessLogic,        // Logic doesn't match
    Unknown
}

/// <summary>
/// Severity levels
/// </summary>
public enum DiscrepancySeverity
{
    Critical = 0,  // Must fix before launch
    High = 1,      // Should fix before launch
    Medium = 2,    // Can fix before launch
    Low = 3        // Can fix post-launch
}

/// <summary>
/// Detection pattern for finding discrepancies
/// </summary>
public class DiscrepancyPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Indicators { get; set; } = new();
    public DiscrepancyCategory Category { get; set; }
    public DiscrepancySeverity DefaultSeverity { get; set; }
    public List<string> CommonRootCauses { get; set; } = new();
    public List<string> SuggestedFixes { get; set; } = new();
}

/// <summary>
/// Configuration for discrepancy detection
/// </summary>
public class DiscrepancyDetectionConfig
{
    /// <summary>Minimum impact percentage to report</summary>
    public double MinimumImpactPercentage { get; set; } = 0.1;

    /// <summary>Match percentage threshold for "acceptable"</summary>
    public double AcceptableMatchPercentage { get; set; } = 95.0;

    /// <summary>Enable context-aware analysis</summary>
    public bool EnableContextualAnalysis { get; set; } = true;

    /// <summary>Enable pattern matching for root causes</summary>
    public bool EnablePatternMatching { get; set; } = true;

    /// <summary>Enable grouping of related discrepancies</summary>
    public bool EnableGrouping { get; set; } = true;

    /// <summary>Custom patterns for detection</summary>
    public List<DiscrepancyPattern> CustomPatterns { get; set; } = new();

    /// <summary>Modules to focus on</summary>
    public List<string> FocusModules { get; set; } = new();

    /// <summary>Modules to exclude from analysis</summary>
    public List<string> ExcludedModules { get; set; } = new();
}

/// <summary>
/// Evidence of a discrepancy
/// </summary>
public class DiscrepancyEvidence
{
    public string Type { get; set; } = string.Empty; // Example, Metric, Log, etc.
    public string Description { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Remediation recommendation
/// </summary>
public class RemediationRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public string? RequiredRole { get; set; }
    public int EstimatedEffortHours { get; set; }
    public string? SpecialConsiderations { get; set; }
    public bool IsAutomatic { get; set; }
    public double SuccessProbability { get; set; }
}

/// <summary>
/// Discrepancy analysis metrics
/// </summary>
public class DiscrepancyMetrics
{
    public int TotalDiscrepancies { get; set; }
    public int SeverityDistribution_Critical { get; set; }
    public int SeverityDistribution_High { get; set; }
    public int SeverityDistribution_Medium { get; set; }
    public int SeverityDistribution_Low { get; set; }
    public double AverageSeverityScore { get; set; }
    public double TotalImpactPercentage { get; set; }
    public int BlockingIssueCount { get; set; }
    public int ResolvableCount { get; set; }
    public Dictionary<string, int> IssuesByModule { get; set; } = new();
    public Dictionary<string, int> IssuesByCategory { get; set; } = new();
}
