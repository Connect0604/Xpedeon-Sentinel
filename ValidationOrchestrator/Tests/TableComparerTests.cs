namespace ValidationOrchestrator.Tests;

using System.Data;
using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for table comparer (Form 5)
/// </summary>
public class TableComparerTests
{
    private DataSet CreateTestDataSet(string tableName, List<Dictionary<string, object>> rows)
    {
        var dataSet = new DataSet();
        var table = new DataTable(tableName);

        if (rows.Count > 0)
        {
            foreach (var column in rows[0].Keys)
            {
                table.Columns.Add(column, typeof(string));
            }
        }

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            foreach (var kvp in row)
            {
                dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
            }
            table.Rows.Add(dataRow);
        }

        dataSet.Tables.Add(table);
        return dataSet;
    }

    [Fact]
    public void Constructor_ShouldInitializeComparer()
    {
        // Act
        var comparer = new TableComparer();

        // Assert
        comparer.Should().NotBeNull();
    }

    [Fact]
    public async Task CompareTablesAsync_WithIdenticalData_ShouldShowFullMatch()
    {
        // Arrange
        var comparer = new TableComparer();
        var data = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Name", "Invoice1" }, { "Amount", "1000.00" } },
            new() { { "ID", "2" }, { "Name", "Invoice2" }, { "Amount", "2000.00" } }
        };

        var legacyDataSet = CreateTestDataSet("Invoices", data);
        var blazonDataSet = CreateTestDataSet("Invoices", data);
        var columnMapping = new Dictionary<string, string>
        {
            { "ID", "ID" },
            { "Name", "Name" },
            { "Amount", "Amount" }
        };

        // Act
        var result = await comparer.CompareTablesAsync(
            legacyDataSet,
            blazonDataSet,
            "Invoices",
            columnMapping);

        // Assert
        result.MatchPercentage.Should().Be(100);
        result.Status.Should().Be(ComparisonStatus.Identical);
        result.MatchedRecords.Should().Be(2);
    }

    [Fact]
    public async Task CompareTablesAsync_WithDifferentData_ShouldIdentifyDifferences()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Name", "Invoice1" }, { "Amount", "1000.00" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Name", "Invoice1" }, { "Amount", "999.50" } }
        };

        var legacyDataSet = CreateTestDataSet("Invoices", legacyData);
        var blazonDataSet = CreateTestDataSet("Invoices", blazonData);
        var columnMapping = new Dictionary<string, string>
        {
            { "ID", "ID" },
            { "Name", "Name" },
            { "Amount", "Amount" }
        };

        // Act
        var result = await comparer.CompareTablesAsync(
            legacyDataSet,
            blazonDataSet,
            "Invoices",
            columnMapping);

        // Assert
        result.MatchPercentage.Should().BeLessThan(100);
        result.ModifiedRecords.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompareTablesAsync_ShouldCalculateMatchPercentage()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "Match" } },
            new() { { "ID", "2" }, { "Value", "Different" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "Match" } },
            new() { { "ID", "2" }, { "Value", "Changed" } }
        };

        var legacyDataSet = CreateTestDataSet("Data", legacyData);
        var blazonDataSet = CreateTestDataSet("Data", blazonData);
        var columnMapping = new Dictionary<string, string>
        {
            { "ID", "ID" },
            { "Value", "Value" }
        };

        // Act
        var result = await comparer.CompareTablesAsync(
            legacyDataSet,
            blazonDataSet,
            "Data",
            columnMapping);

        // Assert
        result.MatchPercentage.Should().BeGreaterThan(0);
        result.MatchPercentage.Should().BeLessThan(100);
    }

    [Fact]
    public async Task FindMissingRowsAsync_ShouldIdentifyMissingRows()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } },
            new() { { "ID", "3" }, { "Value", "C" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } }
        };

        var legacyDataSet = CreateTestDataSet("Data", legacyData);
        var blazonDataSet = CreateTestDataSet("Data", blazonData);

        // Act
        var missingRows = await comparer.FindMissingRowsAsync(
            legacyDataSet,
            blazonDataSet,
            new List<string> { "ID" });

        // Assert
        missingRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindExtraRowsAsync_ShouldIdentifyExtraRows()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } }
        };

        var legacyDataSet = CreateTestDataSet("Data", legacyData);
        var blazonDataSet = CreateTestDataSet("Data", blazonData);

        // Act
        var extraRows = await comparer.FindExtraRowsAsync(
            legacyDataSet,
            blazonDataSet,
            new List<string> { "ID" });

        // Assert
        extraRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateRowAsync_WithMatchingRows_ShouldReturnNoDifferences()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Name", "Test" }, { "Value", "100" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Name", "Test" }, { "Value", "100" } }
        };

        var legacyDataSet = CreateTestDataSet("Data", legacyData);
        var blazonDataSet = CreateTestDataSet("Data", blazonData);
        var columnMapping = new Dictionary<string, string>
        {
            { "ID", "ID" },
            { "Name", "Name" },
            { "Value", "Value" }
        };
        var columnTypes = new Dictionary<string, string>
        {
            { "ID", "String" },
            { "Name", "String" },
            { "Value", "String" }
        };

        // Act
        var differences = await comparer.ValidateRowAsync(
            legacyDataSet.Tables[0].Rows[0],
            blazonDataSet.Tables[0].Rows[0],
            columnMapping,
            columnTypes);

        // Assert
        differences.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRowAsync_WithDifferentValues_ShouldIdentifyDifferences()
    {
        // Arrange
        var comparer = new TableComparer();
        var legacyData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "Old" } }
        };
        var blazonData = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "New" } }
        };

        var legacyDataSet = CreateTestDataSet("Data", legacyData);
        var blazonDataSet = CreateTestDataSet("Data", blazonData);
        var columnMapping = new Dictionary<string, string>
        {
            { "ID", "ID" },
            { "Value", "Value" }
        };
        var columnTypes = new Dictionary<string, string>
        {
            { "ID", "String" },
            { "Value", "String" }
        };

        // Act
        var differences = await comparer.ValidateRowAsync(
            legacyDataSet.Tables[0].Rows[0],
            blazonDataSet.Tables[0].Rows[0],
            columnMapping,
            columnTypes);

        // Assert
        differences.Should().HaveCount(1);
        differences[0].ColumnName.Should().Be("Value");
    }

    [Fact]
    public async Task DetectDuplicatesAsync_WithDuplicateRows_ShouldDetectThem()
    {
        // Arrange
        var comparer = new TableComparer();
        var data = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } }
        };

        var dataSet = CreateTestDataSet("Data", data);

        // Act
        var result = await comparer.DetectDuplicatesAsync(
            dataSet,
            "Data",
            new List<string> { "ID", "Value" });

        // Assert
        result.HasDuplicates.Should().BeTrue();
        result.TotalDuplicates.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DetectDuplicatesAsync_WithUniqueRows_ShouldFindNoDuplicates()
    {
        // Arrange
        var comparer = new TableComparer();
        var data = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } },
            new() { { "ID", "3" }, { "Value", "C" } }
        };

        var dataSet = CreateTestDataSet("Data", data);

        // Act
        var result = await comparer.DetectDuplicatesAsync(
            dataSet,
            "Data",
            new List<string> { "ID" });

        // Assert
        result.HasDuplicates.Should().BeFalse();
        result.TotalDuplicates.Should().Be(0);
    }

    [Fact]
    public async Task AssessDataQualityAsync_ShouldCalculateQualityMetrics()
    {
        // Arrange
        var comparer = new TableComparer();
        var data = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } },
            new() { { "ID", "2" }, { "Value", "B" } },
            new() { { "ID", "3" }, { "Value", null } }
        };

        var dataSet = CreateTestDataSet("Data", data);

        // Act
        var assessment = await comparer.AssessDataQualityAsync(dataSet, "Data");

        // Assert
        assessment.TotalRows.Should().Be(3);
        assessment.ColumnQualities.Should().NotBeEmpty();
        assessment.OverallQualityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public void NormalizeValue_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        var comparer = new TableComparer();

        // Act
        var result = comparer.NormalizeValue(null, "varchar");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void NormalizeValue_WithDBNull_ShouldReturnEmptyString()
    {
        // Arrange
        var comparer = new TableComparer();

        // Act
        var result = comparer.NormalizeValue(DBNull.Value, "varchar");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void NormalizeValue_ShouldTrimSpaces()
    {
        // Arrange
        var comparer = new TableComparer();

        // Act
        var result = comparer.NormalizeValue("  test  ", "varchar");

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void CompareValues_WithIdenticalValues_ShouldReturnTrue()
    {
        // Arrange
        var comparer = new TableComparer();

        // Act
        var result = comparer.CompareValues("value", "value", "varchar");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompareValues_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var comparer = new TableComparer();

        // Act
        var result = comparer.CompareValues("value1", "value2", "varchar");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareValues_WithNumericFuzzyMatch_ShouldTolerate()
    {
        // Arrange
        var comparer = new TableComparer();
        var config = new TableComparisonConfig { EnableNumericFuzzyMatch = true, NumericTolerance = 0.01 };
        comparer.Configure(config);

        // Act
        var result = comparer.CompareValues("100.00", "100.01", "decimal");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CompareBatchAsync_ShouldCompareMultipleTables()
    {
        // Arrange
        var comparer = new TableComparer();
        var data1 = new List<Dictionary<string, object>>
        {
            new() { { "ID", "1" }, { "Value", "A" } }
        };
        var data2 = new List<Dictionary<string, object>>
        {
            new() { { "ID", "2" }, { "Value", "B" } }
        };

        var tableDataSets = new Dictionary<string, (DataSet legacy, DataSet blazon)>
        {
            { "Table1", (CreateTestDataSet("Table1", data1), CreateTestDataSet("Table1", data1)) },
            { "Table2", (CreateTestDataSet("Table2", data2), CreateTestDataSet("Table2", data2)) }
        };

        var schemaMapping = new SchemaMapping
        {
            ColumnMappings = new Dictionary<string, Dictionary<string, string>>
            {
                { "Table1", new Dictionary<string, string> { { "ID", "ID" }, { "Value", "Value" } } },
                { "Table2", new Dictionary<string, string> { { "ID", "ID" }, { "Value", "Value" } } }
            }
        };

        // Act
        var stats = await comparer.CompareBatchAsync(tableDataSets, schemaMapping);

        // Assert
        stats.ComparedTables.Should().Be(2);
        stats.TotalTables.Should().Be(2);
    }

    [Fact]
    public void GetStatistics_ShouldReturnStats()
    {
        // Arrange
        var comparer = new TableComparer();
        var result = new TableComparisonResult
        {
            TableName = "Test",
            LegacyRecordCount = 100,
            BlazonRecordCount = 100,
            MatchedRecords = 95,
            ModifiedRecords = 5,
            MatchPercentage = 95
        };

        // Act
        var stats = comparer.GetStatistics(result);

        // Assert
        stats.TotalRecords.Should().Be(100);
        stats.MatchedRecords.Should().Be(95);
        stats.MatchPercentage.Should().Be(95);
    }

    [Fact]
    public void Configure_ShouldApplyConfiguration()
    {
        // Arrange
        var comparer = new TableComparer();
        var config = new TableComparisonConfig
        {
            EnableNumericFuzzyMatch = false,
            MaxRowsToCompare = 1000
        };

        // Act
        comparer.Configure(config);

        // Assert - No exception should occur
    }
}
