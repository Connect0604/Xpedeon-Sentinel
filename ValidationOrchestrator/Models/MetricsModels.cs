namespace ValidationOrchestrator.Models;

/// <summary>
/// Overall validation metrics aggregating all phases
/// </summary>
public class ValidationMetrics
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Schema Metrics
    public SchemaMetrics SchemaMetrics { get; set; } = new();

    // Data Metrics
    public DataMetrics DataMetrics { get; set; } = new();

    // Discrepancy Metrics
    public ValidationDiscrepancyMetrics DiscrepancyMetrics { get; set; } = new();

    // Performance Metrics
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();

    // Quality Metrics
    public QualityMetrics QualityMetrics { get; set; } = new();

    // Risk Metrics
    public ValidationRiskMetrics RiskMetrics { get; set; } = new();

    // Migration Readiness
    public MigrationReadinessMetrics ReadinessMetrics { get; set; } = new();
}

/// <summary>
/// Schema comparison metrics
/// </summary>
public class SchemaMetrics
{
    // Table metrics
    public int TotalTableCount { get; set; }
    public int MatchedTableCount { get; set; }
    public double TableMatchPercentage { get; set; } // 0-100

    // Column metrics
    public int TotalColumnCount { get; set; }
    public int MatchedColumnCount { get; set; }
    public double ColumnMatchPercentage { get; set; } // 0-100

    // Data type metrics
    public int TotalDataTypeCount { get; set; }
    public int CompatibleDataTypeCount { get; set; }
    public double DataTypeCompatibilityPercentage { get; set; } // 0-100

    // Constraint metrics
    public int PrimaryKeysMatched { get; set; }
    public int ForeignKeysMatched { get; set; }
    public int IndexesMatched { get; set; }
    public double ConstraintMatchPercentage { get; set; } // 0-100

    // Completeness
    public double SchemaCompleteness { get; set; } // 0-100 overall schema coverage
}

/// <summary>
/// Row-by-row data comparison metrics
/// </summary>
public class DataMetrics
{
    // Count metrics
    public long TotalRowsLegacy { get; set; }
    public long TotalRowsBlazor { get; set; }
    public long MatchedRowCount { get; set; }
    public long MismatchedRowCount { get; set; }
    public long UnmatchedLegacyRows { get; set; }
    public long UnmatchedBlazorRows { get; set; }

    // Percentage metrics
    public double DataMatchPercentage { get; set; } // 0-100
    public double DataLossPercentage { get; set; } // 0-100
    public double DataGainPercentage { get; set; } // 0-100
    public double DataIntegrityScore { get; set; } // 0-100

    // Column-level
    public int TotalColumnCount { get; set; }
    public int ColumnsWithMismatches { get; set; }
    public double ColumnMismatchPercentage { get; set; } // 0-100

    // Value-level
    public long TotalValueCount { get; set; }
    public long MismatchedValueCount { get; set; }
    public double ValueMismatchPercentage { get; set; } // 0-100

    // Data quality
    public int NullValueDiscrepancies { get; set; }
    public int TypeMismatchCount { get; set; }
    public int OutOfRangeValuesCount { get; set; }
}

/// <summary>
/// Discrepancy analysis metrics
/// </summary>
public class ValidationDiscrepancyMetrics
{
    // Counts
    public int TotalDiscrepancies { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }

    // Distribution by category
    public int DataTypeIssues { get; set; }
    public int DataLossIssues { get; set; }
    public int NullHandlingIssues { get; set; }
    public int ReferentialIntegrityIssues { get; set; }
    public int CalculationLogicIssues { get; set; }
    public int FormatConversionIssues { get; set; }
    public int BusinessRuleViolations { get; set; }
    public int PerformanceDegradations { get; set; }
    public int OtherIssues { get; set; }

    // Impact metrics
    public double AverageImpactedRowPercentage { get; set; } // 0-100
    public int TotalAffectedTables { get; set; }
    public int TotalAffectedColumns { get; set; }
    public int EstimatedAffectedUsers { get; set; }

    // Resolution metrics
    public int ResolvedDiscrepancies { get; set; }
    public int UnresolvedDiscrepancies { get; set; }
    public double ResolutionPercentage { get; set; } // 0-100
}

/// <summary>
/// Validation process performance metrics
/// </summary>
public class PerformanceMetrics
{
    // Timing
    public long DiscoveryDurationMs { get; set; }
    public long TestGenerationDurationMs { get; set; }
    public long ExecutionDurationMs { get; set; }
    public long ComparisonDurationMs { get; set; }
    public long ReviewDurationMs { get; set; }
    public long ReportingDurationMs { get; set; }
    public long TotalDurationMs { get; set; }

    // Throughput
    public double TablesAnalyzedPerSecond { get; set; }
    public double RowsComparedPerSecond { get; set; }
    public double TestsExecutedPerSecond { get; set; }
    public double DiscrepanciesIdentifiedPerSecond { get; set; }

    // Resource efficiency
    public double AverageMemoryUsageMb { get; set; }
    public double PeakMemoryUsageMb { get; set; }
    public double AverageCpuUsagePercent { get; set; }
    public int ParallelThreadsUsed { get; set; }

    // Optimization
    public double CacheHitRatePercent { get; set; } // 0-100
    public int DatabaseConnectionPoolSize { get; set; }
    public double CompressionRatioPercent { get; set; } // 0-100
}

/// <summary>
/// Data quality metrics
/// </summary>
public class QualityMetrics
{
    // Accuracy metrics
    public double SchemaAccuracy { get; set; } // 0-100
    public double DataAccuracy { get; set; } // 0-100
    public double LogicAccuracy { get; set; } // 0-100
    public double OverallAccuracy { get; set; } // 0-100

    // Completeness metrics
    public double SchemaCompleteness { get; set; } // 0-100
    public double DataCompleteness { get; set; } // 0-100
    public double TestCoveragePercentage { get; set; } // 0-100
    public double ValidationCompleteness { get; set; } // 0-100

    // Consistency metrics
    public double DataConsistencyScore { get; set; } // 0-100
    public double ReferentialIntegrityScore { get; set; } // 0-100
    public double ConstraintComplianceScore { get; set; } // 0-100

    // Validation metrics
    public int FailedValidations { get; set; }
    public int PassedValidations { get; set; }
    public int SkippedValidations { get; set; }
    public double ValidationPassRate { get; set; } // 0-100

    // Test metrics
    public int TotalTestCases { get; set; }
    public int PassedTestCases { get; set; }
    public int FailedTestCases { get; set; }
    public double TestPassRate { get; set; } // 0-100
}

/// <summary>
/// Risk assessment metrics
/// </summary>
public class ValidationRiskMetrics
{
    // Risk scoring
    public double OverallRiskScore { get; set; } // 0-100
    public double SchemaRiskScore { get; set; } // 0-100
    public double DataRiskScore { get; set; } // 0-100
    public double LogicRiskScore { get; set; } // 0-100
    public double PerformanceRiskScore { get; set; } // 0-100

    // Risk distribution
    public int CriticalRisks { get; set; }
    public int HighRisks { get; set; }
    public int MediumRisks { get; set; }
    public int LowRisks { get; set; }

    // Risk areas
    public int AffectedModules { get; set; }
    public int AffectedBusinessProcesses { get; set; }
    public int PotentiallyAffectedUsers { get; set; }
    public int HighImpactTables { get; set; }

    // Mitigation
    public int MitigationStrategiesIdentified { get; set; }
    public int MitigationStrategiesImplemented { get; set; }
    public double MitigationCoveragePercent { get; set; } // 0-100

    // Confidence
    public double AssessmentConfidenceLevel { get; set; } // 0-1
    public string ConfidenceRating { get; set; } = "medium"; // low, medium, high
}

/// <summary>
/// Migration readiness metrics
/// </summary>
public class MigrationReadinessMetrics
{
    // Readiness scores
    public double TechnicalReadiness { get; set; } // 0-100
    public double DataReadiness { get; set; } // 0-100
    public double UserReadiness { get; set; } // 0-100
    public double ProcessReadiness { get; set; } // 0-100
    public double OverallReadiness { get; set; } // 0-100

    // Go/No-Go indicators
    public string RecommendedDecision { get; set; } = "GoWithRisks"; // Go, GoWithRisks, Delay, NoGo
    public string DecisionRationale { get; set; } = string.Empty;
    public double DecisionConfidence { get; set; } // 0-100

    // Blockers and issues
    public int BlockingIssues { get; set; }
    public int WarningIssues { get; set; }
    public int InfoIssues { get; set; }

    // Timeline
    public int EstimatedCutoverDays { get; set; }
    public int EstimatedRollbackTimeDays { get; set; }
    public string PreferredCutoverWindow { get; set; } = string.Empty;

    // Success probability
    public double FirstTimeSucessProbability { get; set; } // 0-100
    public double ZeroDowntimeMigrationProbability { get; set; } // 0-100
    public double NoUserImpactProbability { get; set; } // 0-100

    // Key dependencies
    public int CriticalDependencies { get; set; }
    public int ExternalDependencies { get; set; }
    public int ThirdPartyIntegrations { get; set; }
}

/// <summary>
/// Per-module metrics for breakdown analysis
/// </summary>
public class ModuleMetrics
{
    public string ModuleName { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int RecordCount { get; set; }

    // Quality scores
    public double SchemaQualityScore { get; set; } // 0-100
    public double DataQualityScore { get; set; } // 0-100
    public double OverallQualityScore { get; set; } // 0-100

    // Migration readiness
    public double MigrationReadiness { get; set; } // 0-100
    public string MigrationStatus { get; set; } = "Ready"; // Ready, AtRisk, NotReady
    public int PendingIssues { get; set; }

    // Complexity
    public int ComplexityScore { get; set; } // 0-100
    public int DependencyCount { get; set; }
    public string ComplexityRating { get; set; } = "Medium"; // Low, Medium, High, Critical
}

/// <summary>
/// Time-series metric snapshot for trend analysis
/// </summary>
public class MetricSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SessionId { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;

    // Key metrics at this point
    public double OverallProgress { get; set; } // 0-100
    public double DataMatchPercentage { get; set; } // 0-100
    public double RiskScore { get; set; } // 0-100
    public double QualityScore { get; set; } // 0-100

    // Trending
    public string ProgressTrend { get; set; } = "stable"; // accelerating, stable, decelerating
    public string QualityTrend { get; set; } = "stable"; // improving, stable, degrading
    public string RiskTrend { get; set; } = "stable"; // decreasing, stable, increasing
}

/// <summary>
/// Comparative metrics between legacy and Blazor systems
/// </summary>
public class ComparativeMetrics
{
    public string MetricName { get; set; } = string.Empty;
    public double LegacyValue { get; set; }
    public double BlazorValue { get; set; }
    public double DifferenceAbsolute { get; set; }
    public double DifferencePercentage { get; set; }
    public string Status { get; set; } = "Match"; // Match, Mismatch, Migration-Acceptable, DataLoss
    public string Severity { get; set; } = "Low"; // Low, Medium, High, Critical
}

/// <summary>
/// Metrics configuration
/// </summary>
public class MetricsCalculationConfig
{
    /// <summary>Enable automatic metric recalculation</summary>
    public bool EnableAutoCalculation { get; set; } = true;

    /// <summary>Interval for automatic recalculation (ms)</summary>
    public int RecalculationIntervalMs { get; set; } = 5000;

    /// <summary>Number of snapshots to retain for trend analysis</summary>
    public int SnapshotRetentionCount { get; set; } = 100;

    /// <summary>Enable detailed module-level metrics</summary>
    public bool EnableModuleMetrics { get; set; } = true;

    /// <summary>Enable time-series snapshots</summary>
    public bool EnableSnapshots { get; set; } = true;

    /// <summary>Thresholds for quality scoring</summary>
    public double ExcellentThreshold { get; set; } = 95; // >= 95%
    public double GoodThreshold { get; set; } = 80; // >= 80%
    public double AcceptableThreshold { get; set; } = 65; // >= 65%
    public double PoorThreshold { get; set; } = 50; // >= 50%
    // Below 50% is considered failing

    /// <summary>Risk score calculation weights</summary>
    public double DataLossWeight { get; set; } = 0.35;
    public double DiscrepancyWeight { get; set; } = 0.25;
    public double PerformanceRiskWeight { get; set; } = 0.20;
    public double ComplexityWeight { get; set; } = 0.20;
}

/// <summary>
/// Health indicators derived from metrics
/// </summary>
public class ValidationHealth
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

    // Overall health
    public double HealthScore { get; set; } // 0-100
    public string HealthStatus { get; set; } = "Healthy"; // Healthy, Warning, Critical

    // Component health
    public MetricComponentHealth SchemaHealth { get; set; } = new();
    public MetricComponentHealth DataHealth { get; set; } = new();
    public MetricComponentHealth LogicHealth { get; set; } = new();
    public MetricComponentHealth PerformanceHealth { get; set; } = new();

    // Trend
    public string HealthTrend { get; set; } = "stable"; // improving, stable, degrading
    public int DaysToGoLive { get; set; }
}

/// <summary>
/// Individual component health status
/// </summary>
public class MetricComponentHealth
{
    public string ComponentName { get; set; } = string.Empty;
    public double HealthScore { get; set; } // 0-100
    public string Status { get; set; } = "Healthy"; // Healthy, Warning, Critical
    public int IssueCount { get; set; }
    public List<string> TopIssues { get; set; } = new();
}
