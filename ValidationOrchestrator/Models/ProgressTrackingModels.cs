namespace ValidationOrchestrator.Models;

/// <summary>
/// Real-time progress tracking for validation pipeline
/// </summary>
public class ProgressTracker
{
    public string SessionId { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = string.Empty;
    public int CurrentPhaseNumber { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedCompletionTime { get; set; }

    public List<PhaseProgress> PhaseProgresses { get; set; } = new();
    public double OverallProgress { get; set; } // 0-100
    public long ElapsedMs { get; set; }
    public long EstimatedRemainingMs { get; set; }
    public string Status { get; set; } = "running"; // running, paused, completed, failed
}

/// <summary>
/// Per-phase progress details
/// </summary>
public class PhaseProgress
{
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double Progress { get; set; } // 0-100
    public string Status { get; set; } = "pending"; // pending, running, completed, failed, skipped
    public long DurationMs { get; set; }
    public long EstimatedRemainingMs { get; set; }

    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public List<ProgressStep> Steps { get; set; } = new();

    public double StepProgressPercentage => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
}

/// <summary>
/// Individual step within a phase
/// </summary>
public class ProgressStep
{
    public string StepName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public string Status { get; set; } = "pending"; // pending, running, completed, failed, skipped
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long DurationMs { get; set; }
    public double Progress { get; set; } // 0-100 for long-running steps
    public string? Details { get; set; }
    public List<string> Artifacts { get; set; } = new(); // Generated outputs
}

/// <summary>
/// Progress update event
/// </summary>
public class ProgressUpdateEvent
{
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // PhaseStarted, StepStarted, StepProgress, StepCompleted, PhaseCompleted, EstimateUpdated
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? PhaseName { get; set; }
    public string? StepName { get; set; }
    public double? ProgressPercentage { get; set; }
    public long? ElapsedMs { get; set; }
    public long? EstimatedRemainingMs { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Progress estimation based on historical data
/// </summary>
public class ProgressEstimate
{
    public string PhaseName { get; set; } = string.Empty;
    public long AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public int SampleCount { get; set; }
    public double Confidence { get; set; } // 0-1, higher = more confident
    public DateTime? EstimatedCompletionTime { get; set; }
}

/// <summary>
/// Progress milestone
/// </summary>
public class ProgressMilestone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double TargetProgress { get; set; } // 0-100
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public string Priority { get; set; } = "normal"; // critical, high, normal, low
}

/// <summary>
/// Progress history entry
/// </summary>
public class ProgressHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public double Progress { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
}

/// <summary>
/// Progress statistics
/// </summary>
public class ProgressStatistics
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long TotalDurationMs { get; set; }

    public int PhasesCompleted { get; set; }
    public int TotalPhases { get; set; }
    public double AveragePhaseTime { get; set; }
    public double AverageStepTime { get; set; }

    public int StepsCompleted { get; set; }
    public int TotalSteps { get; set; }
    public int StepsFailed { get; set; }
    public int StepsSkipped { get; set; }

    public double AverageProgressVelocity { get; set; } // % per second
    public long EstimatedRemainingTime { get; set; }
    public DateTime? ProjectedCompletionTime { get; set; }
}

/// <summary>
/// Progress tracking configuration
/// </summary>
public class ProgressTrackingConfig
{
    /// <summary>Enable automatic estimate updates</summary>
    public bool EnableAutomaticEstimates { get; set; } = true;

    /// <summary>Number of historical samples to keep per phase</summary>
    public int HistoricalSampleSize { get; set; } = 10;

    /// <summary>Update interval for progress broadcasts (ms)</summary>
    public int UpdateIntervalMs { get; set; } = 500;

    /// <summary>Enable milestone tracking</summary>
    public bool EnableMilestones { get; set; } = true;

    /// <summary>Enable detailed step tracking</summary>
    public bool EnableDetailedSteps { get; set; } = true;

    /// <summary>Keep historical progress data</summary>
    public bool KeepHistory { get; set; } = true;

    /// <summary>History retention days</summary>
    public int HistoryRetentionDays { get; set; } = 30;
}

/// <summary>
/// Progress speed and velocity analysis
/// </summary>
public class ProgressVelocity
{
    public string SessionId { get; set; } = string.Empty;
    public double CurrentVelocityPercentPerSecond { get; set; }
    public double AverageVelocityPercentPerSecond { get; set; }
    public double MaxVelocityPercentPerSecond { get; set; }
    public double MinVelocityPercentPerSecond { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string Trend { get; set; } = "stable"; // accelerating, stable, decelerating
}
