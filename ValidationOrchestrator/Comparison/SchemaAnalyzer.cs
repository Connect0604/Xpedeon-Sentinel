namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using System.Text;
using Serilog;
using ValidationOrchestrator.Database;
using ValidationOrchestrator.Models;

/// <summary>
/// Schema analyzer implementation
/// Analyzes and compares database schemas
/// </summary>
public class SchemaAnalyzer : ISchemaAnalyzer
{
    private readonly ILogger _logger;
    private SchemaAnalyzerConfiguration _config;

    public SchemaAnalyzer(ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new SchemaAnalyzerConfiguration();
        _logger.Information("Schema analyzer initialized");
    }

    public async Task<SchemaInfo> ExtractSchemaAsync(
        IDatabaseConnector connector,
        string clientId,
        string databaseName)
    {
        var stopwatch = Stopwatch.StartNew();
        var schema = new SchemaInfo
        {
            DatabaseName = databaseName,
            SchemaName = "dbo"
        };

        try
        {
            // Get all tables
            var tables = await ExtractAllTablesAsync(connector, clientId);
            schema.Tables = tables;

            _logger.Information(
                "Schema extracted: {Database} - {TableCount} tables, {ColumnCount} columns",
                databaseName,
                schema.TotalTables,
                schema.TotalColumns);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract schema for {Database}", databaseName);
        }
        finally
        {
            stopwatch.Stop();
        }

        return schema;
    }

    public async Task<TableInfo> ExtractTableAsync(
        IDatabaseConnector connector,
        string tableName,
        string clientId)
    {
        var table = new TableInfo { TableName = tableName };

        try
        {
            // Get table schema
            var schemaResult = await connector.GetTableSchemaAsync(tableName, clientId);
            if (!schemaResult.Success || schemaResult.Data == null)
            {
                return table;
            }

            // Extract column information
            var columns = new List<ColumnInfo>();
            int ordinal = 1;

            foreach (System.Data.DataRow row in schemaResult.Data.Tables[0].Rows)
            {
                var column = new ColumnInfo
                {
                    ColumnName = row["COLUMN_NAME"].ToString() ?? string.Empty,
                    DataType = row["DATA_TYPE"].ToString() ?? string.Empty,
                    IsNullable = row["IS_NULLABLE"].ToString() == "YES",
                    OrdinalPosition = ordinal++
                };

                // Try to get length/precision info
                if (row.Table.Columns.Contains("CHARACTER_MAXIMUM_LENGTH"))
                {
                    if (int.TryParse(row["CHARACTER_MAXIMUM_LENGTH"].ToString(), out var length))
                    {
                        column.MaxLength = length;
                    }
                }

                columns.Add(column);
            }

            table.Columns = columns;
            table.RecordCount = await connector.CountRecordsAsync(tableName, clientId);

            _logger.Debug(
                "Table extracted: {TableName} - {ColumnCount} columns, {RecordCount} records",
                tableName,
                table.ColumnCount,
                table.RecordCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract table: {TableName}", tableName);
        }

        return table;
    }

    public async Task<List<TableInfo>> ExtractAllTablesAsync(
        IDatabaseConnector connector,
        string clientId)
    {
        var tables = new List<TableInfo>();

        try
        {
            var tablesResult = await connector.GetTablesAsync(clientId);
            if (!tablesResult.Success || tablesResult.Data == null)
            {
                return tables;
            }

            int extracted = 0;
            foreach (System.Data.DataRow row in tablesResult.Data.Tables[0].Rows)
            {
                if (_config.MaxTablesToAnalyze > 0 && extracted >= _config.MaxTablesToAnalyze)
                {
                    break;
                }

                var tableName = row["TABLE_NAME"].ToString() ?? string.Empty;

                if (_config.IgnoreSystemTables && IsSystemTable(tableName))
                {
                    continue;
                }

                var table = await ExtractTableAsync(connector, tableName, clientId);
                tables.Add(table);
                extracted++;
            }

            _logger.Information("Extracted {TableCount} tables", extracted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract all tables");
        }

        return tables;
    }

    public async Task<SchemaAnalysisResult> AnalyzeSchemasAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SchemaAnalysisResult
        {
            LegacySchema = legacySchema,
            BlazonSchema = blazonSchema
        };

        try
        {
            // Create mapping
            result.Mapping = await CreateSchemaMappingAsync(legacySchema, blazonSchema);

            // Compare structures
            foreach (var legacyTable in legacySchema.Tables)
            {
                if (result.Mapping.TableMappings.TryGetValue(legacyTable.TableName, out var blazonTableName))
                {
                    var blazonTable = blazonSchema.Tables.FirstOrDefault(t => t.TableName == blazonTableName);
                    if (blazonTable != null)
                    {
                        var comparison = await CompareTableStructureAsync(legacyTable, blazonTable);
                        result.Differences.AddRange(
                            comparison.TypeMismatches.Select(tm => new SchemaDifference
                            {
                                DifferenceType = "ColumnTypeMismatch",
                                Table = legacyTable.TableName,
                                Column = tm.ColumnName,
                                LegacyValue = tm.LegacyType,
                                BlazonValue = tm.BlazonType,
                                Severity = tm.IsDataLossPossible ? DifferenceSeverity.Critical : DifferenceSeverity.Medium
                            })
                        );

                        // Check for missing columns
                        foreach (var missing in comparison.MissingColumns)
                        {
                            result.Differences.Add(new SchemaDifference
                            {
                                DifferenceType = "ColumnMissing",
                                Table = legacyTable.TableName,
                                Column = missing,
                                LegacyValue = "present",
                                BlazonValue = "missing",
                                Severity = DifferenceSeverity.High
                            });
                        }

                        // Check for extra columns
                        foreach (var extra in comparison.ExtraColumns)
                        {
                            result.Differences.Add(new SchemaDifference
                            {
                                DifferenceType = "ColumnExtra",
                                Table = legacyTable.TableName,
                                Column = extra,
                                LegacyValue = "missing",
                                BlazonValue = "present",
                                Severity = DifferenceSeverity.Low
                            });
                        }
                    }
                }
            }

            // Add missing tables
            foreach (var missingTable in result.Mapping.LegacyOnlyTables)
            {
                result.Differences.Add(new SchemaDifference
                {
                    DifferenceType = "TableMissing",
                    Table = missingTable,
                    LegacyValue = "present",
                    BlazonValue = "missing",
                    Severity = DifferenceSeverity.Critical
                });
            }

            // Validate compatibility
            result.Compatibility = await ValidateCompatibilityAsync(result);
            result.Success = true;

            _logger.Information(
                "Schema analysis complete: {LegacyTables} legacy, {BlazonTables} blazon, {Differences} differences",
                legacySchema.TotalTables,
                blazonSchema.TotalTables,
                result.Differences.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.Error(ex, "Schema analysis failed");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<SchemaMapping> CreateSchemaMappingAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema)
    {
        var mapping = new SchemaMapping
        {
            LegacyDatabase = legacySchema.DatabaseName,
            BlazonDatabase = blazonSchema.DatabaseName
        };

        // Map tables
        foreach (var legacyTable in legacySchema.Tables)
        {
            var matchedTable = blazonSchema.Tables.FirstOrDefault(t =>
                t.TableName.Equals(legacyTable.TableName, StringComparison.OrdinalIgnoreCase));

            if (matchedTable != null)
            {
                mapping.TableMappings[legacyTable.TableName] = matchedTable.TableName;
            }
            else
            {
                mapping.LegacyOnlyTables.Add(legacyTable.TableName);
            }
        }

        // Find extra tables in Blazor
        foreach (var blazonTable in blazonSchema.Tables)
        {
            if (!mapping.TableMappings.Values.Contains(blazonTable.TableName))
            {
                mapping.BlazonOnlyTables.Add(blazonTable.TableName);
            }
        }

        // Map columns for each table
        foreach (var kvp in mapping.TableMappings)
        {
            var legacyTable = legacySchema.Tables.First(t => t.TableName == kvp.Key);
            var blazonTable = blazonSchema.Tables.First(t => t.TableName == kvp.Value);

            var columnMapping = new Dictionary<string, string>();
            foreach (var legacyCol in legacyTable.Columns)
            {
                var matchedCol = blazonTable.Columns.FirstOrDefault(c =>
                    c.ColumnName.Equals(legacyCol.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (matchedCol != null)
                {
                    columnMapping[legacyCol.ColumnName] = matchedCol.ColumnName;
                }
            }

            mapping.ColumnMappings[kvp.Key] = columnMapping;
        }

        _logger.Information(
            "Schema mapping created: {MappedTables} tables mapped, {LegacyOnly} legacy-only, {BlazonOnly} blazor-only",
            mapping.TableMappings.Count,
            mapping.LegacyOnlyTables.Count,
            mapping.BlazonOnlyTables.Count);

        return mapping;
    }

    public async Task<TableStructureComparison> CompareTableStructureAsync(
        TableInfo legacyTable,
        TableInfo blazonTable)
    {
        var comparison = new TableStructureComparison
        {
            TableName = legacyTable.TableName,
            LegacyColumnCount = legacyTable.ColumnCount,
            BlazonColumnCount = blazonTable.ColumnCount
        };

        // Map columns
        foreach (var legacyCol in legacyTable.Columns)
        {
            var blazonCol = blazonTable.Columns.FirstOrDefault(c =>
                c.ColumnName.Equals(legacyCol.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (blazonCol != null)
            {
                comparison.ColumnMappings.Add(new ColumnMapping
                {
                    LegacyColumn = legacyCol.ColumnName,
                    BlazonColumn = blazonCol.ColumnName,
                    IsMatched = true,
                    IsMapped = true
                });

                // Check type mismatch
                if (!legacyCol.DataType.Equals(blazonCol.DataType, StringComparison.OrdinalIgnoreCase))
                {
                    var isDataLoss = IsDataLossPossible(legacyCol.DataType, blazonCol.DataType);
                    comparison.TypeMismatches.Add(new ColumnTypeMismatch
                    {
                        ColumnName = legacyCol.ColumnName,
                        LegacyType = legacyCol.DataType,
                        BlazonType = blazonCol.DataType,
                        IsDataLossPossible = isDataLoss
                    });
                }
            }
            else
            {
                comparison.MissingColumns.Add(legacyCol.ColumnName);
            }
        }

        // Find extra columns in Blazon
        foreach (var blazonCol in blazonTable.Columns)
        {
            if (!legacyTable.Columns.Any(c =>
                c.ColumnName.Equals(blazonCol.ColumnName, StringComparison.OrdinalIgnoreCase)))
            {
                comparison.ExtraColumns.Add(blazonCol.ColumnName);
            }
        }

        // Determine compatibility
        if (comparison.TypeMismatches.Any(tm => tm.IsDataLossPossible) ||
            comparison.MissingColumns.Count > 0)
        {
            comparison.Compatibility = TableStructureCompatibility.Incompatible;
        }
        else if (comparison.TypeMismatches.Count > 0 || comparison.ExtraColumns.Count > 0)
        {
            comparison.Compatibility = TableStructureCompatibility.PartiallyCompatible;
        }
        else
        {
            comparison.Compatibility = TableStructureCompatibility.FullyCompatible;
        }

        return comparison;
    }

    public async Task<SchemaCompatibility> ValidateCompatibilityAsync(
        SchemaAnalysisResult analysis)
    {
        if (analysis.CriticalDifferences > 0)
        {
            return SchemaCompatibility.Incompatible;
        }

        var highDifferences = analysis.Differences.Count(d => d.Severity == DifferenceSeverity.High);
        if (highDifferences > 0)
        {
            return SchemaCompatibility.Partial;
        }

        var hasAnyDifference = analysis.Differences.Count > 0;
        if (hasAnyDifference)
        {
            return SchemaCompatibility.Compatible;
        }

        return SchemaCompatibility.FullyCompatible;
    }

    public async Task<List<string>> CheckDataLossRisksAsync(
        SchemaAnalysisResult analysis)
    {
        var risks = new List<string>();

        foreach (var diff in analysis.Differences)
        {
            if (diff.DifferenceType == "ColumnMissing")
            {
                risks.Add($"Column {diff.Column} missing from {diff.Table} in Blazor (data loss risk)");
            }
            else if (diff.DifferenceType == "ColumnTypeMismatch" && diff.Severity == DifferenceSeverity.Critical)
            {
                risks.Add($"Column {diff.Column} type mismatch: {diff.LegacyValue} → {diff.BlazonValue} (potential data loss)");
            }
            else if (diff.DifferenceType == "TableMissing")
            {
                risks.Add($"Table {diff.Table} missing from Blazor (data loss risk)");
            }
        }

        return risks;
    }

    public async Task<List<SchemaDifference>> FindCriticalDifferencesAsync(
        SchemaAnalysisResult analysis)
    {
        return analysis.Differences
            .Where(d => d.Severity == DifferenceSeverity.Critical)
            .ToList();
    }

    public async Task<List<ColumnMapping>> GetColumnMappingSuggestionsAsync(
        TableInfo legacyTable,
        TableInfo blazonTable)
    {
        var suggestions = new List<ColumnMapping>();

        foreach (var legacyCol in legacyTable.Columns)
        {
            var suggestion = new ColumnMapping
            {
                LegacyColumn = legacyCol.ColumnName
            };

            // Exact match
            var exactMatch = blazonTable.Columns.FirstOrDefault(c =>
                c.ColumnName.Equals(legacyCol.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                suggestion.BlazonColumn = exactMatch.ColumnName;
                suggestion.IsMatched = true;
                suggestion.IsMapped = true;
                suggestion.SimilarityScore = 1.0;
            }
            else
            {
                // Find similar names
                foreach (var blazonCol in blazonTable.Columns)
                {
                    var similarity = CalculateSimilarity(legacyCol.ColumnName, blazonCol.ColumnName);
                    if (similarity >= _config.ColumnNameSimilarityThreshold)
                    {
                        suggestion.PotentialMatches.Add($"{blazonCol.ColumnName} ({similarity:P0})");
                    }
                }
            }

            suggestions.Add(suggestion);
        }

        return suggestions;
    }

    public async Task<double> CalculateSchemaSimilarityAsync(
        SchemaInfo legacySchema,
        SchemaInfo blazonSchema)
    {
        int matchedTables = 0;
        foreach (var legacyTable in legacySchema.Tables)
        {
            if (blazonSchema.Tables.Any(t =>
                t.TableName.Equals(legacyTable.TableName, StringComparison.OrdinalIgnoreCase)))
            {
                matchedTables++;
            }
        }

        var tableSimilarity = legacySchema.TotalTables > 0
            ? (double)matchedTables / legacySchema.TotalTables
            : 0;

        return tableSimilarity;
    }

    public async Task<string> GenerateSchemaReportAsync(
        SchemaAnalysisResult analysis)
    {
        var report = new StringBuilder();

        report.AppendLine("## Schema Analysis Report");
        report.AppendLine();

        report.AppendLine($"**Legacy Database:** {analysis.LegacySchema?.DatabaseName}");
        report.AppendLine($"**Blazor Database:** {analysis.BlazonSchema?.DatabaseName}");
        report.AppendLine($"**Analyzed At:** {analysis.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Execution Time:** {analysis.ExecutionTimeMs}ms");
        report.AppendLine();

        report.AppendLine("### Summary");
        report.AppendLine($"- Total Tables: {analysis.TotalTables}");
        report.AppendLine($"- Mapped Tables: {analysis.MappedTables}");
        report.AppendLine($"- Missing Tables: {analysis.MissingTables}");
        report.AppendLine($"- Extra Tables: {analysis.ExtraTables}");
        report.AppendLine($"- Total Differences: {analysis.Differences.Count}");
        report.AppendLine($"- Critical Issues: {analysis.CriticalDifferences}");
        report.AppendLine($"- Compatibility: {analysis.Compatibility}");
        report.AppendLine();

        if (analysis.Differences.Count > 0)
        {
            report.AppendLine("### Differences Found");
            foreach (var diff in analysis.Differences.OrderBy(d => d.Severity))
            {
                report.AppendLine($"- **{diff.Severity}**: {diff.DifferenceType} in {diff.Table}");
                if (!string.IsNullOrEmpty(diff.Column))
                {
                    report.AppendLine($"  - Column: {diff.Column}");
                }
                report.AppendLine($"  - Legacy: {diff.LegacyValue} → Blazor: {diff.BlazonValue}");
            }
        }

        return report.ToString();
    }

    public TableStructureComparison GetTableStructureDetails(
        TableInfo legacyTable,
        TableInfo blazonTable)
    {
        return CompareTableStructureAsync(legacyTable, blazonTable).Result;
    }

    public void Configure(SchemaAnalyzerConfiguration config)
    {
        _config = config;
        _logger.Information("Schema analyzer configured");
    }

    // Private helpers

    private bool IsSystemTable(string tableName)
    {
        return _config.SystemTablePatterns.Any(pattern =>
        {
            var regexPattern = pattern.Replace("*", ".*");
            return System.Text.RegularExpressions.Regex.IsMatch(tableName, regexPattern);
        });
    }

    private bool IsDataLossPossible(string legacyType, string blazonType)
    {
        // Numeric to string is OK, but string to numeric is risky
        if (IsNumericType(legacyType) && IsStringType(blazonType))
            return false;

        if (IsStringType(legacyType) && IsNumericType(blazonType))
            return true;

        // Large type to small type is risky
        if (GetTypeSize(legacyType) > GetTypeSize(blazonType))
            return true;

        return false;
    }

    private bool IsNumericType(string dataType)
    {
        var numeric = new[] { "int", "bigint", "smallint", "decimal", "numeric", "float", "real" };
        return numeric.Any(n => dataType.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsStringType(string dataType)
    {
        var stringTypes = new[] { "varchar", "nvarchar", "char", "nchar", "text" };
        return stringTypes.Any(s => dataType.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private int GetTypeSize(string dataType)
    {
        return dataType.ToLower() switch
        {
            "tinyint" => 1,
            "smallint" => 2,
            "int" => 4,
            "bigint" => 8,
            "float" => 8,
            "real" => 4,
            "decimal" or "numeric" => 17,
            "date" => 3,
            "datetime" => 8,
            "datetime2" => 8,
            "bit" => 1,
            _ => dataType.Contains("varchar") || dataType.Contains("nvarchar") ? 8000 : 4000
        };
    }

    private double CalculateSimilarity(string str1, string str2)
    {
        if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var s1 = str1.ToLower();
        var s2 = str2.ToLower();

        int matches = 0;
        int maxLen = Math.Max(s1.Length, s2.Length);

        for (int i = 0; i < Math.Min(s1.Length, s2.Length); i++)
        {
            if (s1[i] == s2[i])
                matches++;
        }

        return (double)matches / maxLen;
    }
}
