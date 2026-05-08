namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Database;
using ValidationOrchestrator.Models;
using System.Data;

/// <summary>
/// Unit tests for database connectors (Form 1)
/// </summary>
public class DatabaseConnectorTests
{
    private const string TestConnectionString = "Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=true;";
    private readonly DatabaseConnectionOptions _defaultOptions = new()
    {
        CommandTimeout = 300,
        MaxRetries = 3,
        RetryDelayMs = 1000
    };

    [Fact]
    public void Constructor_WithValidConnectionString_ShouldInitialize()
    {
        // Arrange & Act
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLegacyConnector_ShouldInitialize()
    {
        // Arrange & Act
        var connector = new LegacyDatabaseConnector(TestConnectionString, _defaultOptions);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithBlazonConnector_ShouldInitialize()
    {
        // Arrange & Act
        var connector = new BlazonDatabaseConnector(TestConnectionString, _defaultOptions);

        // Assert
        connector.Should().NotBeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_WithInvalidConnectionString_ShouldReturnFalse()
    {
        // Arrange
        var invalidConnectionString = "Server=invalid-server;Database=invalid;";
        var connector = new DatabaseConnector(invalidConnectionString, _defaultOptions);

        // Act
        var result = await connector.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithValidQuery_ShouldReturnQueryResult()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var query = "SELECT 1 as TestColumn";
        var clientId = "test-client";

        // Act
        var result = await connector.ExecuteQueryAsync(query, clientId);

        // Assert
        result.Should().NotBeNull();
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldMeasureExecutionTime()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var query = "SELECT 1 as TestColumn";
        var clientId = "test-client";

        // Act
        var result = await connector.ExecuteQueryAsync(query, clientId);

        // Assert
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteStoredProcedureAsync_WithValidProcedure_ShouldReturnResult()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var procedureName = "sp_TestProcedure";
        var clientId = "test-client";
        var parameters = new DatabaseParameter[]
        {
            new DatabaseParameter
            {
                Name = "@param1",
                Value = "test",
                SqlType = System.Data.SqlDbType.VarChar
            }
        };

        // Act
        var result = await connector.ExecuteStoredProcedureAsync(procedureName, clientId, parameters);

        // Assert
        result.Should().NotBeNull();
        result.OutputParameters.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTablesAsync_ShouldReturnQueryResult()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var clientId = "test-client";

        // Act
        var result = await connector.GetTablesAsync(clientId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTableSchemaAsync_WithValidTableName_ShouldReturnSchema()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var tableName = "TestTable";
        var clientId = "test-client";

        // Act
        var result = await connector.GetTableSchemaAsync(tableName, clientId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CountRecordsAsync_WithValidTableName_ShouldReturnCount()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);
        var tableName = "TestTable";
        var clientId = "test-client";

        // Act
        var count = await connector.CountRecordsAsync(tableName, clientId);

        // Assert
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var connector = new DatabaseConnector(TestConnectionString, _defaultOptions);

        // Act & Assert
        await connector.DisposeAsync();
    }

    [Fact]
    public async Task LegacyDatabaseConnector_ExecuteQuery_ShouldDelegate()
    {
        // Arrange
        var connector = new LegacyDatabaseConnector(TestConnectionString, _defaultOptions);
        var query = "SELECT 1 as TestColumn";
        var clientId = "test-client";

        // Act
        var result = await connector.ExecuteQueryAsync(query, clientId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task BlazonDatabaseConnector_ExecuteQuery_ShouldDelegate()
    {
        // Arrange
        var connector = new BlazonDatabaseConnector(TestConnectionString, _defaultOptions);
        var query = "SELECT 1 as TestColumn";
        var clientId = "test-client";

        // Act
        var result = await connector.ExecuteQueryAsync(query, clientId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void QueryResult_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var result = new QueryResult();

        // Assert
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Data.Should().BeNull();
        result.Error.Should().BeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void StoredProcedureResult_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var result = new StoredProcedureResult();

        // Assert
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Data.Should().BeNull();
        result.Error.Should().BeNull();
        result.Success.Should().BeFalse();
        result.OutputParameters.Should().BeEmpty();
    }
}
