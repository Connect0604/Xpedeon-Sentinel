namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for database schema analysis
/// Analyzes and compares schemas from legacy and Blazor databases
/// </summary>
public interface ISchemaAnalyzer
{
    // Schema Extraction
    /// <summary>
    /// Extract complete schema information from database
    /// </summary>
    Task<SchemaInfo> ExtractSchemaAsync(
        IDatabaseConnector connector,
        string clientId,
        string databaseName);

    /// <summary>
    /// Extract specific table structure
    /// </summary>
    Task<TableInfo> ExtractTableAsync(
        IDatabaseConnector connector,
        string tableName,
        string clientId);

    /// <summary>
    /// Get all tables in database
    /// </summary>
    Task<List<TableInfo>> ExtractAllTablesAsync(
        IDatabaseConnector connector,
        string clientId);

    // Schema Comparison
    /// <summary>
    /// Analyze and compare two schemas
    /// </summary>
    Task<SchemaAnalysisResult> AnalyzeSchemasAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema);

    /// <summary>
    /// Create mapping between legacy and Blazor schemas
    /// </summary>
    Task<SchemaMapping> CreateSchemaMappingAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema);

    /// <summary>
    /// Compare two tables structure
    /// </summary>
    Task<TableStructureComparison> CompareTableStructureAsync(
        TableInfo legacyTable,
        TableInfo blazonTable);

    // Schema Validation
    /// <summary>
    /// Validate schema compatibility
    /// </summary>
    Task<SchemaCompatibility> ValidateCompatibilityAsync(
        SchemaAnalysisResult analysis);

    /// <summary>
    /// Check for data loss risks
    /// </summary>
    Task<List<string>> CheckDataLossRisksAsync(
        SchemaAnalysisResult analysis);

    /// <summary>
    /// Identify critical schema issues
    /// </summary>
    Task<List<SchemaDifference>> FindCriticalDifferencesAsync(
        SchemaAnalysisResult analysis);

    // Schema Utilities
    /// <summary>
    /// Get column mapping suggestions
    /// </summary>
    Task<List<ColumnMapping>> GetColumnMappingSuggestionsAsync(
        TableInfo legacyTable,
        TableInfo blazonTable);

    /// <summary>
    /// Calculate schema similarity score
    /// </summary>
    Task<double> CalculateSchemaSimilarityAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema);

    /// <summary>
    /// Generate schema comparison report
    /// </summary>
    Task<string> GenerateSchemaReportAsync(
        SchemaAnalysisResult analysis);

    /// <summary>
    /// Get detailed table structure information
    /// </summary>
    TableStructureComparison GetTableStructureDetails(
        TableInfo legacyTable,
        TableInfo blazonTable);

    // Configuration
    /// <summary>
    /// Configure schema analyzer
    /// </summary>
    void Configure(SchemaAnalyzerConfiguration config);
}

/// <summary>
/// Configuration for schema analyzer
/// </summary>
public class SchemaAnalyzerConfiguration
{
    /// <summary>Enable automatic column mapping</summary>
    public bool EnableAutoMapping { get; set; } = true;

    /// <summary>Column name similarity threshold (0-1)</summary>
    public double ColumnNameSimilarityThreshold { get; set; } = 0.8;

    /// <summary>Enable data loss risk detection</summary>
    public bool EnableDataLossDetection { get; set; } = true;

    /// <summary>Consider column order in comparisons</summary>
    public bool ConsiderColumnOrder { get; set; } = true;

    /// <summary>Detect renamed tables/columns</summary>
    public bool DetectRenames { get; set; } = true;

    /// <summary>Ignore system tables</summary>
    public bool IgnoreSystemTables { get; set; } = true;

    /// <summary>System table patterns to ignore</summary>
    public List<string> SystemTablePatterns { get; set; } = new()
    {
        "sys*",
        "aspnet*",
        "sp_*"
    };

    /// <summary>Maximum tables to analyze (-1 = all)</summary>
    public int MaxTablesToAnalyze { get; set; } = -1;
}
