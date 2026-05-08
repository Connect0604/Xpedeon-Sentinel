namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for validation session memory service
/// Maintains context across all 6 phases of validation
/// </summary>
public interface IValidationMemoryService
{
    // Session Management
    /// <summary>
    /// Create a new validation session
    /// </summary>
    Task<ValidationSession> CreateSessionAsync(string clientId, string? clientName = null);

    /// <summary>
    /// Get current session or load from storage
    /// </summary>
    Task<ValidationSession?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Update session with latest status
    /// </summary>
    Task UpdateSessionAsync(ValidationSession session);

    /// <summary>
    /// Archive completed session
    /// </summary>
    Task ArchiveSessionAsync(string sessionId, string reason = "Validation completed");

    // Phase Management
    /// <summary>
    /// Move to next validation phase
    /// </summary>
    Task TransitionPhaseAsync(string sessionId, ValidationPhase nextPhase);

    /// <summary>
    /// Get status of specific phase
    /// </summary>
    Task<PhaseStatus?> GetPhaseStatusAsync(string sessionId, ValidationPhase phase);

    /// <summary>
    /// Update phase status with progress
    /// </summary>
    Task UpdatePhaseStatusAsync(string sessionId, PhaseStatus status);

    // Artifact Storage
    /// <summary>
    /// Store compressed artifact from phase
    /// </summary>
    Task StoreArtifactAsync(
        string sessionId,
        CompressedArtifact artifact);

    /// <summary>
    /// Retrieve artifact by ID
    /// </summary>
    Task<CompressedArtifact?> GetArtifactAsync(string sessionId, string artifactId);

    /// <summary>
    /// Get all artifacts from a phase
    /// </summary>
    Task<List<CompressedArtifact>> GetPhaseArtifactsAsync(
        string sessionId,
        ValidationPhase phase);

    // Context Retrieval
    /// <summary>
    /// Get context for current phase analysis
    /// Automatically decompresses relevant artifacts
    /// </summary>
    Task<string> GetContextForPhaseAsync(string sessionId, ValidationPhase phase);

    /// <summary>
    /// Get context from previous phases
    /// Used for intelligent cross-phase analysis
    /// </summary>
    Task<string> GetPreviousPhaseContextAsync(string sessionId, ValidationPhase upToPhase);

    /// <summary>
    /// Get full session context (all phases)
    /// </summary>
    Task<string> GetFullSessionContextAsync(string sessionId);

    // Findings Storage
    /// <summary>
    /// Store discovery findings
    /// </summary>
    Task StoreDiscoveryFindingsAsync(string sessionId, DiscoveryFindings findings);

    /// <summary>
    /// Retrieve discovery findings
    /// </summary>
    Task<DiscoveryFindings?> GetDiscoveryFindingsAsync(string sessionId);

    /// <summary>
    /// Store execution results
    /// </summary>
    Task StoreExecutionResultsAsync(string sessionId, ExecutionResults results);

    /// <summary>
    /// Retrieve execution results
    /// </summary>
    Task<ExecutionResults?> GetExecutionResultsAsync(string sessionId);

    /// <summary>
    /// Store comparison discrepancies
    /// </summary>
    Task StoreDiscrepanciesAsync(string sessionId, List<Discrepancy> discrepancies);

    /// <summary>
    /// Retrieve discrepancies
    /// </summary>
    Task<List<Discrepancy>> GetDiscrepanciesAsync(string sessionId);

    /// <summary>
    /// Store risk assessment
    /// </summary>
    Task StoreRiskAssessmentAsync(string sessionId, RiskAssessment assessment);

    /// <summary>
    /// Retrieve risk assessment
    /// </summary>
    Task<RiskAssessment?> GetRiskAssessmentAsync(string sessionId);

    // Persistence
    /// <summary>
    /// Persist session to storage (serialize + compress)
    /// </summary>
    Task PersistSessionAsync(ValidationSession session);

    /// <summary>
    /// Restore session from storage (decompress + deserialize)
    /// </summary>
    Task<ValidationSession?> RestoreSessionAsync(string sessionId);

    /// <summary>
    /// List all available sessions
    /// </summary>
    Task<List<ValidationSession>> ListSessionsAsync(string? clientId = null);

    // Statistics
    /// <summary>
    /// Get memory usage statistics for session
    /// </summary>
    SessionMemoryStats GetMemoryStatistics(string sessionId);

    /// <summary>
    /// Get decompression cache statistics
    /// </summary>
    Dictionary<string, int> GetCacheStatistics();

    /// <summary>
    /// Clear decompression cache
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Clear all memory for specific session
    /// </summary>
    Task ClearSessionMemoryAsync(string sessionId);
}

/// <summary>
/// Configuration for memory service
/// </summary>
public class MemoryServiceConfiguration
{
    /// <summary>Enable decompression caching</summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>Cache size limit (items)</summary>
    public int CacheSizeLimit { get; set; } = 500;

    /// <summary>Cache TTL (seconds)</summary>
    public int CacheTtlSeconds { get; set; } = 3600; // 1 hour

    /// <summary>Enable persistence to storage</summary>
    public bool EnablePersistence { get; set; } = true;

    /// <summary>Persistence path for serialized sessions</summary>
    public string PersistencePath { get; set; } = "./sessions";

    /// <summary>Auto-persist after each phase</summary>
    public bool AutoPersistAfterPhase { get; set; } = true;

    /// <summary>Enable memory optimization (aggressive cleanup)</summary>
    public bool EnableMemoryOptimization { get; set; } = true;

    /// <summary>Session expiration time (hours)</summary>
    public int SessionExpirationHours { get; set; } = 168; // 7 days
}
