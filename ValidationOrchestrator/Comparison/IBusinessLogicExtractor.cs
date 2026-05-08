namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for extracting business logic from WinForms C# source code.
/// Combines static text pattern analysis with Claude AI for deep semantic extraction.
/// </summary>
public interface IBusinessLogicExtractor
{
    // File Analysis
    /// <summary>
    /// Analyze a single source code file and extract all business logic items
    /// </summary>
    Task<List<BusinessLogicItem>> ExtractFromFileAsync(
        CodeFile file,
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze a batch of source files in a module
    /// </summary>
    Task<List<BusinessLogicItem>> ExtractFromModuleAsync(
        List<CodeFile> files,
        string module,
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze all files across all modules in a codebase
    /// </summary>
    Task<List<BusinessLogicItem>> ExtractFromCodebaseAsync(
        List<CodeFile> files,
        string sessionId,
        string clientId,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    // Code Discovery
    /// <summary>
    /// Scan a directory path and collect code files for analysis
    /// </summary>
    Task<List<CodeFile>> DiscoverCodeFilesAsync(
        string rootPath,
        BusinessLogicExtractionConfig config);

    /// <summary>
    /// Detect which ERP module a file belongs to from its path and content
    /// </summary>
    string DetectModule(CodeFile file);

    // Pattern-based Extraction
    /// <summary>
    /// Extract calculation formulas using regex and AST patterns
    /// </summary>
    List<BusinessLogicItem> ExtractCalculations(CodeFile file);

    /// <summary>
    /// Extract validation rules (if-condition guards, validators)
    /// </summary>
    List<BusinessLogicItem> ExtractValidationRules(CodeFile file);

    /// <summary>
    /// Extract workflow transitions and state machine logic
    /// </summary>
    List<BusinessLogicItem> ExtractWorkflows(CodeFile file);

    /// <summary>
    /// Extract business rules (thresholds, approval limits, policy constants)
    /// </summary>
    List<BusinessLogicItem> ExtractBusinessRules(CodeFile file);

    // AI-powered Extraction
    /// <summary>
    /// Use Claude AI to perform deep semantic analysis and identify hidden/tribal knowledge
    /// </summary>
    Task<List<BusinessLogicItem>> ExtractWithAiAsync(
        CodeFile file,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate plain-language pseudo-code for a discovered logic item
    /// </summary>
    Task<string> GeneratePseudoCodeAsync(
        BusinessLogicItem item,
        CancellationToken cancellationToken = default);

    // Post-processing
    /// <summary>
    /// Deduplicate and merge similar items found across multiple passes
    /// </summary>
    List<BusinessLogicItem> DeduplicateItems(List<BusinessLogicItem> items);

    /// <summary>
    /// Rank items by business risk and migration impact
    /// </summary>
    List<BusinessLogicItem> RankByRisk(List<BusinessLogicItem> items);

    /// <summary>
    /// Group items by module for reporting
    /// </summary>
    Dictionary<string, List<BusinessLogicItem>> GroupByModule(List<BusinessLogicItem> items);

    // Configuration
    /// <summary>Configure extraction behavior</summary>
    void Configure(BusinessLogicExtractionConfig config);
}

/// <summary>
/// Configuration for business logic extraction
/// </summary>
public class BusinessLogicExtractionConfig
{
    /// <summary>File extensions to include</summary>
    public List<string> FileExtensions { get; set; } = new() { ".cs", ".vb" };

    /// <summary>Directory segments to exclude</summary>
    public List<string> ExcludePaths { get; set; } = new() { "bin", "obj", "Test", ".git", "Migration" };

    /// <summary>Modules to treat as highest priority</summary>
    public List<string> PriorityModules { get; set; } = new()
    {
        "Finance", "Accounting", "Procurement", "Inventory", "HR", "Payroll", "Plant"
    };

    /// <summary>Module keyword → module name mapping for auto-detection</summary>
    public Dictionary<string, string> ModuleKeywords { get; set; } = new()
    {
        { "Invoice", "Finance" },
        { "Payment", "Finance" },
        { "Tax", "Finance" },
        { "Purchase", "Procurement" },
        { "Vendor", "Procurement" },
        { "Stock", "Inventory" },
        { "Material", "Inventory" },
        { "Employee", "HR" },
        { "Payroll", "HR" },
        { "Salary", "HR" },
        { "Plant", "PlantManagement" },
        { "Subcontract", "Subcontractors" }
    };

    /// <summary>Regex patterns that signal a calculation</summary>
    public List<string> CalculationPatterns { get; set; } = new()
    {
        @"[\w\.]+\s*=\s*[\w\.]+\s*[\*\/\+\-]\s*[\w\.\(]",
        @"Math\.(Round|Floor|Ceiling|Abs)",
        @"decimal\s+\w+\s*=",
        @"\.Sum\(|\.Average\(|\.Aggregate\("
    };

    /// <summary>Regex patterns that signal a business rule</summary>
    public List<string> BusinessRulePatterns { get; set; } = new()
    {
        @"if\s*\(.*(>|<|>=|<=|==).*\)",
        @"throw new.*Exception",
        @"MessageBox\.Show",
        @"Validate\w*\(|IsValid\w*\("
    };

    /// <summary>Use Claude AI for deep extraction (slower but thorough)</summary>
    public bool UseAiExtraction { get; set; } = true;

    /// <summary>Maximum file size in KB to process (skip large generated files)</summary>
    public int MaxFileSizeKb { get; set; } = 500;

    /// <summary>Max files to analyze (-1 = unlimited)</summary>
    public int MaxFiles { get; set; } = -1;

    /// <summary>Capture raw code snippets in output</summary>
    public bool IncludeCodeSnippets { get; set; } = true;

    /// <summary>Generate pseudo-code for each item via AI</summary>
    public bool GeneratePseudoCode { get; set; } = true;
}
