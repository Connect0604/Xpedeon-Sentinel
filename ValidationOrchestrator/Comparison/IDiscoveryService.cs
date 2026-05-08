namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Orchestrates Phase 1 — Discovery &amp; Planning.
/// Coordinates business logic extraction, schema discovery, expert interviews,
/// and produces a DiscoveryReport as the phase deliverable.
/// </summary>
public interface IDiscoveryService
{
    // Phase Execution
    /// <summary>
    /// Run the full Phase 1 Discovery pipeline and return the discovery report
    /// </summary>
    Task<DiscoveryReport> RunDiscoveryAsync(
        string sessionId,
        string clientId,
        DiscoveryConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume a previously interrupted discovery session
    /// </summary>
    Task<DiscoveryReport> ResumeDiscoveryAsync(
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default);

    // Business Logic Analysis
    /// <summary>
    /// Analyze legacy source code and extract business logic items
    /// </summary>
    Task<List<BusinessLogicItem>> AnalyzeSourceCodeAsync(
        string sessionId,
        string clientId,
        List<CodeFile> files,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load and prepare code files from a directory for analysis
    /// </summary>
    Task<List<CodeFile>> LoadCodeFilesAsync(
        string sourcePath,
        DiscoveryConfig config);

    // Expert Interviews
    /// <summary>
    /// Record findings from a domain expert interview
    /// </summary>
    Task<ExpertInterviewRecord> RecordExpertInterviewAsync(
        string sessionId,
        string clientId,
        string expertName,
        string expertRole,
        List<ExpertFinding> findings,
        string? rawNotes = null);

    /// <summary>
    /// Retrieve all expert interviews for a session
    /// </summary>
    Task<List<ExpertInterviewRecord>> GetExpertInterviewsAsync(
        string sessionId,
        string clientId);

    /// <summary>
    /// Apply expert feedback to update logic item risk/confirmation status
    /// </summary>
    Task ApplyExpertFeedbackAsync(
        string sessionId,
        string clientId,
        ExpertInterviewRecord interview);

    // Findings Management
    /// <summary>
    /// Store business logic items discovered during the session
    /// </summary>
    Task StoreDiscoveryFindingsAsync(
        string sessionId,
        string clientId,
        List<BusinessLogicItem> items);

    /// <summary>
    /// Retrieve all business logic items for a session
    /// </summary>
    Task<List<BusinessLogicItem>> GetDiscoveryFindingsAsync(
        string sessionId,
        string clientId);

    /// <summary>
    /// Add a single finding discovered post-extraction (e.g., from an expert)
    /// </summary>
    Task AddFindingAsync(
        string sessionId,
        string clientId,
        BusinessLogicItem item);

    // Reporting
    /// <summary>
    /// Generate the Phase 1 Discovery Report from all collected findings
    /// </summary>
    Task<DiscoveryReport> GenerateDiscoveryReportAsync(
        string sessionId,
        string clientId);

    /// <summary>
    /// Compute module-level risk summaries from discovered items
    /// </summary>
    List<ModuleRiskSummary> ComputeModuleRisks(List<BusinessLogicItem> items);

    /// <summary>
    /// Return the session's current discovery progress (0-100)
    /// </summary>
    Task<double> GetDiscoveryProgressAsync(string sessionId, string clientId);

    // Configuration
    /// <summary>Configure the discovery service</summary>
    void Configure(DiscoveryServiceConfig config);
}

/// <summary>
/// Configuration for the discovery service
/// </summary>
public class DiscoveryServiceConfig
{
    /// <summary>Minimum risk level to include in the report</summary>
    public BusinessLogicRisk MinimumRiskLevel { get; set; } = BusinessLogicRisk.Low;

    /// <summary>Auto-run expert interview prompts via AI when no human available</summary>
    public bool EnableAiExpertSimulation { get; set; } = false;

    /// <summary>Compress stored findings using CavemanCompressionService</summary>
    public bool CompressFindings { get; set; } = true;

    /// <summary>Number of minutes before stale session is auto-archived</summary>
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours

    /// <summary>Target modules for discovery (empty = all detected)</summary>
    public List<string> TargetModules { get; set; } = new();
}
