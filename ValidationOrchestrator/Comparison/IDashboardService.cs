namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for real-time dashboard updates and state management
/// Provides API contracts for frontend dashboard consumption
/// </summary>
public interface IDashboardService
{
    // State Management
    /// <summary>
    /// Get current dashboard state for session
    /// </summary>
    Task<DashboardState> GetDashboardStateAsync(string sessionId);

    /// <summary>
    /// Initialize dashboard for new session
    /// </summary>
    Task<DashboardState> InitializeDashboardAsync(string sessionId, string clientId);

    /// <summary>
    /// Update dashboard with latest data
    /// </summary>
    Task<DashboardState> RefreshDashboardAsync(string sessionId);

    // Phase Updates
    /// <summary>
    /// Update phase status and progress
    /// </summary>
    Task UpdatePhaseStatusAsync(
        string sessionId,
        string phaseName,
        string status,
        double progress);

    /// <summary>
    /// Record phase completion
    /// </summary>
    Task<PhaseStatus> CompletePhaseAsync(
        string sessionId,
        string phaseName,
        long durationMs);

    /// <summary>
    /// Record phase error
    /// </summary>
    Task RecordPhaseErrorAsync(
        string sessionId,
        string phaseName,
        string errorMessage);

    // Metrics Updates
    /// <summary>
    /// Update dashboard metrics
    /// </summary>
    Task UpdateMetricsAsync(
        string sessionId,
        DiscrepancyAnalysisResult? discrepancies = null,
        MigrationRiskAssessment? riskAssessment = null);

    /// <summary>
    /// Get latest metrics
    /// </summary>
    Task<DashboardMetrics> GetMetricsAsync(string sessionId);

    /// <summary>
    /// Update specific metric
    /// </summary>
    Task<MetricCard> UpdateMetricAsync(
        string sessionId,
        string metricName,
        object value);

    // Alerts
    /// <summary>
    /// Add alert to dashboard
    /// </summary>
    Task<DashboardAlert> AddAlertAsync(
        string sessionId,
        string type,
        string title,
        string message);

    /// <summary>
    /// Get active alerts
    /// </summary>
    Task<List<DashboardAlert>> GetAlertsAsync(
        string sessionId,
        bool includeAcknowledged = false);

    /// <summary>
    /// Acknowledge alert
    /// </summary>
    Task AcknowledgeAlertAsync(string alertId);

    /// <summary>
    /// Clear alerts
    /// </summary>
    Task ClearAlertsAsync(string sessionId);

    // Progress Tracking
    /// <summary>
    /// Add progress item
    /// </summary>
    Task<ProgressItem> AddProgressItemAsync(
        string sessionId,
        string title,
        string status);

    /// <summary>
    /// Get progress history
    /// </summary>
    Task<List<ProgressItem>> GetProgressAsync(string sessionId);

    /// <summary>
    /// Update progress item
    /// </summary>
    Task UpdateProgressItemAsync(
        string progressId,
        string status,
        long durationMs);

    // Executive Summary
    /// <summary>
    /// Get executive snapshot
    /// </summary>
    Task<ExecutiveSnapshot> GetExecutiveSnapshotAsync(string sessionId);

    /// <summary>
    /// Update executive snapshot
    /// </summary>
    Task UpdateExecutiveSnapshotAsync(
        string sessionId,
        MigrationRiskAssessment riskAssessment);

    // Module Details
    /// <summary>
    /// Get module details for drill-down
    /// </summary>
    Task<ModuleDetails> GetModuleDetailsAsync(
        string sessionId,
        string moduleName);

    /// <summary>
    /// Get all modules overview
    /// </summary>
    Task<List<ModuleDetails>> GetModulesOverviewAsync(string sessionId);

    // Real-time Updates
    /// <summary>
    /// Get update stream for session
    /// </summary>
    IAsyncEnumerable<DashboardUpdate> GetUpdatesStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to updates (for event-based systems)
    /// </summary>
    void SubscribeToUpdates(
        string sessionId,
        Func<DashboardUpdate, Task> callback);

    /// <summary>
    /// Unsubscribe from updates
    /// </summary>
    void UnsubscribeFromUpdates(string sessionId);

    /// <summary>
    /// Broadcast update to all subscribers
    /// </summary>
    Task BroadcastUpdateAsync(DashboardUpdate update);

    // Notifications
    /// <summary>
    /// Send notification to user
    /// </summary>
    Task<DashboardNotification> SendNotificationAsync(
        string userId,
        string type,
        string title,
        string message,
        int priority = 1);

    /// <summary>
    /// Get user notifications
    /// </summary>
    Task<List<DashboardNotification>> GetNotificationsAsync(
        string userId,
        bool unreadOnly = false);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task MarkNotificationAsReadAsync(string notificationId);

    // Filtering and Drill-down
    /// <summary>
    /// Apply filter to dashboard view
    /// </summary>
    Task<DashboardState> ApplyFilterAsync(
        string sessionId,
        DashboardFilter filter);

    /// <summary>
    /// Clear filters
    /// </summary>
    Task<DashboardState> ClearFiltersAsync(string sessionId);

    // Trends and Analytics
    /// <summary>
    /// Get trend data for metric
    /// </summary>
    Task<TrendLine> GetTrendAsync(
        string sessionId,
        string metricName,
        int hoursBack = 24);

    /// <summary>
    /// Get historical metrics
    /// </summary>
    Task<List<DashboardMetrics>> GetHistoricalMetricsAsync(
        string sessionId,
        DateTime startTime,
        DateTime endTime);

    // Export
    /// <summary>
    /// Export dashboard state
    /// </summary>
    Task<DashboardExport> ExportDashboardAsync(
        string sessionId,
        string format = "json",
        List<string>? sections = null);

    /// <summary>
    /// Get export status
    /// </summary>
    Task<DashboardExport?> GetExportStatusAsync(string exportId);

    // User Sessions
    /// <summary>
    /// Create user session
    /// </summary>
    Task<DashboardUserSession> CreateUserSessionAsync(
        string userId,
        string sessionId);

    /// <summary>
    /// Update user session
    /// </summary>
    Task UpdateUserSessionAsync(
        string userId,
        string sessionId,
        string currentView);

    /// <summary>
    /// End user session
    /// </summary>
    Task EndUserSessionAsync(string userId);

    // Configuration
    /// <summary>
    /// Get dashboard configuration
    /// </summary>
    DashboardConfig GetConfiguration();

    /// <summary>
    /// Update dashboard configuration
    /// </summary>
    void Configure(DashboardConfig config);

    /// <summary>
    /// Get live update configuration
    /// </summary>
    LiveUpdateConfig GetLiveUpdateConfig();

    /// <summary>
    /// Update live update configuration
    /// </summary>
    void ConfigureLiveUpdates(LiveUpdateConfig config);

    // Health and Status
    /// <summary>
    /// Get service health
    /// </summary>
    Task<DashboardServiceHealth> GetHealthAsync();

    /// <summary>
    /// Get active sessions count
    /// </summary>
    Task<int> GetActiveSessionsCountAsync();

    /// <summary>
    /// Get metrics for session
    /// </summary>
    Task<DashboardServiceMetrics> GetServiceMetricsAsync();
}

/// <summary>
/// Dashboard service health status
/// </summary>
public class DashboardServiceHealth
{
    public string Status { get; set; } = "healthy"; // healthy, degraded, unhealthy
    public long UptimeMs { get; set; }
    public int ActiveConnections { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Dashboard service metrics
/// </summary>
public class DashboardServiceMetrics
{
    public int TotalUpdatesProcessed { get; set; }
    public int TotalAlertsGenerated { get; set; }
    public int ActiveDashboards { get; set; }
    public long AverageUpdateLatencyMs { get; set; }
    public double UpdatesPerSecond { get; set; }
    public int PeakConcurrentConnections { get; set; }
}
