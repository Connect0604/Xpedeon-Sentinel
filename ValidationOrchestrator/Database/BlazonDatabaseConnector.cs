namespace ValidationOrchestrator.Database;

using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Blazor database connector (read-only via stored procedures)
/// </summary>
public class BlazonDatabaseConnector : IDatabaseConnector
{
    private readonly IDatabaseConnector _connector;
    private readonly ILogger _logger;

    public BlazonDatabaseConnector(
        string connectionString,
        DatabaseConnectionOptions options,
        ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _connector = new DatabaseConnector(connectionString, options, _logger);
        _logger.Information("Blazor database connector initialized");
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, string clientId)
    {
        _logger.Debug("Executing query on Blazor database for client {ClientId}", clientId);
        return await _connector.ExecuteQueryAsync(query, clientId);
    }

    public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(
        string procedureName,
        string clientId,
        params DatabaseParameter[] parameters)
    {
        _logger.Debug("Executing stored procedure {ProcedureName} on Blazor database", procedureName);
        return await _connector.ExecuteStoredProcedureAsync(procedureName, clientId, parameters);
    }

    public async Task<QueryResult> GetTablesAsync(string clientId)
    {
        _logger.Information("Fetching table list from Blazor database");
        return await _connector.GetTablesAsync(clientId);
    }

    public async Task<QueryResult> GetTableSchemaAsync(string tableName, string clientId)
    {
        _logger.Debug("Fetching schema for table {TableName} from Blazor database", tableName);
        return await _connector.GetTableSchemaAsync(tableName, clientId);
    }

    public async Task<int> CountRecordsAsync(string tableName, string clientId)
    {
        var count = await _connector.CountRecordsAsync(tableName, clientId);
        _logger.Debug("Blazor table {TableName} has {RecordCount} records", tableName, count);
        return count;
    }

    public async Task<bool> TestConnectionAsync()
    {
        _logger.Information("Testing connection to Blazor database");
        return await _connector.TestConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        await _connector.DisposeAsync();
        _logger.Information("Blazor database connector disposed");
    }
}
