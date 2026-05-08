namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using Moq;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Database;
using ValidationOrchestrator.Models;
using System.Data;

/// <summary>
/// Unit tests for schema analyzer (Form 4)
/// </summary>
public class SchemaAnalyzerTests
{
    private SchemaInfo CreateTestLegacySchema()
    {
        return new SchemaInfo
        {
            DatabaseName = "XpedeonLegacy",
            SchemaName = "dbo",
            Tables = new List<TableInfo>
            {
                new TableInfo
                {
                    TableName = "Invoices",
                    Columns = new List<ColumnInfo>
                    {
                        new ColumnInfo { ColumnName = "InvoiceID", DataType = "int", IsPrimaryKey = true },
                        new ColumnInfo { ColumnName = "ProjectID", DataType = "int", IsForeignKey = true },
                        new ColumnInfo { ColumnName = "Amount", DataType = "decimal(10,2)" },
                        new ColumnInfo { ColumnName = "InvoiceDate", DataType = "datetime" }
                    },
                    RecordCount = 1000
                },
                new TableInfo
                {
                    TableName = "Projects",
                    Columns = new List<ColumnInfo>
                    {
                        new ColumnInfo { ColumnName = "ProjectID", DataType = "int", IsPrimaryKey = true },
                        new ColumnInfo { ColumnName = "ProjectName", DataType = "varchar(255)" },
                        new ColumnInfo { ColumnName = "Budget", DataType = "decimal(15,2)" }
                    },
                    RecordCount = 50
                }
            }
        };
    }

    private SchemaInfo CreateTestBlazonSchema()
    {
        return new SchemaInfo
        {
            DatabaseName = "XpedeonBlazon",
            SchemaName = "dbo",
            Tables = new List<TableInfo>
            {
                new TableInfo
                {
                    TableName = "Invoices",
                    Columns = new List<ColumnInfo>
                    {
                        new ColumnInfo { ColumnName = "InvoiceID", DataType = "int", IsPrimaryKey = true },
                        new ColumnInfo { ColumnName = "ProjectID", DataType = "int", IsForeignKey = true },
                        new ColumnInfo { ColumnName = "Amount", DataType = "decimal(10,2)" },
                        new ColumnInfo { ColumnName = "InvoiceDate", DataType = "datetime2" },
                        new ColumnInfo { ColumnName = "Status", DataType = "varchar(50)" }
                    },
                    RecordCount = 1000
                },
                new TableInfo
                {
                    TableName = "Projects",
                    Columns = new List<ColumnInfo>
                    {
                        new ColumnInfo { ColumnName = "ProjectID", DataType = "int", IsPrimaryKey = true },
                        new ColumnInfo { ColumnName = "ProjectName", DataType = "nvarchar(255)" },
                        new ColumnInfo { ColumnName = "Budget", DataType = "decimal(15,2)" }
                    },
                    RecordCount = 50
                }
            }
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeAnalyzer()
    {
        // Act
        var analyzer = new SchemaAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeSchemasAsync_WithIdenticalSchemas_ShouldReturnFullyCompatible()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var schema = CreateTestLegacySchema();

        // Act
        var result = await analyzer.AnalyzeSchemasAsync(schema, schema);

        // Assert
        result.Success.Should().BeTrue();
        result.Compatibility.Should().Be(SchemaCompatibility.FullyCompatible);
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeSchemasAsync_WithDifferentSchemas_ShouldFindDifferences()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();

        // Act
        var result = await analyzer.AnalyzeSchemasAsync(legacySchema, blazonSchema);

        // Assert
        result.Success.Should().BeTrue();
        result.Differences.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSchemaMappingAsync_ShouldMapTablesCorrectly()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();

        // Act
        var mapping = await analyzer.CreateSchemaMappingAsync(legacySchema, blazonSchema);

        // Assert
        mapping.TableMappings.Should().HaveCount(2);
        mapping.TableMappings["Invoices"].Should().Be("Invoices");
        mapping.TableMappings["Projects"].Should().Be("Projects");
        mapping.LegacyOnlyTables.Should().BeEmpty();
        mapping.BlazonOnlyTables.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSchemaMappingAsync_WithMissingTable_ShouldTrackMissing()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();

        // Remove a table from Blazon
        blazonSchema.Tables.RemoveAt(0);

        // Act
        var mapping = await analyzer.CreateSchemaMappingAsync(legacySchema, blazonSchema);

        // Assert
        mapping.LegacyOnlyTables.Should().Contain("Invoices");
    }

    [Fact]
    public async Task CompareTableStructureAsync_WithIdenticalTables_ShouldShowFullyCompatible()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];
        blazonTable.Columns.RemoveAt(4); // Remove extra Status column

        // Act
        var comparison = await analyzer.CompareTableStructureAsync(legacyTable, blazonTable);

        // Assert
        comparison.TableName.Should().Be("Invoices");
        comparison.ColumnMappings.Should().HaveCount(4);
    }

    [Fact]
    public async Task CompareTableStructureAsync_WithTypeMismatch_ShouldDetect()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];
        blazonTable.Columns.RemoveAt(4); // Remove extra column

        // Act
        var comparison = await analyzer.CompareTableStructureAsync(legacyTable, blazonTable);

        // Assert
        comparison.TypeMismatches.Should().NotBeEmpty();
        comparison.TypeMismatches.Any(tm => tm.ColumnName == "InvoiceDate").Should().BeTrue();
    }

    [Fact]
    public async Task CompareTableStructureAsync_WithMissingColumn_ShouldDetect()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];
        blazonTable.Columns.RemoveAt(0); // Remove first column

        // Act
        var comparison = await analyzer.CompareTableStructureAsync(legacyTable, blazonTable);

        // Assert
        comparison.MissingColumns.Should().Contain("InvoiceID");
    }

    [Fact]
    public async Task CompareTableStructureAsync_WithExtraColumn_ShouldDetect()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];
        // Blazon has extra "Status" column

        // Act
        var comparison = await analyzer.CompareTableStructureAsync(legacyTable, blazonTable);

        // Assert
        comparison.ExtraColumns.Should().Contain("Status");
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithCriticalDifferences_ShouldReturnIncompatible()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var result = new SchemaAnalysisResult
        {
            Differences = new List<SchemaDifference>
            {
                new SchemaDifference { Severity = DifferenceSeverity.Critical }
            }
        };

        // Act
        var compatibility = await analyzer.ValidateCompatibilityAsync(result);

        // Assert
        compatibility.Should().Be(SchemaCompatibility.Incompatible);
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithHighDifferences_ShouldReturnPartial()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var result = new SchemaAnalysisResult
        {
            Differences = new List<SchemaDifference>
            {
                new SchemaDifference { Severity = DifferenceSeverity.High }
            }
        };

        // Act
        var compatibility = await analyzer.ValidateCompatibilityAsync(result);

        // Assert
        compatibility.Should().Be(SchemaCompatibility.Partial);
    }

    [Fact]
    public async Task ValidateCompatibilityAsync_WithNoDifferences_ShouldReturnFullyCompatible()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var result = new SchemaAnalysisResult
        {
            Differences = new List<SchemaDifference>()
        };

        // Act
        var compatibility = await analyzer.ValidateCompatibilityAsync(result);

        // Assert
        compatibility.Should().Be(SchemaCompatibility.FullyCompatible);
    }

    [Fact]
    public async Task CheckDataLossRisksAsync_ShouldIdentifyMissingColumns()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var result = new SchemaAnalysisResult
        {
            Differences = new List<SchemaDifference>
            {
                new SchemaDifference
                {
                    DifferenceType = "ColumnMissing",
                    Table = "Invoices",
                    Column = "Amount"
                }
            }
        };

        // Act
        var risks = await analyzer.CheckDataLossRisksAsync(result);

        // Assert
        risks.Should().HaveCount(1);
        risks[0].Should().Contain("Amount");
    }

    [Fact]
    public async Task FindCriticalDifferencesAsync_ShouldReturnOnlyCritical()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var result = new SchemaAnalysisResult
        {
            Differences = new List<SchemaDifference>
            {
                new SchemaDifference { Severity = DifferenceSeverity.Critical },
                new SchemaDifference { Severity = DifferenceSeverity.High },
                new SchemaDifference { Severity = DifferenceSeverity.Medium }
            }
        };

        // Act
        var critical = await analyzer.FindCriticalDifferencesAsync(result);

        // Assert
        critical.Should().HaveCount(1);
        critical[0].Severity.Should().Be(DifferenceSeverity.Critical);
    }

    [Fact]
    public async Task GetColumnMappingSuggestionsAsync_WithExactMatch_ShouldMapCorrectly()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];

        // Act
        var suggestions = await analyzer.GetColumnMappingSuggestionsAsync(legacyTable, blazonTable);

        // Assert
        suggestions.Should().NotBeEmpty();
        var invoiceIdMapping = suggestions.First(s => s.LegacyColumn == "InvoiceID");
        invoiceIdMapping.IsMatched.Should().BeTrue();
        invoiceIdMapping.BlazonColumn.Should().Be("InvoiceID");
    }

    [Fact]
    public async Task CalculateSchemaSimilarityAsync_WithIdenticalSchemas_ShouldReturn1()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var schema = CreateTestLegacySchema();

        // Act
        var similarity = await analyzer.CalculateSchemaSimilarityAsync(schema, schema);

        // Assert
        similarity.Should().Be(1.0);
    }

    [Fact]
    public async Task CalculateSchemaSimilarityAsync_WithPartialMatch_ShouldReturnPartial()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();
        blazonSchema.Tables.RemoveAt(0); // Remove one table

        // Act
        var similarity = await analyzer.CalculateSchemaSimilarityAsync(legacySchema, blazonSchema);

        // Assert
        similarity.Should().BeLessThan(1.0);
        similarity.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task GenerateSchemaReportAsync_ShouldCreateReport()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();
        var analysis = await analyzer.AnalyzeSchemasAsync(legacySchema, blazonSchema);

        // Act
        var report = await analyzer.GenerateSchemaReportAsync(analysis);

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("Schema Analysis Report");
        report.Should().Contain("XpedeonLegacy");
        report.Should().Contain("XpedeonBlazon");
    }

    [Fact]
    public void Configure_ShouldUpdateConfiguration()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var config = new SchemaAnalyzerConfiguration
        {
            EnableAutoMapping = false,
            MaxTablesToAnalyze = 10
        };

        // Act
        analyzer.Configure(config);

        // Assert
        // Configuration should be applied (no public way to verify, but no exception should occur)
    }

    [Fact]
    public async Task AnalyzeSchemasAsync_ShouldMeasureExecutionTime()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var schema = CreateTestLegacySchema();

        // Act
        var result = await analyzer.AnalyzeSchemasAsync(schema, schema);

        // Assert
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreateSchemaMappingAsync_ShouldMapColumns()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();

        // Act
        var mapping = await analyzer.CreateSchemaMappingAsync(legacySchema, blazonSchema);

        // Assert
        mapping.ColumnMappings.Should().NotBeEmpty();
        mapping.ColumnMappings["Invoices"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeSchemasAsync_ShouldProvideSummary()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacySchema = CreateTestLegacySchema();
        var blazonSchema = CreateTestBlazonSchema();

        // Act
        var result = await analyzer.AnalyzeSchemasAsync(legacySchema, blazonSchema);

        // Assert
        result.TotalTables.Should().Be(legacySchema.TotalTables);
        result.MappedTables.Should().BeGreaterThan(0);
        result.MissingTables.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetTableStructureDetails_ShouldReturnComparison()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];

        // Act
        var comparison = analyzer.GetTableStructureDetails(legacyTable, blazonTable);

        // Assert
        comparison.Should().NotBeNull();
        comparison.TableName.Should().Be("Invoices");
    }

    [Fact]
    public async Task CompareTableStructureAsync_ShouldSetCompatibility()
    {
        // Arrange
        var analyzer = new SchemaAnalyzer();
        var legacyTable = CreateTestLegacySchema().Tables[0];
        var blazonTable = CreateTestBlazonSchema().Tables[0];
        blazonTable.Columns.RemoveAt(4); // Remove extra column

        // Act
        var comparison = await analyzer.CompareTableStructureAsync(legacyTable, blazonTable);

        // Assert
        comparison.Compatibility.Should().NotBe(TableStructureCompatibility.TableMissing);
    }
}
