namespace ValidationOrchestrator.Comparison;

using System.Data;
using ValidationOrchestrator.Models;

/// <summary>
/// Interface for row-by-row table comparison
/// Compares actual data between legacy and Blazor systems
/// </summary>
public interface ITableComparer
{
    // Table Comparison
    /// <summary>
    /// Compare two tables row-by-row
    /// </summary>
    Task<TableComparisonResult> CompareTablesAsync(
        DataSet legacyData,
        DataSet blazonData,
        string tableName,
        SchemaMapping schemaMapping);

    /// <summary>
    /// Compare tables with column mapping
    /// </summary>
    Task<TableComparisonResult> CompareTablesAsync(
        DataSet legacyData,
        DataSet blazonData,
        string tableName,
        Dictionary<string, string> columnMapping);

    /// <summary>
    /// Batch compare multiple tables
    /// </summary>
    Task<BatchComparisonStats> CompareBatchAsync(
        Dictionary<string, (DataSet legacy, DataSet blazon)> tableDataSets,
        SchemaMapping schemaMapping);

    // Row Matching
    /// <summary>
    /// Find matching rows between tables
    /// </summary>
    Task<List<RowComparison>> MatchRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        Dictionary<string, string> columnMapping,
        RowMatchingStrategy strategy = RowMatchingStrategy.ByPrimaryKey);

    /// <summary>
    /// Identify missing rows (in legacy but not in blazon)
    /// </summary>
    Task<List<DataRow>> FindMissingRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        List<string> primaryKeyColumns);

    /// <summary>
    /// Identify extra rows (in blazon but not in legacy)
    /// </summary>
    Task<List<DataRow>> FindExtraRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        List<string> primaryKeyColumns);

    // Data Validation
    /// <summary>
    /// Validate data consistency
    /// </summary>
    Task<List<ColumnValueDifference>> ValidateRowAsync(
        DataRow legacyRow,
        DataRow blazonRow,
        Dictionary<string, string> columnMapping,
        Dictionary<string, string> columnTypes);

    /// <summary>
    /// Detect duplicate rows in table
    /// </summary>
    Task<DuplicateDetectionResult> DetectDuplicatesAsync(
        DataSet data,
        string tableName,
        List<string>? keyColumns = null);

    /// <summary>
    /// Assess data quality for table
    /// </summary>
    Task<DataQualityAssessment> AssessDataQualityAsync(
        DataSet data,
        string tableName);

    // Utilities
    /// <summary>
    /// Convert value for comparison
    /// </summary>
    string NormalizeValue(object? value, string dataType);

    /// <summary>
    /// Compare two values with tolerance
    /// </summary>
    bool CompareValues(
        string? legacyValue,
        string? blazonValue,
        string dataType);

    /// <summary>
    /// Get matching statistics
    /// </summary>
    MatchingStatistics GetStatistics(TableComparisonResult result);

    /// <summary>
    /// Configure comparer
    /// </summary>
    void Configure(TableComparisonConfig config);
}

/// <summary>
/// Matching statistics
/// </summary>
public class MatchingStatistics
{
    public long TotalRecords { get; set; }
    public long MatchedRecords { get; set; }
    public long DifferentRecords { get; set; }
    public long MissingRecords { get; set; }
    public long ExtraRecords { get; set; }
    public double MatchPercentage { get; set; }
    public Dictionary<string, long> DifferencesByColumn { get; set; } = new();
    public Dictionary<string, long> DifferencesByType { get; set; } = new();
}
