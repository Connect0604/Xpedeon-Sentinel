namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for Caveman semantic compression service
/// Removes predictable grammar while preserving factual content
/// </summary>
public interface ICavemanCompressionService
{
    /// <summary>
    /// Compress content using Caveman semantic compression
    /// </summary>
    Task<CompressionResult> CompressAsync(
        string content,
        CompressionLevel level = CompressionLevel.High);

    /// <summary>
    /// Decompress content (reconstruct using LLM understanding)
    /// </summary>
    Task<DecompressionResult> DecompressAsync(string compressedContent);

    /// <summary>
    /// Estimate compression ratio before compressing
    /// </summary>
    Task<double> EstimateCompressionRatioAsync(
        string content,
        CompressionLevel level = CompressionLevel.High);

    /// <summary>
    /// Batch compress multiple artifacts
    /// </summary>
    Task<List<CompressionResult>> CompressBatchAsync(
        IEnumerable<string> contents,
        CompressionLevel level = CompressionLevel.High);

    /// <summary>
    /// Batch decompress multiple artifacts
    /// </summary>
    Task<List<DecompressionResult>> DecompressBatchAsync(
        IEnumerable<string> compressedContents);

    /// <summary>
    /// Get compression statistics
    /// </summary>
    CompressionStatistics GetStatistics();

    /// <summary>
    /// Reset statistics
    /// </summary>
    void ResetStatistics();

    /// <summary>
    /// Configure compression preferences
    /// </summary>
    void Configure(CompressionConfiguration config);
}

/// <summary>
/// Configuration for compression service
/// </summary>
public class CompressionConfiguration
{
    /// <summary>Default compression level</summary>
    public CompressionLevel DefaultLevel { get; set; } = CompressionLevel.High;

    /// <summary>Minimum content length to compress (avoid compressing short strings)</summary>
    public int MinimumContentLengthToCompress { get; set; } = 100;

    /// <summary>Enable local NLP compression (no API calls)</summary>
    public bool EnableLocalCompression { get; set; } = true;

    /// <summary>Cache compressed results</summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>Cache size limit</summary>
    public int CacheSizeLimit { get; set; } = 1000;

    /// <summary>Preserve numbers during compression</summary>
    public bool PreserveNumbers { get; set; } = true;

    /// <summary>Preserve dates during compression</summary>
    public bool PreserveDates { get; set; } = true;

    /// <summary>Preserve technical terms (SQL keywords, etc.)</summary>
    public bool PreserveTechnicalTerms { get; set; } = true;
}
