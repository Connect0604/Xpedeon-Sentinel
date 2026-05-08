namespace ValidationOrchestrator.Models;

/// <summary>
/// Workflow execution context
/// </summary>
public class WorkflowContext
{
    public string SessionId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
    public List<PhaseExecution> Phases { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
}

/// <summary>
/// Phase execution details
/// </summary>
public class PhaseExecution
{
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseNumber { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long DurationMs { get; set; }
    public int? ProgressPercentage { get; set; }
    public List<string> Errors { get; set; } = new();
    public int RetryCount { get; set; }
    public object? Output { get; set; }
}

/// <summary>
/// Workflow configuration
/// </summary>
public class WorkflowConfig
{
    public bool Sequential { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 3600;
    public bool StopOnError { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public List<string> PhasesToExecute { get; set; } = new();
}
