namespace ValidationOrchestrator.Models;

/// <summary>
/// Overall migration risk assessment result
/// </summary>
public class MigrationRiskAssessment
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public MigrationReadiness OverallReadiness { get; set; } = MigrationReadiness.NotReady;
    public double OverallRiskScore { get; set; } // 0-100, higher = worse
    public double OverallHealthScore { get; set; } // 0-100, higher = better
    public MigrationRecommendation Recommendation { get; set; } = new();
    public List<ModuleRiskAssessment> ModuleRisks { get; set; } = new();
    public List<CriticalRiskItem> CriticalItems { get; set; } = new();
    public List<RiskMitigationStrategy> MitigationStrategies { get; set; } = new();
    public RiskTrendAnalysis TrendAnalysis { get; set; } = new();
    public List<string> GoBlockers { get; set; } = new(); // Issues blocking Go decision
    public List<string> Warnings { get; set; } = new(); // Non-blocking concerns
    public long CalculationTimeMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Module-level risk assessment
/// </summary>
public class ModuleRiskAssessment
{
    public string ModuleName { get; set; } = string.Empty;
    public double RiskScore { get; set; } // 0-100
    public double HealthScore { get; set; } // 0-100
    public int DiscrepancyCount { get; set; }
    public int BlockingIssueCount { get; set; }
    public int ResolvableIssueCount { get; set; }
    public MigrationReadiness Readiness { get; set; }
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
    public List<string> CriticalPaths { get; set; } = new(); // Critical features/flows
    public List<string> Dependencies { get; set; } = new(); // Module dependencies
    public double DataCompleteness { get; set; } // Percentage of data migrated
    public double DataAccuracy { get; set; } // Match percentage
    public double FunctionalCoverage { get; set; } // Logic migrated percentage
    public int EstimatedFixHours { get; set; }
    public DateTime EstimatedReadyDate { get; set; }
}

/// <summary>
/// Critical issue requiring immediate attention
/// </summary>
public class CriticalRiskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Table { get; set; }
    public double RiskContribution { get; set; } // Percentage of overall risk
    public int AffectedRecordCount { get; set; }
    public double ImpactPercentage { get; set; }
    public int EstimatedFixHours { get; set; }
    public bool CanBeFixedPostLaunch { get; set; }
    public string? ResolutionPath { get; set; }
    public List<string> DependentOn { get; set; } = new(); // Other items this depends on
    public List<string> Enables { get; set; } = new(); // Other items this enables
    public RiskPriority Priority { get; set; } = RiskPriority.High;
}

/// <summary>
/// Risk mitigation strategy
/// </summary>
public class RiskMitigationStrategy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> TargetsRiskItems { get; set; } = new(); // IDs of items it addresses
    public List<string> Actions { get; set; } = new(); // Specific mitigation actions
    public int EstimatedHours { get; set; }
    public string Owner { get; set; } = string.Empty; // Role responsible
    public DateTime TargetCompletionDate { get; set; }
    public double RiskReduction { get; set; } // Percentage of risk reduced
    public MitigationPriority Priority { get; set; } = MitigationPriority.High;
    public bool IsPreLaunch { get; set; } = true; // Must complete before launch
    public string? ImplementationPath { get; set; }
}

/// <summary>
/// Risk trend over time
/// </summary>
public class RiskTrendAnalysis
{
    public List<RiskSnapshot> Snapshots { get; set; } = new();
    public double TrendDirection { get; set; } // -1 to 1, negative = improving
    public DateTime? ProjectedGoReadyDate { get; set; }
    public double ImprovementRate { get; set; } // % improvement per day
    public string Trend { get; set; } = "Stable"; // Improving, Stable, Deteriorating
    public List<string> TrendObservations { get; set; } = new();
}

/// <summary>
/// Risk snapshot at point in time
/// </summary>
public class RiskSnapshot
{
    public DateTime Timestamp { get; set; }
    public double OverallRiskScore { get; set; }
    public double OverallHealthScore { get; set; }
    public int TotalCriticalItems { get; set; }
    public int TotalIssues { get; set; }
    public MigrationReadiness Readiness { get; set; }
}

/// <summary>
/// Migration recommendation
/// </summary>
public class MigrationRecommendation
{
    public MigrationDecision Decision { get; set; } = MigrationDecision.NoGo;
    public string Summary { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public List<string> RequiredActions { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public List<string> CanBePostLaunch { get; set; } = new();
    public int MinimumDaysToReady { get; set; }
    public double ConfidenceLevel { get; set; } // 0-1
    public string? AlternativeApproach { get; set; }
}

/// <summary>
/// Risk component for detailed scoring
/// </summary>
public class RiskComponent
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; } // 0-100
    public double Weight { get; set; } // 0-1
    public string Category { get; set; } = string.Empty; // Data, Schema, Logic, Performance
    public List<string> ContributingFactors { get; set; } = new();
    public string? Recommendation { get; set; }
}

/// <summary>
/// Risk configuration
/// </summary>
public class RiskCalculationConfig
{
    /// <summary>Risk score threshold for Critical readiness</summary>
    public double CriticalThreshold { get; set; } = 25.0;

    /// <summary>Risk score threshold for High readiness</summary>
    public double HighThreshold { get; set; } = 50.0;

    /// <summary>Risk score threshold for Medium readiness</summary>
    public double MediumThreshold { get; set; } = 75.0;

    /// <summary>Weight for data completeness in risk calculation</summary>
    public double DataCompletenessWeight { get; set; } = 0.25;

    /// <summary>Weight for data accuracy in risk calculation</summary>
    public double DataAccuracyWeight { get; set; } = 0.25;

    /// <summary>Weight for schema compatibility in risk calculation</summary>
    public double SchemaCompatibilityWeight { get; set; } = 0.20;

    /// <summary>Weight for functional coverage in risk calculation</summary>
    public double FunctionalCoverageWeight { get; set; } = 0.20;

    /// <summary>Weight for blocking issues in risk calculation</summary>
    public double BlockingIssuesWeight { get; set; } = 0.10;

    /// <summary>Minimum acceptable data match percentage</summary>
    public double MinimumDataMatch { get; set; } = 95.0;

    /// <summary>Minimum acceptable schema compatibility</summary>
    public double MinimumSchemaCompatibility { get; set; } = 90.0;

    /// <summary>Acceptable post-launch issue count</summary>
    public int AcceptablePostLaunchIssues { get; set; } = 10;

    /// <summary>Enable trend analysis</summary>
    public bool EnableTrendAnalysis { get; set; } = true;

    /// <summary>Enable dependency analysis</summary>
    public bool EnableDependencyAnalysis { get; set; } = true;

    /// <summary>Enable mitigation planning</summary>
    public bool EnableMitigationPlanning { get; set; } = true;

    /// <summary>Days assumed for fix implementation per hour</summary>
    public double EstimatedDaysPerFixHour { get; set; } = 0.125; // 8 hours = 1 day
}

/// <summary>
/// Risk scoring weights
/// </summary>
public class RiskScoringWeights
{
    public double CriticalDiscrepancyWeight { get; set; } = 1.0;
    public double HighDiscrepancyWeight { get; set; } = 0.5;
    public double MediumDiscrepancyWeight { get; set; } = 0.25;
    public double LowDiscrepancyWeight { get; set; } = 0.05;
    public double BlockingIssueWeight { get; set; } = 5.0;
    public double DataImpactWeight { get; set; } = 2.0;
    public double SchemaImpactWeight { get; set; } = 1.5;
    public double LogicImpactWeight { get; set; } = 1.0;
}

/// <summary>
/// Migration readiness levels
/// </summary>
public enum MigrationReadiness
{
    Ready = 0,           // Go decision - all blockers resolved
    AlmostReady = 1,     // Close to ready - few items left
    PartiallyReady = 2,  // Some modules ready, others need work
    NotReady = 3         // No-Go decision - significant work needed
}

/// <summary>
/// Migration decision
/// </summary>
public enum MigrationDecision
{
    Go = 0,              // Proceed with migration
    GoWithRisks = 1,     // Proceed but with known risks
    Delay = 2,           // Wait for fixes
    NoGo = 3             // Do not migrate - blockers unresolved
}

/// <summary>
/// Risk priority
/// </summary>
public enum RiskPriority
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3
}

/// <summary>
/// Mitigation priority
/// </summary>
public enum MitigationPriority
{
    Immediate = 0,
    High = 1,
    Medium = 2,
    Low = 3
}

/// <summary>
/// Risk metrics for analysis
/// </summary>
public class RiskMetrics
{
    public double OverallRiskScore { get; set; }
    public double OverallHealthScore { get; set; }
    public int TotalDiscrepancies { get; set; }
    public int CriticalDiscrepancies { get; set; }
    public int BlockingIssues { get; set; }
    public double AverageModuleRiskScore { get; set; }
    public int HighRiskModules { get; set; }
    public int CriticalRiskItems { get; set; }
    public double DataCompleteness { get; set; }
    public double DataAccuracy { get; set; }
    public double SchemaCompatibility { get; set; }
    public double FunctionalCoverage { get; set; }
    public int EstimatedTotalFixHours { get; set; }
    public int EstimatedDaysToReady { get; set; }
    public Dictionary<string, int> RiskByModule { get; set; } = new();
    public Dictionary<string, int> IssuesByCategory { get; set; } = new();
}

/// <summary>
/// Launch readiness checklist
/// </summary>
public class LaunchReadinessChecklist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool AllDataMigrated { get; set; }
    public bool AllDataValidated { get; set; }
    public bool AllSchemaMapped { get; set; }
    public bool AllFunctionalityTested { get; set; }
    public bool AllCriticalIssuesFixed { get; set; }
    public bool PerformanceAcceptable { get; set; }
    public bool UserAcceptanceTested { get; set; }
    public bool DataBackupComplete { get; set; }
    public bool RollbackPlanReady { get; set; }
    public bool SupportTeamTrained { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public double CompletionPercentage => CompletedItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;
    public bool IsReadyForLaunch => CompletionPercentage >= 90;
}
