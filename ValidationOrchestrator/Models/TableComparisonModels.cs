namespace ValidationOrchestrator.Models;

using System.Data;

/// <summary>
/// Result of comparing two tables row-by-row
/// </summary>
public class TableComparisonResult
{
    public string TableName { get; set; } = string.Empty;
    public long LegacyRecordCount { get; set; }
    public long BlazonRecordCount { get; set; }
    public long MatchedRecords { get; set; }
    public long MissingRecords { get; set; }
    public long ExtraRecords { get; set; }
    public long ModifiedRecords { get; set; }
    public List<RowComparison> RowComparisons { get; set; } = new();
    public List<ColumnComparison> ColumnComparisons { get; set; } = new();
    public double MatchPercentage { get; set; }
    public ComparisonStatus Status { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public string? Error { get; set; }
}

/// <summary>
/// Comparison result for a single row
/// </summary>
public class RowComparison
{
    public int RowNumber { get; set; }
    public string? PrimaryKey { get; set; }
    public List<ColumnValueDifference> Differences { get; set; } = new();
    public bool IsMatched { get; set; }
    public int DifferenceCount => Differences.Count;
}

/// <summary>
/// Difference in a single column value
/// </summary>
public class ColumnValueDifference
{
    public string ColumnName { get; set; } = string.Empty;
    public string? LegacyValue { get; set; }
    public string? BlazonValue { get; set; }
    public string DataType { get; set; } = string.Empty;
    public bool IsTypeConversionIssue { get; set; }
    public bool IsNullMismatch { get; set; }
    public double? NumericDifference { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>
/// Per-column comparison statistics
/// </summary>
public class ColumnComparison
{
    public string ColumnName { get; set; } = string.Empty;
    public long TotalValues { get; set; }
    public long MatchedValues { get; set; }
    public long DifferentValues { get; set; }
    public long NullMismatches { get; set; }
    public long TypeConversionIssues { get; set; }
    public double MatchPercentage { get; set; }
    public string DataType { get; set; } = string.Empty;
    public List<string> SampleDifferences { get; set; } = new();
}

/// <summary>
/// Comparison status
/// </summary>
public enum ComparisonStatus
{
    Identical,          // All records match
    Compatible,         // Minor differences
    Differences,        // Significant differences
    Error,              // Comparison failed
    Incomplete          // Partial comparison
}

/// <summary>
/// Configuration for table comparison
/// </summary>
public class TableComparisonConfig
{
    /// <summary>Maximum rows to compare (-1 = all)</summary>
    public long MaxRowsToCompare { get; set; } = -1;

    /// <summary>Batch size for processing</summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>Enable numeric fuzzy matching</summary>
    public bool EnableNumericFuzzyMatch { get; set; } = true;

    /// <summary>Numeric tolerance for fuzzy matching (0.0001 = 0.01%)</summary>
    public double NumericTolerance { get; set; } = 0.0001;

    /// <summary>Ignore trailing spaces in string comparison</summary>
    public bool IgnoreTrailingSpaces { get; set; } = true;

    /// <summary>Case-insensitive string comparison</summary>
    public bool CaseSensitiveStrings { get; set; } = true;

    /// <summary>Treat NULL and empty string as equal</summary>
    public bool TreatNullAsEmpty { get; set; } = false;

    /// <summary>Stop on first error</summary>
    public bool StopOnFirstError { get; set; } = false;

    /// <summary>Sample size for differences (0 = all)</summary>
    public int SampleDifferencesSize { get; set; } = 10;
}

/// <summary>
/// Statistics for batch comparison
/// </summary>
public class BatchComparisonStats
{
    public int TotalTables { get; set; }
    public int ComparedTables { get; set; }
    public int TablesMatched { get; set; }
    public int TablesMismatched { get; set; }
    public long TotalRecordsLegacy { get; set; }
    public long TotalRecordsBlazon { get; set; }
    public long TotalMatchedRecords { get; set; }
    public long TotalDifferentRecords { get; set; }
    public long TotalMissingRecords { get; set; }
    public long TotalExtraRecords { get; set; }
    public double OverallMatchPercentage { get; set; }
    public long TotalExecutionTimeMs { get; set; }
    public DateTime CompletedAt { get; set; }
    public Dictionary<string, double> TableMatchPercentages { get; set; } = new();
}

/// <summary>
/// Row matching strategy
/// </summary>
public enum RowMatchingStrategy
{
    ByPrimaryKey,    // Match on primary key
    ByAllColumns,    // All columns must match
    ByKeyColumns,    // Only key columns must match
    Sequential       // Match by row order
}

/// <summary>
/// Duplicate detection result
/// </summary>
public class DuplicateDetectionResult
{
    public string TableName { get; set; } = string.Empty;
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public int TotalDuplicates { get; set; }
    public bool HasDuplicates => TotalDuplicates > 0;
}

/// <summary>
/// Group of duplicate rows
/// </summary>
public class DuplicateGroup
{
    public List<int> RowNumbers { get; set; } = new();
    public Dictionary<string, string> DuplicateValues { get; set; } = new();
    public int DuplicateCount => RowNumbers.Count;
}

/// <summary>
/// Data quality assessment for a table
/// </summary>
public class DataQualityAssessment
{
    public string TableName { get; set; } = string.Empty;
    public long TotalRows { get; set; }
    public long RowsWithNulls { get; set; }
    public long DuplicateRows { get; set; }
    public Dictionary<string, ColumnQuality> ColumnQualities { get; set; } = new();
    public double OverallQualityScore { get; set; }
}

/// <summary>
/// Quality metrics for a single column
/// </summary>
public class ColumnQuality
{
    public string ColumnName { get; set; } = string.Empty;
    public long NullCount { get; set; }
    public long UniqueCount { get; set; }
    public long DuplicateCount { get; set; }
    public double Completeness { get; set; } // % non-null
    public double Uniqueness { get; set; }   // % unique
    public double QualityScore { get; set; }
}
