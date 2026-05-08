namespace ValidationOrchestrator.Models;

/// <summary>
/// Real-time dashboard state for UI consumption
/// </summary>
public class DashboardState
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ValidationPipelineStatus PipelineStatus { get; set; } = new();
    public DashboardMetrics Metrics { get; set; } = new();
    public List<DashboardAlert> Alerts { get; set; } = new();
    public List<ProgressItem> Progress { get; set; } = new();
    public ExecutiveSnapshot ExecutiveSnapshot { get; set; } = new();

    public bool IsComplete { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public double OverallProgress { get; set; } // 0-100
}

/// <summary>
/// Pipeline status and phase information
/// </summary>
public class ValidationPipelineStatus
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long ElapsedMs { get; set; }

    public List<PhaseStatus> Phases { get; set; } = new();
    public string CurrentPhase { get; set; } = string.Empty;
    public int CurrentPhaseNumber { get; set; }
    public int TotalPhases { get; set; } = 6;

    public bool IsRunning { get; set; }
    public bool HasErrors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Individual phase status
/// </summary>
public class PhaseStatus
{
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseNumber { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed, Skipped
    public double Progress { get; set; } // 0-100
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public List<string> Artifacts { get; set; } = new(); // Generated files/data
}

/// <summary>
/// Dashboard metrics for charts and KPIs
/// </summary>
public class DashboardMetrics
{
    public MetricCard RiskScore { get; set; } = new();
    public MetricCard HealthScore { get; set; } = new();
    public MetricCard DiscrepancyCount { get; set; } = new();
    public MetricCard BlockingIssues { get; set; } = new();
    public MetricCard ReadyModules { get; set; } = new();
    public MetricCard EstimatedDaysToReady { get; set; } = new();

    public ChartData SeverityDistribution { get; set; } = new();
    public ChartData DiscrepanciesByModule { get; set; } = new();
    public ChartData ModuleRiskScores { get; set; } = new();
    public ChartData ReadinessByModule { get; set; } = new();

    public TrendLine RiskTrend { get; set; } = new();
    public TrendLine HealthTrend { get; set; } = new();
}

/// <summary>
/// Single metric card for dashboard display
/// </summary>
public class MetricCard
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "normal"; // normal, warning, critical, success
    public string? Target { get; set; }
    public double? PercentComplete { get; set; }
    public string TrendDirection { get; set; } = "stable"; // improving, stable, deteriorating
    public string? Tooltip { get; set; }
}

/// <summary>
/// Chart data (for pie, bar, line charts)
/// </summary>
public class ChartData
{
    public string Title { get; set; } = string.Empty;
    public string ChartType { get; set; } = "pie"; // pie, bar, line, doughnut
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
    public ChartOptions Options { get; set; } = new();
}

/// <summary>
/// Chart dataset
/// </summary>
public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public List<double> Data { get; set; } = new();
    public List<string>? BackgroundColor { get; set; }
    public List<string>? BorderColor { get; set; }
    public double? BorderWidth { get; set; }
}

/// <summary>
/// Chart options
/// </summary>
public class ChartOptions
{
    public bool Responsive { get; set; } = true;
    public bool MaintainAspectRatio { get; set; } = true;
    public string? Title { get; set; }
    public Dictionary<string, object>? Legend { get; set; }
}

/// <summary>
/// Trend line data
/// </summary>
public class TrendLine
{
    public string Title { get; set; } = string.Empty;
    public List<DateTime> Timestamps { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public string Trend { get; set; } = "stable"; // improving, stable, deteriorating
    public double? ProjectedValue { get; set; }
    public DateTime? ProjectedDate { get; set; }
}

/// <summary>
/// Alert for dashboard notifications
/// </summary>
public class DashboardAlert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "info"; // info, warning, error, success
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; } // Phase name
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; }
    public int? RelatedIssueCount { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
}

/// <summary>
/// Progress item for timeline
/// </summary>
public class ProgressItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Completed, InProgress, Pending
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long DurationMs { get; set; }
    public string? Details { get; set; }
    public int? CompletedCount { get; set; }
    public int? TotalCount { get; set; }
}

/// <summary>
/// Executive snapshot for summary view
/// </summary>
public class ExecutiveSnapshot
{
    public string Decision { get; set; } = string.Empty; // Go, NoGo, Delay, etc.
    public double OverallRiskScore { get; set; }
    public double OverallHealthScore { get; set; }
    public int CriticalBlockers { get; set; }
    public int TotalDiscrepancies { get; set; }
    public int ReadyModules { get; set; }
    public int TotalModules { get; set; }
    public double ConfidenceLevel { get; set; }
    public DateTime? ProjectedReadyDate { get; set; }
    public List<string> TopRisks { get; set; } = new();
    public List<string> TopActions { get; set; } = new();
}

/// <summary>
/// Module details for drill-down view
/// </summary>
public class ModuleDetails
{
    public string ModuleName { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DiscrepancyCount { get; set; }
    public int BlockingIssues { get; set; }
    public double DataCompleteness { get; set; }
    public double DataAccuracy { get; set; }
    public double FunctionalCoverage { get; set; }
    public List<ModuleIssue> TopIssues { get; set; } = new();
    public List<ModuleAction> RequiredActions { get; set; } = new();
}

/// <summary>
/// Module-level issue
/// </summary>
public class ModuleIssue
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int AffectedCount { get; set; }
    public double ImpactPercentage { get; set; }
    public bool IsBlocker { get; set; }
}

/// <summary>
/// Module action item
/// </summary>
public class ModuleAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int EstimatedHours { get; set; }
    public bool IsPreLaunch { get; set; }
}

/// <summary>
/// Real-time update event for WebSocket/SignalR
/// </summary>
public class DashboardUpdate
{
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // PhaseStarted, PhaseCompleted, MetricsUpdated, AlertAdded, etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Payload { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Notification for user alerts
/// </summary>
public class DashboardNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // BlockerFound, PhaseCompleted, RiskThresholdExceeded
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Priority { get; set; } = 1; // 1-5, 5 is highest
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Read { get; set; }
    public string? ActionUrl { get; set; }
}

/// <summary>
/// Live update configuration
/// </summary>
public class LiveUpdateConfig
{
    public bool EnableRealTimeUpdates { get; set; } = true;
    public int UpdateIntervalMs { get; set; } = 1000; // 1 second
    public bool PushAlerts { get; set; } = true;
    public bool PushMetricsUpdates { get; set; } = true;
    public bool PushPhaseUpdates { get; set; } = true;
    public int MaxConcurrentConnections { get; set; } = 100;
    public int AlertRetentionMinutes { get; set; } = 60;
    public int MetricsHistoryPoints { get; set; } = 100;
}

/// <summary>
/// Dashboard configuration
/// </summary>
public class DashboardConfig
{
    public bool ShowDiscoveryPhase { get; set; } = true;
    public bool ShowExecutionPhase { get; set; } = true;
    public bool ShowComparisonPhase { get; set; } = true;
    public bool ShowRiskAssessment { get; set; } = true;
    public bool ShowDetailedMetrics { get; set; } = true;
    public bool ShowModuleDetails { get; set; } = true;
    public bool ShowTrendAnalysis { get; set; } = true;
    public bool AllowDrillDown { get; set; } = true;
    public bool ShowExportOptions { get; set; } = true;

    public string DefaultView { get; set; } = "executive"; // executive, detailed, technical
    public int RefreshIntervalMs { get; set; } = 2000; // 2 seconds
    public bool DarkMode { get; set; } = false;
    public string? ThemeColor { get; set; } = "#1976d2"; // Blue
}

/// <summary>
/// Filter criteria for dashboard views
/// </summary>
public class DashboardFilter
{
    public List<string>? ModuleFilter { get; set; }
    public List<string>? SeverityFilter { get; set; } // Critical, High, Medium, Low
    public List<string>? StatusFilter { get; set; } // Success, Warning, Error
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? OnlyBlockers { get; set; }
    public bool? OnlyUnresolved { get; set; }
}

/// <summary>
/// Export format for dashboard data
/// </summary>
public class DashboardExport
{
    public string SessionId { get; set; } = string.Empty;
    public string Format { get; set; } = "json"; // json, csv, pdf, xlsx
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string? ExportPath { get; set; }
    public long FileSizeBytes { get; set; }
    public List<string> IncludedSections { get; set; } = new();
}

/// <summary>
/// Dashboard user session
/// </summary>
public class DashboardUserSession
{
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public string CurrentView { get; set; } = "executive";
    public DashboardFilter? CurrentFilter { get; set; }
    public List<string> ViewHistory { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
