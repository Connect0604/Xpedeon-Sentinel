namespace ValidationOrchestrator.Models;

/// <summary>
/// Complete schema information for a database
/// </summary>
public class SchemaInfo
{
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public List<TableInfo> Tables { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public int TotalTables => Tables.Count;
    public int TotalColumns => Tables.Sum(t => t.Columns.Count);
    public long TotalRecordCount { get; set; }
}

/// <summary>
/// Information about a single table
/// </summary>
public class TableInfo
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<PrimaryKeyInfo> PrimaryKeys { get; set; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    public List<IndexInfo> Indexes { get; set; } = new();
    public long RecordCount { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? Description { get; set; }

    public int ColumnCount => Columns.Count;
    public int PrimaryKeyCount => PrimaryKeys.Count;
    public int ForeignKeyCount => ForeignKeys.Count;
}

/// <summary>
/// Information about a single column
/// </summary>
public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
    public int OrdinalPosition { get; set; }
    public string? Description { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
}

/// <summary>
/// Primary key information
/// </summary>
public class PrimaryKeyInfo
{
    public string KeyName { get; set; } = string.Empty;
    public List<string> ColumnNames { get; set; } = new();
}

/// <summary>
/// Foreign key information
/// </summary>
public class ForeignKeyInfo
{
    public string KeyName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
}

/// <summary>
/// Index information
/// </summary>
public class IndexInfo
{
    public string IndexName { get; set; } = string.Empty;
    public List<string> ColumnNames { get; set; } = new();
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsClustered { get; set; }
}

/// <summary>
/// Mapping between legacy and Blazor schemas
/// </summary>
public class SchemaMapping
{
    public string LegacyDatabase { get; set; } = string.Empty;
    public string BlazonDatabase { get; set; } = string.Empty;

    // Table mappings (legacy table name → blazor table name)
    public Dictionary<string, string> TableMappings { get; set; } = new();

    // Column mappings per table
    public Dictionary<string, Dictionary<string, string>> ColumnMappings { get; set; } = new();

    // Tables only in legacy
    public List<string> LegacyOnlyTables { get; set; } = new();

    // Tables only in Blazor
    public List<string> BlazonOnlyTables { get; set; } = new();

    // Schema differences found
    public List<SchemaDifference> Differences { get; set; } = new();

    public DateTime MappedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Single schema difference between systems
/// </summary>
public class SchemaDifference
{
    public string DifferenceType { get; set; } = string.Empty; // TableMissing, ColumnMissing, TypeMismatch, etc.
    public string Table { get; set; } = string.Empty;
    public string? Column { get; set; }
    public string LegacyValue { get; set; } = string.Empty;
    public string BlazonValue { get; set; } = string.Empty;
    public DifferenceSeverity Severity { get; set; } = DifferenceSeverity.Medium;
    public string? Description { get; set; }
}

/// <summary>
/// Schema difference severity
/// </summary>
public enum DifferenceSeverity
{
    Critical,    // Data loss risk, must fix
    High,        // Schema mismatch, should fix
    Medium,      // Minor difference, can work with
    Low          // Non-data difference
}

/// <summary>
/// Schema analysis result
/// </summary>
public class SchemaAnalysisResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public SchemaInfo? LegacySchema { get; set; }
    public SchemaInfo? BlazonSchema { get; set; }
    public SchemaMapping? Mapping { get; set; }
    public List<SchemaDifference> Differences { get; set; } = new();
    public SchemaCompatibility Compatibility { get; set; } = SchemaCompatibility.Unknown;
    public long ExecutionTimeMs { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Summary
    public int TotalTables => LegacySchema?.TotalTables ?? 0;
    public int MappedTables => Mapping?.TableMappings.Count ?? 0;
    public int MissingTables => Mapping?.LegacyOnlyTables.Count ?? 0;
    public int ExtraTables => Mapping?.BlazonOnlyTables.Count ?? 0;
    public int CriticalDifferences => Differences.Count(d => d.Severity == DifferenceSeverity.Critical);
}

/// <summary>
/// Schema compatibility assessment
/// </summary>
public enum SchemaCompatibility
{
    Unknown,         // Not analyzed
    FullyCompatible, // Schemas match perfectly
    Compatible,      // Minor differences, compatible
    Partial,         // Some compatibility issues
    Incompatible     // Major compatibility issues
}

/// <summary>
/// Column mapping result
/// </summary>
public class ColumnMapping
{
    public string LegacyColumn { get; set; } = string.Empty;
    public string? BlazonColumn { get; set; }
    public bool IsMatched { get; set; }
    public bool IsMapped { get; set; }
    public double SimilarityScore { get; set; } // 0-1, how similar
    public List<string> PotentialMatches { get; set; } = new();
}

/// <summary>
/// Table structure comparison result
/// </summary>
public class TableStructureComparison
{
    public string TableName { get; set; } = string.Empty;
    public int LegacyColumnCount { get; set; }
    public int BlazonColumnCount { get; set; }
    public List<ColumnMapping> ColumnMappings { get; set; } = new();
    public List<string> MissingColumns { get; set; } = new();
    public List<string> ExtraColumns { get; set; } = new();
    public List<ColumnTypeMismatch> TypeMismatches { get; set; } = new();
    public TableStructureCompatibility Compatibility { get; set; }
}

/// <summary>
/// Column type mismatch
/// </summary>
public class ColumnTypeMismatch
{
    public string ColumnName { get; set; } = string.Empty;
    public string LegacyType { get; set; } = string.Empty;
    public string BlazonType { get; set; } = string.Empty;
    public bool IsDataLossPossible { get; set; }
    public string? Recommendation { get; set; }
}

/// <summary>
/// Table structure compatibility
/// </summary>
public enum TableStructureCompatibility
{
    FullyCompatible,
    PartiallyCompatible,
    Incompatible,
    TableMissing
}
