namespace ValidationOrchestrator.Comparison;

using System.Data;
using System.Diagnostics;
using System.Globalization;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Table comparer implementation
/// Compares actual data between tables row-by-row
/// </summary>
public class TableComparer : ITableComparer
{
    private readonly ILogger _logger;
    private TableComparisonConfig _config;

    public TableComparer(ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new TableComparisonConfig();
        _logger.Information("Table comparer initialized");
    }

    public async Task<TableComparisonResult> CompareTablesAsync(
        DataSet legacyData,
        DataSet blazonData,
        string tableName,
        SchemaMapping schemaMapping)
    {
        // Get column mapping for this table
        var columnMapping = schemaMapping.ColumnMappings.TryGetValue(tableName, out var mapping)
            ? mapping
            : new Dictionary<string, string>();

        return await CompareTablesAsync(legacyData, blazonData, tableName, columnMapping);
    }

    public async Task<TableComparisonResult> CompareTablesAsync(
        DataSet legacyData,
        DataSet blazonData,
        string tableName,
        Dictionary<string, string> columnMapping)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TableComparisonResult { TableName = tableName };

        try
        {
            var legacyTable = legacyData.Tables[0];
            var blazonTable = blazonData.Tables[0];

            result.LegacyRecordCount = legacyTable.Rows.Count;
            result.BlazonRecordCount = blazonTable.Rows.Count;

            // Match rows
            var rowComparisons = await MatchRowsAsync(
                legacyData,
                blazonData,
                columnMapping,
                RowMatchingStrategy.ByAllColumns);

            result.RowComparisons = rowComparisons;

            // Calculate statistics
            var matched = rowComparisons.Count(r => r.IsMatched);
            var different = rowComparisons.Count(r => !r.IsMatched && r.DifferenceCount > 0);

            result.MatchedRecords = matched;
            result.ModifiedRecords = different;
            result.MissingRecords = Math.Max(0, result.LegacyRecordCount - rowComparisons.Count);
            result.ExtraRecords = Math.Max(0, result.BlazonRecordCount - rowComparisons.Count);

            if (result.LegacyRecordCount > 0)
            {
                result.MatchPercentage = (double)result.MatchedRecords / result.LegacyRecordCount * 100;
            }

            // Column statistics
            result.ColumnComparisons = CalculateColumnComparisons(
                rowComparisons,
                columnMapping,
                legacyTable);

            // Determine status
            result.Status = DetermineStatus(result);
            result.Error = null;

            _logger.Information(
                "Table comparison complete: {TableName} - {MatchPercentage:F1}% match ({MatchedRecords}/{TotalRecords})",
                tableName,
                result.MatchPercentage,
                result.MatchedRecords,
                result.LegacyRecordCount);
        }
        catch (Exception ex)
        {
            result.Status = ComparisonStatus.Error;
            result.Error = ex.Message;
            _logger.Error(ex, "Table comparison failed: {TableName}", tableName);
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<BatchComparisonStats> CompareBatchAsync(
        Dictionary<string, (DataSet legacy, DataSet blazon)> tableDataSets,
        SchemaMapping schemaMapping)
    {
        var stats = new BatchComparisonStats { TotalTables = tableDataSets.Count };
        var stopwatch = Stopwatch.StartNew();

        foreach (var kvp in tableDataSets)
        {
            var result = await CompareTablesAsync(
                kvp.Value.legacy,
                kvp.Value.blazon,
                kvp.Key,
                schemaMapping);

            stats.ComparedTables++;
            stats.TotalRecordsLegacy += result.LegacyRecordCount;
            stats.TotalRecordsBlazon += result.BlazonRecordCount;
            stats.TotalMatchedRecords += result.MatchedRecords;
            stats.TotalDifferentRecords += result.ModifiedRecords;
            stats.TotalMissingRecords += result.MissingRecords;
            stats.TotalExtraRecords += result.ExtraRecords;

            if (result.Status == ComparisonStatus.Identical)
            {
                stats.TablesMatched++;
            }
            else
            {
                stats.TablesMismatched++;
            }

            stats.TableMatchPercentages[kvp.Key] = result.MatchPercentage;
        }

        stopwatch.Stop();

        if (stats.TotalRecordsLegacy > 0)
        {
            stats.OverallMatchPercentage = (double)stats.TotalMatchedRecords / stats.TotalRecordsLegacy * 100;
        }

        stats.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        stats.CompletedAt = DateTime.UtcNow;

        _logger.Information(
            "Batch comparison complete: {TableCount} tables, {MatchPercentage:F1}% overall match",
            stats.ComparedTables,
            stats.OverallMatchPercentage);

        return stats;
    }

    public async Task<List<RowComparison>> MatchRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        Dictionary<string, string> columnMapping,
        RowMatchingStrategy strategy = RowMatchingStrategy.ByPrimaryKey)
    {
        var comparisons = new List<RowComparison>();
        var legacyTable = legacyData.Tables[0];
        var blazonTable = blazonData.Tables[0];

        var rowLimit = _config.MaxRowsToCompare > 0 ? _config.MaxRowsToCompare : long.MaxValue;
        int rowNumber = 0;

        foreach (DataRow legacyRow in legacyTable.Rows)
        {
            if (rowNumber >= rowLimit) break;
            rowNumber++;

            var rowComparison = new RowComparison { RowNumber = rowNumber };

            // Try to find matching row in Blazon
            DataRow? matchedBlazonRow = null;
            foreach (DataRow blazonRow in blazonTable.Rows)
            {
                var differences = await ValidateRowAsync(
                    legacyRow,
                    blazonRow,
                    columnMapping,
                    GetColumnTypes(legacyTable));

                if (differences.Count == 0)
                {
                    matchedBlazonRow = blazonRow;
                    rowComparison.IsMatched = true;
                    break;
                }
            }

            if (matchedBlazonRow == null)
            {
                // Find closest match for differences
                var closestMatch = FindClosestMatch(legacyRow, blazonTable, columnMapping);
                if (closestMatch != null)
                {
                    rowComparison.Differences = await ValidateRowAsync(
                        legacyRow,
                        closestMatch,
                        columnMapping,
                        GetColumnTypes(legacyTable));
                    rowComparison.IsMatched = false;
                }
            }

            comparisons.Add(rowComparison);
        }

        return comparisons;
    }

    public async Task<List<DataRow>> FindMissingRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        List<string> primaryKeyColumns)
    {
        var missingRows = new List<DataRow>();
        var legacyTable = legacyData.Tables[0];
        var blazonTable = blazonData.Tables[0];

        var blazonKeys = new HashSet<string>();
        foreach (DataRow row in blazonTable.Rows)
        {
            var key = GetKeyValue(row, primaryKeyColumns);
            blazonKeys.Add(key);
        }

        foreach (DataRow legacyRow in legacyTable.Rows)
        {
            var key = GetKeyValue(legacyRow, primaryKeyColumns);
            if (!blazonKeys.Contains(key))
            {
                missingRows.Add(legacyRow);
            }
        }

        return missingRows;
    }

    public async Task<List<DataRow>> FindExtraRowsAsync(
        DataSet legacyData,
        DataSet blazonData,
        List<string> primaryKeyColumns)
    {
        var extraRows = new List<DataRow>();
        var legacyTable = legacyData.Tables[0];
        var blazonTable = blazonData.Tables[0];

        var legacyKeys = new HashSet<string>();
        foreach (DataRow row in legacyTable.Rows)
        {
            var key = GetKeyValue(row, primaryKeyColumns);
            legacyKeys.Add(key);
        }

        foreach (DataRow blazonRow in blazonTable.Rows)
        {
            var key = GetKeyValue(blazonRow, primaryKeyColumns);
            if (!legacyKeys.Contains(key))
            {
                extraRows.Add(blazonRow);
            }
        }

        return extraRows;
    }

    public async Task<List<ColumnValueDifference>> ValidateRowAsync(
        DataRow legacyRow,
        DataRow blazonRow,
        Dictionary<string, string> columnMapping,
        Dictionary<string, string> columnTypes)
    {
        var differences = new List<ColumnValueDifference>();

        foreach (var kvp in columnMapping)
        {
            var legacyCol = kvp.Key;
            var blazonCol = kvp.Value;

            if (!legacyRow.Table.Columns.Contains(legacyCol) ||
                !blazonRow.Table.Columns.Contains(blazonCol))
            {
                continue;
            }

            var legacyValue = legacyRow[legacyCol];
            var blazonValue = blazonRow[blazonCol];
            var dataType = columnTypes.TryGetValue(legacyCol, out var type) ? type : "varchar";

            var legacyStr = NormalizeValue(legacyValue, dataType);
            var blazonStr = NormalizeValue(blazonValue, dataType);

            if (!CompareValues(legacyStr, blazonStr, dataType))
            {
                var difference = new ColumnValueDifference
                {
                    ColumnName = legacyCol,
                    LegacyValue = legacyStr,
                    BlazonValue = blazonStr,
                    DataType = dataType,
                    IsNullMismatch = (legacyValue == null) != (blazonValue == null)
                };

                // Check for numeric difference
                if (IsNumericType(dataType) &&
                    double.TryParse(legacyStr, out var legacyNum) &&
                    double.TryParse(blazonStr, out var blazonNum))
                {
                    difference.NumericDifference = Math.Abs(legacyNum - blazonNum);
                }

                differences.Add(difference);
            }
        }

        return differences;
    }

    public async Task<DuplicateDetectionResult> DetectDuplicatesAsync(
        DataSet data,
        string tableName,
        List<string>? keyColumns = null)
    {
        var result = new DuplicateDetectionResult { TableName = tableName };
        var table = data.Tables[0];

        var rowHashes = new Dictionary<string, List<int>>();

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            var hash = GetRowHash(row, keyColumns);

            if (!rowHashes.ContainsKey(hash))
            {
                rowHashes[hash] = new List<int>();
            }

            rowHashes[hash].Add(i);
        }

        foreach (var kvp in rowHashes.Where(x => x.Value.Count > 1))
        {
            var duplicateGroup = new DuplicateGroup
            {
                RowNumbers = kvp.Value,
                DuplicateValues = GetRowValues(table.Rows[kvp.Value[0]], keyColumns)
            };

            result.DuplicateGroups.Add(duplicateGroup);
            result.TotalDuplicates += kvp.Value.Count - 1;
        }

        return result;
    }

    public async Task<DataQualityAssessment> AssessDataQualityAsync(
        DataSet data,
        string tableName)
    {
        var assessment = new DataQualityAssessment { TableName = tableName };
        var table = data.Tables[0];

        assessment.TotalRows = table.Rows.Count;

        for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
        {
            var column = table.Columns[colIndex];
            var columnName = column.ColumnName;
            var nullCount = 0;
            var uniqueValues = new HashSet<string>();

            foreach (DataRow row in table.Rows)
            {
                var value = row[colIndex];

                if (value == null || value == DBNull.Value)
                {
                    nullCount++;
                }
                else
                {
                    uniqueValues.Add(value.ToString() ?? string.Empty);
                }
            }

            var quality = new ColumnQuality
            {
                ColumnName = columnName,
                NullCount = nullCount,
                UniqueCount = uniqueValues.Count,
                DuplicateCount = table.Rows.Count - nullCount - uniqueValues.Count,
                Completeness = table.Rows.Count > 0 ? (double)(table.Rows.Count - nullCount) / table.Rows.Count : 0,
                Uniqueness = table.Rows.Count > 0 ? (double)uniqueValues.Count / table.Rows.Count : 0
            };

            quality.QualityScore = (quality.Completeness + quality.Uniqueness) / 2;

            assessment.ColumnQualities[columnName] = quality;
        }

        assessment.OverallQualityScore = assessment.ColumnQualities.Values.Count > 0
            ? assessment.ColumnQualities.Values.Average(c => c.QualityScore)
            : 0;

        return assessment;
    }

    public string NormalizeValue(object? value, string dataType)
    {
        if (value == null || value == DBNull.Value)
        {
            return "";
        }

        var str = value.ToString() ?? "";

        if (_config.IgnoreTrailingSpaces && IsStringType(dataType))
        {
            str = str.Trim();
        }

        if (!_config.CaseSensitiveStrings && IsStringType(dataType))
        {
            str = str.ToLower();
        }

        return str;
    }

    public bool CompareValues(string? legacyValue, string? blazonValue, string dataType)
    {
        if (legacyValue == blazonValue)
            return true;

        if (_config.TreatNullAsEmpty &&
            ((legacyValue == "" && blazonValue == "") ||
             (legacyValue == null && blazonValue == "") ||
             (legacyValue == "" && blazonValue == null)))
            return true;

        if (_config.EnableNumericFuzzyMatch && IsNumericType(dataType))
        {
            if (double.TryParse(legacyValue, out var legacyNum) &&
                double.TryParse(blazonValue, out var blazonNum))
            {
                var difference = Math.Abs(legacyNum - blazonNum);
                var tolerance = legacyNum == 0 ? _config.NumericTolerance : Math.Abs(legacyNum * _config.NumericTolerance);
                return difference <= tolerance;
            }
        }

        return false;
    }

    public MatchingStatistics GetStatistics(TableComparisonResult result)
    {
        var stats = new MatchingStatistics
        {
            TotalRecords = result.LegacyRecordCount,
            MatchedRecords = result.MatchedRecords,
            DifferentRecords = result.ModifiedRecords,
            MissingRecords = result.MissingRecords,
            ExtraRecords = result.ExtraRecords,
            MatchPercentage = result.MatchPercentage
        };

        foreach (var column in result.ColumnComparisons)
        {
            if (column.DifferentValues > 0)
            {
                stats.DifferencesByColumn[column.ColumnName] = column.DifferentValues;
            }
        }

        return stats;
    }

    public void Configure(TableComparisonConfig config)
    {
        _config = config;
        _logger.Information("Table comparer configured");
    }

    // Private helpers

    private ComparisonStatus DetermineStatus(TableComparisonResult result)
    {
        if (result.MatchPercentage == 100)
            return ComparisonStatus.Identical;

        if (result.MatchPercentage >= 95)
            return ComparisonStatus.Compatible;

        if (result.MatchPercentage >= 80)
            return ComparisonStatus.Differences;

        return ComparisonStatus.Error;
    }

    private List<ColumnComparison> CalculateColumnComparisons(
        List<RowComparison> comparisons,
        Dictionary<string, string> columnMapping,
        DataTable legacyTable)
    {
        var columnComparisons = new List<ColumnComparison>();

        foreach (var kvp in columnMapping)
        {
            var columnName = kvp.Key;
            var comparison = new ColumnComparison
            {
                ColumnName = columnName,
                TotalValues = comparisons.Count,
                DataType = GetColumnType(legacyTable, columnName)
            };

            var differences = comparisons
                .SelectMany(r => r.Differences)
                .Where(d => d.ColumnName == columnName)
                .ToList();

            comparison.DifferentValues = differences.Count;
            comparison.MatchedValues = comparison.TotalValues - comparison.DifferentValues;
            comparison.NullMismatches = differences.Count(d => d.IsNullMismatch);
            comparison.TypeConversionIssues = differences.Count(d => d.IsTypeConversionIssue);

            if (comparison.TotalValues > 0)
            {
                comparison.MatchPercentage = (double)comparison.MatchedValues / comparison.TotalValues * 100;
            }

            comparison.SampleDifferences = differences
                .Take(_config.SampleDifferencesSize)
                .Select(d => $"{d.LegacyValue} → {d.BlazonValue}")
                .ToList();

            columnComparisons.Add(comparison);
        }

        return columnComparisons;
    }

    private DataRow? FindClosestMatch(
        DataRow legacyRow,
        DataTable blazonTable,
        Dictionary<string, string> columnMapping)
    {
        DataRow? closestMatch = null;
        int minDifferences = int.MaxValue;

        foreach (DataRow blazonRow in blazonTable.Rows)
        {
            int differenceCount = 0;

            foreach (var kvp in columnMapping)
            {
                if (!legacyRow.Table.Columns.Contains(kvp.Key) ||
                    !blazonRow.Table.Columns.Contains(kvp.Value))
                {
                    continue;
                }

                var legacyValue = legacyRow[kvp.Key]?.ToString() ?? "";
                var blazonValue = blazonRow[kvp.Value]?.ToString() ?? "";

                if (legacyValue != blazonValue)
                {
                    differenceCount++;
                }
            }

            if (differenceCount < minDifferences)
            {
                minDifferences = differenceCount;
                closestMatch = blazonRow;
            }
        }

        return closestMatch;
    }

    private string GetKeyValue(DataRow row, List<string> keyColumns)
    {
        var values = keyColumns
            .Select(col => row.Table.Columns.Contains(col) ? (row[col]?.ToString() ?? "") : "")
            .ToList();

        return string.Join("|", values);
    }

    private string GetRowHash(DataRow row, List<string>? keyColumns)
    {
        var columnsToHash = keyColumns ?? Enumerable.Range(0, row.Table.Columns.Count)
            .Select(i => row.Table.Columns[i].ColumnName)
            .ToList();

        var values = columnsToHash
            .Select(col => row.Table.Columns.Contains(col) ? (row[col]?.ToString() ?? "") : "")
            .ToList();

        var combined = string.Join("|", values);
        return combined.GetHashCode().ToString();
    }

    private Dictionary<string, string> GetRowValues(DataRow row, List<string>? columns)
    {
        var values = new Dictionary<string, string>();
        var cols = columns ?? Enumerable.Range(0, row.Table.Columns.Count)
            .Select(i => row.Table.Columns[i].ColumnName)
            .ToList();

        foreach (var col in cols)
        {
            if (row.Table.Columns.Contains(col))
            {
                values[col] = row[col]?.ToString() ?? "";
            }
        }

        return values;
    }

    private Dictionary<string, string> GetColumnTypes(DataTable table)
    {
        var types = new Dictionary<string, string>();

        foreach (DataColumn column in table.Columns)
        {
            types[column.ColumnName] = column.DataType.Name;
        }

        return types;
    }

    private string GetColumnType(DataTable table, string columnName)
    {
        return table.Columns.Contains(columnName)
            ? table.Columns[columnName].DataType.Name
            : "unknown";
    }

    private bool IsNumericType(string dataType)
    {
        var numeric = new[] { "int", "bigint", "smallint", "decimal", "numeric", "float", "real", "double", "Int32", "Int64" };
        return numeric.Any(n => dataType.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsStringType(string dataType)
    {
        var stringTypes = new[] { "varchar", "nvarchar", "char", "nchar", "text", "string" };
        return stringTypes.Any(s => dataType.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
