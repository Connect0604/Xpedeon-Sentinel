namespace ValidationOrchestrator.Database;

using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// SQL Server database connector (read-only access)
/// </summary>
public class DatabaseConnector : IDatabaseConnector
{
    private readonly string _connectionString;
    private readonly DatabaseConnectionOptions _options;
    private readonly ILogger _logger;

    public DatabaseConnector(
        string connectionString,
        DatabaseConnectionOptions options,
        ILogger? logger = null)
    {
        _connectionString = connectionString;
        _options = options;
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                _logger.Information("Database connection test successful");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Database connection test failed");
            return false;
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, string clientId)
    {
        var result = new QueryResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = _options.CommandTimeout;

                    var dataAdapter = new SqlDataAdapter(command);
                    var dataSet = new DataSet();

                    await Task.Run(() => dataAdapter.Fill(dataSet));

                    result.Success = true;
                    result.Data = dataSet;
                    result.RowsAffected = dataSet.Tables[0].Rows.Count;
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.Error(ex, "Query execution failed for client {ClientId}", clientId);
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(
        string procedureName,
        string clientId,
        params DatabaseParameter[] parameters)
    {
        var result = new StoredProcedureResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = _options.CommandTimeout;

                    // Add input parameters
                    foreach (var param in parameters)
                    {
                        var sqlParam = new SqlParameter(param.Name, param.SqlType)
                        {
                            Value = param.Value ?? DBNull.Value
                        };
                        command.Parameters.Add(sqlParam);
                    }

                    var dataAdapter = new SqlDataAdapter(command);
                    var dataSet = new DataSet();

                    await Task.Run(() => dataAdapter.Fill(dataSet));

                    result.Success = true;
                    result.Data = dataSet;

                    // Capture output parameters
                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Output ||
                            param.Direction == ParameterDirection.InputOutput)
                        {
                            result.OutputParameters[param.ParameterName] = param.Value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.Error(ex, "Stored procedure {ProcedureName} execution failed for client {ClientId}",
                procedureName, clientId);
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<QueryResult> GetTablesAsync(string clientId)
    {
        const string query = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
            ORDER BY TABLE_NAME";

        return await ExecuteQueryAsync(query, clientId);
    }

    public async Task<QueryResult> GetTableSchemaAsync(string tableName, string clientId)
    {
        var query = $@"
            SELECT
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = 'dbo'
            ORDER BY ORDINAL_POSITION";

        return await ExecuteQueryAsync(query, clientId);
    }

    public async Task<int> CountRecordsAsync(string tableName, string clientId)
    {
        var query = $"SELECT COUNT(*) as RecordCount FROM [{tableName}]";
        var result = await ExecuteQueryAsync(query, clientId);

        if (result.Success && result.Data?.Tables.Count > 0)
        {
            return (int)(result.Data.Tables[0].Rows[0]["RecordCount"] ?? 0);
        }

        return 0;
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }
}
