namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for Ruflow workflow orchestration
/// Coordinates all 6 validation phases
/// </summary>
public interface IRuflowOrchestrator
{
    // Workflow Management
    Task<WorkflowContext> InitializeAsync(string sessionId, WorkflowConfig config);
    Task<WorkflowContext> ExecuteAsync(string sessionId);
    Task<WorkflowContext> GetStatusAsync(string sessionId);
    Task<bool> CancelAsync(string sessionId);
    Task<bool> PauseAsync(string sessionId);
    Task<bool> ResumeAsync(string sessionId);

    // Phase Execution
    Task<PhaseExecution> ExecutePhaseAsync(string sessionId, string phaseName);
    Task<PhaseExecution> GetPhaseStatusAsync(string sessionId, string phaseName);
    Task<List<PhaseExecution>> GetAllPhaseStatusesAsync(string sessionId);

    // Results
    Task<object?> GetPhaseResultAsync(string sessionId, string phaseName);
    Task<Dictionary<string, object>> GetAllResultsAsync(string sessionId);
    Task<bool> ExportResultsAsync(string sessionId, string format);

    // Configuration
    void Configure(WorkflowConfig config);
    WorkflowConfig GetConfiguration();
}
