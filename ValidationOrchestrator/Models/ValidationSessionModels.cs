namespace ValidationOrchestrator.Models;

/// <summary>
/// Complete validation session representing the state across all 6 phases
/// </summary>
public class ValidationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClientId { get; set; } = string.Empty;
    public string? ClientName { get; set; }

    // Session timing
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Phase tracking
    public ValidationPhase CurrentPhase { get; set; } = ValidationPhase.Initialize;
    public Dictionary<ValidationPhase, PhaseStatus> PhaseStatuses { get; set; } = new();

    // Compressed artifacts from each phase
    public Dictionary<string, List<CompressedArtifact>> PhaseArtifacts { get; set; } = new();

    // Key findings (decompressed, in-memory for current processing)
    public DiscoveryFindings? DiscoveryFindings { get; set; }
    public List<ValidationTest> GeneratedTests { get; set; } = new();
    public ExecutionResults? ExecutionResults { get; set; }
    public List<Discrepancy> Discrepancies { get; set; } = new();
    public RiskAssessment? RiskAssessment { get; set; }

    // Session metadata
    public bool IsComplete { get; set; }
    public bool IsArchived { get; set; }
    public string? ArchiveReason { get; set; }
    public long TotalSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }

    // Configuration
    public CompressionLevel DefaultCompressionLevel { get; set; } = CompressionLevel.High;
    public string Environment { get; set; } = "Development"; // Development, Testing, Production
}

/// <summary>
/// Validation phase enumeration
/// </summary>
public enum ValidationPhase
{
    Initialize = 0,
    Discovery = 1,
    TestGeneration = 2,
    Execution = 3,
    Comparison = 4,
    ExpertReview = 5,
    Reporting = 6,
    Complete = 7
}

/// <summary>
/// Status of a validation phase
/// </summary>
public class PhaseStatus
{
    public ValidationPhase Phase { get; set; }
    public PhaseState State { get; set; } = PhaseState.Pending;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public long DurationMs { get; set; }
    public double ProgressPercentage { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Phase execution state
/// </summary>
public enum PhaseState
{
    Pending,      // Not started
    InProgress,   // Currently executing
    Completed,    // Successfully completed
    Failed,       // Failed with error
    Skipped       // Skipped intentionally
}

/// <summary>
/// Discovery phase findings
/// </summary>
public class DiscoveryFindings
{
    public int TotalBusinessLogicItems { get; set; }
    public int UndocumentedLogicItems { get; set; }
    public List<string> CriticalModules { get; set; } = new();
    public List<string> IdentifiedPatterns { get; set; } = new();
    public Dictionary<string, int> LogicByModule { get; set; } = new();
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? ExpertNotes { get; set; }
}

/// <summary>
/// Single validation test case
/// </summary>
public class ValidationTest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // HappyPath, EdgeCase, Performance, etc.
    public string? Description { get; set; }
    public Dictionary<string, object?> InputData { get; set; } = new();
    public Dictionary<string, object?> ExpectedOutput { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Execution phase results
/// </summary>
public class ExecutionResults
{
    public int TotalTestsRun { get; set; }
    public int LegacyPassedTests { get; set; }
    public int BlazonPassedTests { get; set; }
    public int FailedTests { get; set; }
    public double LegacyPassPercentage { get; set; }
    public double BlazonPassPercentage { get; set; }
    public Dictionary<string, long> PerformanceMetrics { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public long TotalExecutionTimeMs { get; set; }
}

/// <summary>
/// Single discrepancy between systems
/// </summary>
public class Discrepancy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Module { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public DiscrepancySeverity Severity { get; set; } = DiscrepancySeverity.Medium;
    public string? LegacyResult { get; set; }
    public string? BlazonResult { get; set; }
    public string? RootCause { get; set; }
    public double ImpactPercentage { get; set; } // Affects X% of users
    public bool IsResolved { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Discrepancy severity levels
/// </summary>
public enum DiscrepancySeverity
{
    Critical,   // Must fix before launch
    High,       // Should fix before launch
    Medium,     // Can fix before launch
    Low         // Can fix post-launch
}

/// <summary>
/// Risk assessment results
/// </summary>
public class RiskAssessment
{
    public double OverallRiskScore { get; set; } // 0-100, lower is better
    public double ValidationCompleteness { get; set; } // 0-100%
    public Dictionary<string, ModuleRisk> ModuleRisks { get; set; } = new();
    public List<string> CriticalFindings { get; set; } = new();
    public bool GoNoGo { get; set; } // true = Go, false = No-Go
    public string? GoNoGoReason { get; set; }
    public List<string> PreLaunchActions { get; set; } = new();
    public List<string> PostLaunchMonitoring { get; set; } = new();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string? ExpertSignoff { get; set; }
}

/// <summary>
/// Risk score for a single module
/// </summary>
public class ModuleRisk
{
    public string ModuleName { get; set; } = string.Empty;
    public double RiskScore { get; set; } // 0-100
    public int CriticalIssues { get; set; }
    public int HighIssues { get; set; }
    public int MediumIssues { get; set; }
    public int LowIssues { get; set; }
    public double DataIntegrityScore { get; set; }
    public double BusinessLogicScore { get; set; }
    public double PerformanceScore { get; set; }
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Memory context for a specific phase
/// </summary>
public class PhaseContext
{
    public ValidationPhase Phase { get; set; }
    public List<CompressedArtifact> Artifacts { get; set; } = new();
    public Dictionary<string, string> DecompressedCache { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Statistics about session memory usage
/// </summary>
public class SessionMemoryStats
{
    public int TotalPhases { get; set; }
    public int CompletedPhases { get; set; }
    public int TotalArtifacts { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public double CompressionSavings { get; set; } // Percentage
    public int CachedItems { get; set; }
    public long CacheMemoryUsage { get; set; }
    public Dictionary<string, int> ArtifactsByPhase { get; set; } = new();
    public Dictionary<string, int> ArtifactsByType { get; set; } = new();
}
