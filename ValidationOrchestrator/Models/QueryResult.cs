namespace ValidationOrchestrator.Models;

using System.Data;

/// <summary>
/// Result from a database query
/// </summary>
public class QueryResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DataSet? Data { get; set; }
    public int RowsAffected { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result from a stored procedure execution
/// </summary>
public class StoredProcedureResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DataSet? Data { get; set; }
    public Dictionary<string, object?> OutputParameters { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
