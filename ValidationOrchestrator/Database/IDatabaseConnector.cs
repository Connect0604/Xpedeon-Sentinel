namespace ValidationOrchestrator.Database;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for database connectors (read-only access)
/// </summary>
public interface IDatabaseConnector
{
    /// <summary>
    /// Execute a SQL query against the database
    /// </summary>
    Task<QueryResult> ExecuteQueryAsync(string query, string clientId);

    /// <summary>
    /// Execute a stored procedure with parameters
    /// </summary>
    Task<StoredProcedureResult> ExecuteStoredProcedureAsync(
        string procedureName,
        string clientId,
        params DatabaseParameter[] parameters);

    /// <summary>
    /// Get list of all tables in the database
    /// </summary>
    Task<QueryResult> GetTablesAsync(string clientId);

    /// <summary>
    /// Get schema information for a specific table
    /// </summary>
    Task<QueryResult> GetTableSchemaAsync(string tableName, string clientId);

    /// <summary>
    /// Count total records in a table
    /// </summary>
    Task<int> CountRecordsAsync(string tableName, string clientId);

    /// <summary>
    /// Test connection to database
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Close/dispose database connection
    /// </summary>
    Task DisposeAsync();
}

/// <summary>
/// Database parameter for stored procedure calls
/// </summary>
public class DatabaseParameter
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }
    public System.Data.SqlDbType SqlType { get; set; }
}
