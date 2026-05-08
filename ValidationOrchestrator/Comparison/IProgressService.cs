namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for real-time progress tracking and estimation
/// Provides accurate progress updates and time-to-completion estimates
/// </summary>
public interface IProgressService
{
    // Initialization
    /// <summary>
    /// Initialize progress tracking for session
    /// </summary>
    Task<ProgressTracker> InitializeProgressAsync(
        string sessionId,
        List<string> phaseNames);

    /// <summary>
    /// Get current progress state
    /// </summary>
    Task<ProgressTracker> GetProgressAsync(string sessionId);

    // Phase Tracking
    /// <summary>
    /// Start phase and initialize progress
    /// </summary>
    Task<PhaseProgress> StartPhaseAsync(
        string sessionId,
        string phaseName,
        int phaseNumber,
        int estimatedSteps = 5);

    /// <summary>
    /// Update phase progress
    /// </summary>
    Task UpdatePhaseProgressAsync(
        string sessionId,
        string phaseName,
        double progressPercentage);

    /// <summary>
    /// Complete phase
    /// </summary>
    Task<PhaseProgress> CompletePhaseAsync(
        string sessionId,
        string phaseName);

    /// <summary>
    /// Mark phase as failed
    /// </summary>
    Task<PhaseProgress> FailPhaseAsync(
        string sessionId,
        string phaseName,
        string errorMessage);

    // Step Tracking
    /// <summary>
    /// Start step within phase
    /// </summary>
    Task<ProgressStep> StartStepAsync(
        string sessionId,
        string phaseName,
        string stepName,
        int stepNumber);

    /// <summary>
    /// Update step progress
    /// </summary>
    Task UpdateStepProgressAsync(
        string sessionId,
        string phaseName,
        string stepName,
        double progressPercentage);

    /// <summary>
    /// Complete step
    /// </summary>
    Task<ProgressStep> CompleteStepAsync(
        string sessionId,
        string phaseName,
        string stepName);

    /// <summary>
    /// Add artifact to step
    /// </summary>
    Task AddStepArtifactAsync(
        string sessionId,
        string phaseName,
        string stepName,
        string artifactPath);

    // Progress Updates
    /// <summary>
    /// Record progress update event
    /// </summary>
    Task<ProgressUpdateEvent> RecordProgressEventAsync(
        ProgressUpdateEvent eventData);

    /// <summary>
    /// Get progress history
    /// </summary>
    Task<List<ProgressHistoryEntry>> GetProgressHistoryAsync(
        string sessionId,
        int? maxEntries = null);

    /// <summary>
    /// Get recent progress updates
    /// </summary>
    Task<List<ProgressUpdateEvent>> GetRecentUpdatesAsync(
        string sessionId,
        int maxEvents = 50);

    // Estimation
    /// <summary>
    /// Get progress estimate for phase
    /// </summary>
    Task<ProgressEstimate> GetPhaseEstimateAsync(
        string phaseName);

    /// <summary>
    /// Update phase estimate based on current progress
    /// </summary>
    Task<ProgressEstimate> UpdatePhaseEstimateAsync(
        string sessionId,
        string phaseName);

    /// <summary>
    /// Get overall completion estimate
    /// </summary>
    Task<DateTime?> GetCompletionEstimateAsync(string sessionId);

    /// <summary>
    /// Get remaining time estimate
    /// </summary>
    Task<long?> GetRemainingTimeEstimateAsync(string sessionId);

    // Velocity and Analytics
    /// <summary>
    /// Calculate progress velocity
    /// </summary>
    Task<ProgressVelocity> CalculateVelocityAsync(string sessionId);

    /// <summary>
    /// Get progress statistics
    /// </summary>
    Task<ProgressStatistics> GetStatisticsAsync(string sessionId);

    /// <summary>
    /// Compare progress to historical average
    /// </summary>
    Task<double> GetProgressComparisonAsync(
        string sessionId,
        string phaseName);

    // Milestones
    /// <summary>
    /// Add progress milestone
    /// </summary>
    Task<ProgressMilestone> AddMilestoneAsync(
        string sessionId,
        string name,
        double targetProgress,
        string priority = "normal");

    /// <summary>
    /// Get milestones
    /// </summary>
    Task<List<ProgressMilestone>> GetMilestonesAsync(string sessionId);

    /// <summary>
    /// Check if milestone reached
    /// </summary>
    Task CheckMilestonesAsync(string sessionId);

    // Real-time Updates
    /// <summary>
    /// Subscribe to progress updates
    /// </summary>
    void SubscribeToUpdates(
        string sessionId,
        Func<ProgressUpdateEvent, Task> callback);

    /// <summary>
    /// Unsubscribe from updates
    /// </summary>
    void UnsubscribeFromUpdates(string sessionId);

    /// <summary>
    /// Stream progress updates
    /// </summary>
    IAsyncEnumerable<ProgressUpdateEvent> GetUpdatesStreamAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast progress update
    /// </summary>
    Task BroadcastUpdateAsync(ProgressUpdateEvent update);

    // Configuration
    /// <summary>
    /// Configure progress tracking
    /// </summary>
    void Configure(ProgressTrackingConfig config);

    /// <summary>
    /// Get current configuration
    /// </summary>
    ProgressTrackingConfig GetConfiguration();

    // Historical Data
    /// <summary>
    /// Record historical data point for phase
    /// </summary>
    Task RecordPhaseMetricsAsync(
        string phaseName,
        long durationMs);

    /// <summary>
    /// Get historical phase metrics
    /// </summary>
    Task<ProgressEstimate> GetHistoricalMetricsAsync(string phaseName);

    /// <summary>
    /// Clear old historical data
    /// </summary>
    Task PurgeOldHistoryAsync(int retentionDays);
}
