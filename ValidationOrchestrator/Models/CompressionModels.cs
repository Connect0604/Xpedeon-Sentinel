namespace ValidationOrchestrator.Models;

/// <summary>
/// Compression level for Caveman semantic compression
/// </summary>
public enum CompressionLevel
{
    /// <summary>NLP-based: 15-30% reduction</summary>
    Low,

    /// <summary>MLM-based: 20-30% reduction</summary>
    Medium,

    /// <summary>LLM-based: 40-58% reduction</summary>
    High
}

/// <summary>
/// Compression method mapping
/// </summary>
public static class CompressionMethods
{
    public static string ToMethodString(CompressionLevel level) => level switch
    {
        CompressionLevel.Low => "nlp",      // 15-30% reduction
        CompressionLevel.Medium => "mlm",   // 20-30% reduction
        CompressionLevel.High => "llm",     // 40-58% reduction
        _ => "llm"
    };

    public static string ToDescription(CompressionLevel level) => level switch
    {
        CompressionLevel.Low => "NLP-based compression (15-30%)",
        CompressionLevel.Medium => "MLM-based compression (20-30%)",
        CompressionLevel.High => "LLM-based compression (40-58%)",
        _ => "Unknown compression level"
    };
}

/// <summary>
/// Request to compress content
/// </summary>
public class CompressionRequest
{
    public string Content { get; set; } = string.Empty;
    public string Method { get; set; } = "llm";
    public bool PreserveNumbers { get; set; } = true;
    public bool PreserveDates { get; set; } = true;
    public int MaxLength { get; set; } = -1; // -1 = no limit
}

/// <summary>
/// Result of compression
/// </summary>
public class CompressionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string CompressedContent { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public int OriginalLength { get; set; }
    public int CompressedLength { get; set; }
    public double CompressionRatio { get; set; } // (Original - Compressed) / Original
    public double EstimatedReduction { get; set; } // Percentage saved
    public string CompressionMethod { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public DateTime CompressedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of decompression
/// </summary>
public class DecompressionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string DecompressedContent { get; set; } = string.Empty;
    public string CompressedContent { get; set; } = string.Empty;
    public int CompressedLength { get; set; }
    public int DecompressedLength { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime DecompressedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Compressed artifact with metadata
/// </summary>
public class CompressedArtifact
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalContent { get; set; } = string.Empty;
    public string CompressedContent { get; set; } = string.Empty;
    public double CompressionRatio { get; set; }
    public string CompressionMethod { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty; // Which validation phase
    public string ArtifactType { get; set; } = string.Empty; // BusinessLogic, TestCase, Result, Finding, etc.
    public bool IsDecompressed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Compression statistics
/// </summary>
public class CompressionStatistics
{
    public int TotalArtifacts { get; set; }
    public long TotalOriginalSize { get; set; }
    public long TotalCompressedSize { get; set; }
    public double AverageCompressionRatio { get; set; }
    public Dictionary<string, int> ArtifactsByType { get; set; } = new();
    public Dictionary<string, int> ArtifactsByPhase { get; set; } = new();
    public long TotalSavedBytes => TotalOriginalSize - TotalCompressedSize;
    public double TotalSavedPercentage => TotalOriginalSize > 0
        ? (double)TotalSavedBytes / TotalOriginalSize * 100
        : 0;
}
