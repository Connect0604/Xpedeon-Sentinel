namespace ValidationOrchestrator.Models;

/// <summary>
/// A single piece of discovered business logic from legacy WinForms code
/// </summary>
public class BusinessLogicItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Module { get; set; } = string.Empty;
    public string SubModule { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BusinessLogicType Type { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
    public string? CodeSnippet { get; set; }
    public string? PseudoCode { get; set; }          // AI-generated plain-language description
    public List<string> InputParameters { get; set; } = new();
    public List<string> OutputValues { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<string> AffectedTables { get; set; } = new();
    public bool IsDocumented { get; set; }
    public bool IsTribalKnowledge { get; set; }      // Undocumented / not in code comments
    public bool IsCritical { get; set; }
    public BusinessLogicRisk Risk { get; set; } = BusinessLogicRisk.Medium;
    public string? ExpertConfirmation { get; set; }
    public List<string> ValidationCriteria { get; set; } = new();
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category of business logic
/// </summary>
public enum BusinessLogicType
{
    Calculation,        // e.g. tax, depreciation, billing formula
    Validation,         // field/form validation rule
    Workflow,           // approval chain, state machine
    DataTransformation, // ETL, format conversion
    BusinessRule,       // domain rule (thresholds, limits)
    Integration,        // external system call
    Reporting,          // report computation
    AccessControl,      // permission / role rule
    EdgeCase            // special handling / exception
}

/// <summary>
/// Risk level of a discovered logic item
/// </summary>
public enum BusinessLogicRisk
{
    Critical,   // Financial calc, data integrity — must match exactly
    High,       // Workflow, access control
    Medium,     // Reporting, transformation
    Low         // UI behavior, cosmetic
}

/// <summary>
/// Source code file submitted for analysis
/// </summary>
public class CodeFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int LineCount { get; set; }
    public string Language { get; set; } = "CSharp";
    public DateTime LastModified { get; set; }
    public bool IsAnalyzed { get; set; }
    public int ItemsExtracted { get; set; }
}

/// <summary>
/// Full validation matrix mapping modules × dimensions
/// </summary>
public class ValidationMatrix
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public List<ValidationMatrixEntry> Entries { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    public List<string> Dimensions { get; set; } = new();
    public int TotalEntries => Entries.Count;
    public int CriticalEntries => Entries.Count(e => e.Priority == MatrixPriority.Critical);
    public int HighEntries => Entries.Count(e => e.Priority == MatrixPriority.High);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    public double OverallCoverage { get; set; }  // 0-100%
}

/// <summary>
/// Single cell in the validation matrix: module × dimension
/// </summary>
public class ValidationMatrixEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Module { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;   // e.g. "FinancialAccuracy"
    public MatrixPriority Priority { get; set; }
    public int EstimatedTestCases { get; set; }
    public int ActualTestCases { get; set; }
    public double CoveragePercentage { get; set; }
    public bool HasDependencies { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public List<string> BusinessLogicItemIds { get; set; } = new();
    public string? Notes { get; set; }
    public bool IsComplete { get; set; }
    public List<string> AcceptanceCriteria { get; set; } = new();
}

/// <summary>
/// Priority level for matrix entries
/// </summary>
public enum MatrixPriority
{
    Critical = 0,   // Must pass — migration blocked otherwise
    High = 1,
    Medium = 2,
    Low = 3
}

/// <summary>
/// Record of a domain expert interview session
/// </summary>
public class ExpertInterviewRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string ExpertName { get; set; } = string.Empty;
    public string ExpertRole { get; set; } = string.Empty;   // Finance, Operations, Procurement, HR
    public List<string> Modules { get; set; } = new();
    public string InterviewType { get; set; } = "Structured"; // Structured, Informal, Written
    public List<ExpertFinding> Findings { get; set; } = new();
    public List<string> ConfirmedLogicIds { get; set; } = new();
    public List<string> DisputedLogicIds { get; set; } = new();
    public List<string> NewlyDiscoveredItems { get; set; } = new();
    public string? RawNotes { get; set; }
    public DateTime InterviewedAt { get; set; } = DateTime.UtcNow;
    public string? RecordedBy { get; set; }
}

/// <summary>
/// A single finding surfaced during an expert interview
/// </summary>
public class ExpertFinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Module { get; set; } = string.Empty;
    public string FindingType { get; set; } = string.Empty;   // TribalKnowledge, EdgeCase, Correction, Confirmation
    public string Description { get; set; } = string.Empty;
    public BusinessLogicRisk Risk { get; set; }
    public bool RequiresTestCase { get; set; } = true;
    public string? Example { get; set; }
}

/// <summary>
/// Module risk summary for the discovery report
/// </summary>
public class ModuleRiskSummary
{
    public string Module { get; set; } = string.Empty;
    public int TotalLogicItems { get; set; }
    public int CriticalItems { get; set; }
    public int HighRiskItems { get; set; }
    public int UndocumentedItems { get; set; }
    public int ExpertConfirmedItems { get; set; }
    public double RiskScore { get; set; }          // 0-100, higher = riskier
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> TopRisks { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// Final deliverable of Phase 1: Discovery Report
/// </summary>
public class DiscoveryReport
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    // Business logic inventory
    public List<BusinessLogicItem> AllLogicItems { get; set; } = new();
    public int TotalLogicItems => AllLogicItems.Count;
    public int CriticalItems => AllLogicItems.Count(i => i.Risk == BusinessLogicRisk.Critical);
    public int UndocumentedItems => AllLogicItems.Count(i => i.IsTribalKnowledge);

    // Module breakdown
    public List<ModuleRiskSummary> ModuleRisks { get; set; } = new();

    // Validation matrix
    public ValidationMatrix? ValidationMatrix { get; set; }

    // Expert interviews
    public List<ExpertInterviewRecord> ExpertInterviews { get; set; } = new();

    // Files analyzed
    public List<CodeFile> AnalyzedFiles { get; set; } = new();
    public int FilesAnalyzed => AnalyzedFiles.Count;

    // Key metrics
    public int ModulesDiscovered { get; set; }
    public int PatternsIdentified { get; set; }
    public int ValidationDimensionsCovered { get; set; }
    public int EstimatedTestCasesRequired { get; set; }

    // Summary
    public List<string> CriticalFindings { get; set; } = new();
    public List<string> KeyRisks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string? ExecutiveSummary { get; set; }

    // Metadata
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public long DiscoveryDurationMs { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Configuration for Phase 1 Discovery
/// </summary>
public class DiscoveryConfig
{
    /// <summary>Root path of WinForms source code to analyze</summary>
    public string SourceCodePath { get; set; } = string.Empty;

    /// <summary>File extensions to include in analysis</summary>
    public List<string> FileExtensions { get; set; } = new() { ".cs", ".vb" };

    /// <summary>Paths to exclude (test projects, generated code, etc.)</summary>
    public List<string> ExcludePaths { get; set; } = new() { "bin", "obj", "Test", ".git" };

    /// <summary>Modules to prioritize (analyzed first, weight applied)</summary>
    public List<string> PriorityModules { get; set; } = new()
    {
        "Finance", "Accounting", "Procurement", "Inventory", "HR", "Payroll"
    };

    /// <summary>Max files to analyze per run (-1 = all)</summary>
    public int MaxFilesToAnalyze { get; set; } = -1;

    /// <summary>Use Claude AI for deep logic extraction</summary>
    public bool UseAiExtraction { get; set; } = true;

    /// <summary>Minimum code snippet length to consider for extraction</summary>
    public int MinSnippetLength { get; set; } = 20;

    /// <summary>Include code snippets in output (may be large)</summary>
    public bool IncludeCodeSnippets { get; set; } = true;

    /// <summary>Generate plain-language pseudo-code for each item</summary>
    public bool GeneratePseudoCode { get; set; } = true;

    /// <summary>Validation dimensions to include in the matrix</summary>
    public List<string> ValidationDimensions { get; set; } = new()
    {
        "FinancialAccuracy",
        "InventoryConsistency",
        "WorkflowIntegrity",
        "DataIntegrity",
        "SecurityAccessControl",
        "Performance",
        "Reporting",
        "IntegrationPoints",
        "BusinessRules",
        "UserBehavior"
    };
}
