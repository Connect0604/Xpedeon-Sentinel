namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for metrics calculation and analysis
/// Provides comprehensive aggregation of validation results into actionable metrics
/// </summary>
public interface IMetricsService
{
    // Initialization
    /// <summary>
    /// Initialize metrics calculation for session
    /// </summary>
    Task<ValidationMetrics> InitializeMetricsAsync(string sessionId);

    /// <summary>
    /// Get current metrics snapshot
    /// </summary>
    Task<ValidationMetrics> GetMetricsAsync(string sessionId);

    // Schema Metrics
    /// <summary>
    /// Calculate schema-level metrics from analysis results
    /// </summary>
    Task<SchemaMetrics> CalculateSchemaMetricsAsync(
        string sessionId,
        SchemaAnalysisResult schemaAnalysis);

    /// <summary>
    /// Update schema metrics as new analysis arrives
    /// </summary>
    Task UpdateSchemaMetricsAsync(
        string sessionId,
        SchemaMetrics metrics);

    // Data Metrics
    /// <summary>
    /// Calculate data comparison metrics from table comparisons
    /// </summary>
    Task<DataMetrics> CalculateDataMetricsAsync(
        string sessionId,
        TableComparisonResult comparison);

    /// <summary>
    /// Aggregate data metrics from multiple tables
    /// </summary>
    Task<DataMetrics> AggregateDataMetricsAsync(
        string sessionId,
        List<TableComparisonResult> comparisons);

    // Discrepancy Metrics
    /// <summary>
    /// Calculate discrepancy metrics from analysis results
    /// </summary>
    Task<DiscrepancyMetrics> CalculateDiscrepancyMetricsAsync(
        string sessionId,
        DiscrepancyAnalysisResult analysis);

    /// <summary>
    /// Get discrepancy distribution by category and severity
    /// </summary>
    Task<DiscrepancyMetrics> GetDiscrepancyBreakdownAsync(string sessionId);

    // Performance Metrics
    /// <summary>
    /// Record phase duration and throughput
    /// </summary>
    Task RecordPhasePerformanceAsync(
        string sessionId,
        string phaseName,
        long durationMs,
        int itemsProcessed);

    /// <summary>
    /// Calculate overall performance metrics
    /// </summary>
    Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(string sessionId);

    // Quality Metrics
    /// <summary>
    /// Calculate quality score (0-100) for validation
    /// </summary>
    Task<QualityMetrics> CalculateQualityMetricsAsync(string sessionId);

    /// <summary>
    /// Get per-module quality breakdown
    /// </summary>
    Task<List<ModuleMetrics>> GetModuleQualityAsync(string sessionId);

    // Risk Metrics
    /// <summary>
    /// Calculate risk metrics from discrepancies and assessment
    /// </summary>
    Task<RiskMetrics> CalculateRiskMetricsAsync(
        string sessionId,
        MigrationRiskAssessment assessment);

    /// <summary>
    /// Update risk metrics as new issues identified
    /// </summary>
    Task UpdateRiskMetricsAsync(
        string sessionId,
        RiskMetrics metrics);

    // Migration Readiness
    /// <summary>
    /// Calculate migration readiness score and recommendation
    /// </summary>
    Task<MigrationReadinessMetrics> CalculateReadinessAsync(string sessionId);

    /// <summary>
    /// Get detailed readiness assessment
    /// </summary>
    Task<MigrationReadinessMetrics> GetReadinessAsync(string sessionId);

    // Comparative Analysis
    /// <summary>
    /// Generate comparative metrics between legacy and Blazor systems
    /// </summary>
    Task<List<ComparativeMetrics>> CalculateComparativeMetricsAsync(
        string sessionId,
        List<string> metricNames);

    /// <summary>
    /// Compare specific metric across systems
    /// </summary>
    Task<ComparativeMetrics> CompareMetricAsync(
        string sessionId,
        string metricName,
        double legacyValue,
        double blazorValue);

    // Snapshots and Trending
    /// <summary>
    /// Record metrics snapshot for trend analysis
    /// </summary>
    Task<MetricSnapshot> RecordSnapshotAsync(
        string sessionId,
        string phaseName);

    /// <summary>
    /// Get historical metric snapshots
    /// </summary>
    Task<List<MetricSnapshot>> GetSnapshotsAsync(
        string sessionId,
        int? maxCount = null);

    /// <summary>
    /// Analyze metric trends from snapshots
    /// </summary>
    Task<MetricTrend> AnalyzeTrendAsync(
        string sessionId,
        string metricName);

    // Health Assessment
    /// <summary>
    /// Calculate overall validation health
    /// </summary>
    Task<ValidationHealth> CalculateHealthAsync(string sessionId);

    /// <summary>
    /// Get component-level health status
    /// </summary>
    Task<ValidationHealth> GetHealthAsync(string sessionId);

    /// <summary>
    /// Identify critical health issues
    /// </summary>
    Task<List<HealthIssue>> GetHealthIssuesAsync(string sessionId);

    // Module Metrics
    /// <summary>
    /// Calculate metrics for specific module
    /// </summary>
    Task<ModuleMetrics> CalculateModuleMetricsAsync(
        string sessionId,
        string moduleName);

    /// <summary>
    /// Get all module metrics
    /// </summary>
    Task<List<ModuleMetrics>> GetAllModuleMetricsAsync(string sessionId);

    /// <summary>
    /// Rank modules by risk and readiness
    /// </summary>
    Task<List<ModuleMetrics>> RankModulesByRiskAsync(string sessionId);

    // Aggregation
    /// <summary>
    /// Aggregate all metrics from sub-services
    /// </summary>
    Task<ValidationMetrics> AggregateAllMetricsAsync(string sessionId);

    /// <summary>
    /// Recalculate all metrics from raw data
    /// </summary>
    Task<ValidationMetrics> RecalculateAllMetricsAsync(string sessionId);

    /// <summary>
    /// Get metrics for specific phase
    /// </summary>
    Task<PhaseMetrics> GetPhaseMetricsAsync(
        string sessionId,
        string phaseName);

    // Analytics
    /// <summary>
    /// Analyze what-if scenarios
    /// </summary>
    Task<ScenarioAnalysis> AnalyzeScenarioAsync(
        string sessionId,
        string scenario,
        Dictionary<string, object> parameters);

    /// <summary>
    /// Get recommendations based on metrics
    /// </summary>
    Task<List<MetricRecommendation>> GetRecommendationsAsync(string sessionId);

    /// <summary>
    /// Identify improvement areas
    /// </summary>
    Task<List<ImprovementArea>> IdentifyImprovementAreasAsync(string sessionId);

    // Historical Data
    /// <summary>
    /// Export metrics for reporting
    /// </summary>
    Task<string> ExportMetricsAsync(
        string sessionId,
        string format); // json, csv, markdown

    /// <summary>
    /// Compare metrics across sessions
    /// </summary>
    Task<MetricsComparison> CompareSessionsAsync(
        string sessionId1,
        string sessionId2);

    /// <summary>
    /// Archive old metrics
    /// </summary>
    Task ArchiveMetricsAsync(
        string sessionId,
        int olderThanDays);

    // Configuration
    /// <summary>
    /// Configure metrics calculation
    /// </summary>
    void Configure(MetricsCalculationConfig config);

    /// <summary>
    /// Get current configuration
    /// </summary>
    MetricsCalculationConfig GetConfiguration();

    // Real-time Updates
    /// <summary>
    /// Subscribe to metrics updates
    /// </summary>
    void SubscribeToMetricsUpdates(
        string sessionId,
        Func<ValidationMetrics, Task> callback);

    /// <summary>
    /// Unsubscribe from metrics updates
    /// </summary>
    void UnsubscribeFromMetricsUpdates(string sessionId);

    /// <summary>
    /// Broadcast metrics update
    /// </summary>
    Task BroadcastMetricsUpdateAsync(ValidationMetrics metrics);

    /// <summary>
    /// Get metrics update stream
    /// </summary>
    IAsyncEnumerable<ValidationMetrics> GetMetricsStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metric trend analysis
/// </summary>
public class MetricTrend
{
    public string MetricName { get; set; } = string.Empty;
    public List<double> Values { get; set; } = new();
    public List<DateTime> Timestamps { get; set; } = new();
    public string Direction { get; set; } = "stable"; // improving, stable, degrading
    public double ChangePercentage { get; set; }
    public double Slope { get; set; }
    public string Forecast { get; set; } = "neutral"; // positive, neutral, negative
}

/// <summary>
/// Health issue detected during assessment
/// </summary>
public class HealthIssue
{
    public string IssueId { get; set; } = Guid.NewGuid().ToString();
    public string Component { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Phase-specific metrics
/// </summary>
public class PhaseMetrics
{
    public string PhaseName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public int ItemsProcessed { get; set; }
    public double ItemsPerSecond { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
    public Dictionary<string, object> PhaseSpecificMetrics { get; set; } = new();
}

/// <summary>
/// Scenario analysis results
/// </summary>
public class ScenarioAnalysis
{
    public string Scenario { get; set; } = string.Empty;
    public double SuccessProbability { get; set; } // 0-100
    public double TimelineImpactPercent { get; set; }
    public double CostImpactPercent { get; set; }
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Recommendation derived from metrics
/// </summary>
public class MetricRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Area { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public string? ActionableMetric { get; set; }
    public double? TargetValue { get; set; }
    public int? EstimatedEffortDays { get; set; }
}

/// <summary>
/// Area identified for improvement
/// </summary>
public class ImprovementArea
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Area { get; set; } = string.Empty;
    public string CurrentScore { get; set; } = string.Empty;
    public string TargetScore { get; set; } = string.Empty;
    public string Gap { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public int? EstimatedDays { get; set; }
}

/// <summary>
/// Metrics comparison between sessions
/// </summary>
public class MetricsComparison
{
    public string Session1Id { get; set; } = string.Empty;
    public string Session2Id { get; set; } = string.Empty;
    public DateTime Session1Time { get; set; }
    public DateTime Session2Time { get; set; }

    public Dictionary<string, ComparisonResult> Results { get; set; } = new();
}

/// <summary>
/// Individual metric comparison result
/// </summary>
public class ComparisonResult
{
    public string MetricName { get; set; } = string.Empty;
    public double Value1 { get; set; }
    public double Value2 { get; set; }
    public double DifferenceAbsolute { get; set; }
    public double DifferencePercent { get; set; }
    public string Status { get; set; } = "Unchanged"; // Improved, Unchanged, Degraded
}
