namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for Caveman compression service (Form 2)
/// </summary>
public class CavemanCompressionServiceTests
{
    private const string SampleContent = "The quick brown fox jumps over the lazy dog. This is a test content for compression.";
    private const string LongContent = @"
        The Xpedeon Construction ERP system is designed to handle complex business logic
        and data management for construction companies. The system manages procurement,
        inventory, accounting, plant management, subcontractor management, and HR payroll.
        It is very important that we validate the migration from WinForms to Blazor thoroughly
        to ensure that no business logic is lost. The validation orchestrator will help us
        find any discrepancies between the old and new systems.
    ";

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act
        var service = new CavemanCompressionService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task CompressAsync_WithValidContent_ShouldReturnCompressedResult()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(SampleContent, CompressionLevel.High);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.CompressedContent.Should().NotBeNullOrEmpty();
        result.OriginalLength.Should().Be(SampleContent.Length);
    }

    [Fact]
    public async Task CompressAsync_WithHighLevel_ShouldCompressMore()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var resultLow = await service.CompressAsync(LongContent, CompressionLevel.Low);
        var resultMedium = await service.CompressAsync(LongContent, CompressionLevel.Medium);
        var resultHigh = await service.CompressAsync(LongContent, CompressionLevel.High);

        // Assert
        resultLow.CompressedLength.Should().BeGreaterThan(resultMedium.CompressedLength);
        resultMedium.CompressedLength.Should().BeGreaterThan(resultHigh.CompressedLength);
    }

    [Fact]
    public async Task CompressAsync_ShouldCalculateCompressionRatio()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(LongContent, CompressionLevel.High);

        // Assert
        result.CompressionRatio.Should().BeGreaterThan(0);
        result.CompressionRatio.Should().BeLessThanOrEqualTo(1);
        result.EstimatedReduction.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompressAsync_WithEmptyContent_ShouldReturnSuccess()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(string.Empty);

        // Assert
        result.Success.Should().BeTrue();
        result.CompressedContent.Should().BeEmpty();
    }

    [Fact]
    public async Task CompressAsync_WithNullContent_ShouldReturnSuccess()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(null!);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CompressAsync_WithShortContent_ShouldNotCompress()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var shortContent = "Hello";

        // Act
        var result = await service.CompressAsync(shortContent);

        // Assert
        result.CompressedContent.Should().Be(shortContent);
        result.CompressionRatio.Should().Be(0);
    }

    [Fact]
    public async Task CompressAsync_ShouldTrackExecutionTime()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(LongContent);

        // Assert
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DecompressAsync_WithValidContent_ShouldReturnDecompressed()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var compressed = await service.CompressAsync(SampleContent, CompressionLevel.High);

        // Act
        var result = await service.DecompressAsync(compressed.CompressedContent);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DecompressedContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DecompressAsync_WithEmptyContent_ShouldReturnSuccess()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.DecompressAsync(string.Empty);

        // Assert
        result.Success.Should().BeTrue();
        result.DecompressedContent.Should().BeEmpty();
    }

    [Fact]
    public async Task EstimateCompressionRatioAsync_WithHighLevel_ShouldReturnHighEstimate()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var estimateHigh = await service.EstimateCompressionRatioAsync(LongContent, CompressionLevel.High);
        var estimateMedium = await service.EstimateCompressionRatioAsync(LongContent, CompressionLevel.Medium);
        var estimateLow = await service.EstimateCompressionRatioAsync(LongContent, CompressionLevel.Low);

        // Assert
        estimateHigh.Should().BeGreaterThan(estimateMedium);
        estimateMedium.Should().BeGreaterThanOrEqualTo(estimateLow);
    }

    [Fact]
    public async Task CompressBatchAsync_WithMultipleItems_ShouldCompressAll()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var contents = new[] { SampleContent, LongContent, "Another test content for compression" };

        // Act
        var results = await service.CompressBatchAsync(contents, CompressionLevel.High);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task DecompressBatchAsync_WithMultipleItems_ShouldDecompressAll()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var contents = new[] { SampleContent, LongContent };
        var compressed = await service.CompressBatchAsync(contents, CompressionLevel.High);
        var compressedContents = compressed.Select(r => r.CompressedContent);

        // Act
        var results = await service.DecompressBatchAsync(compressedContents);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public void GetStatistics_ShouldReturnValidStatistics()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var stats = service.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalArtifacts.Should().Be(0);
        stats.TotalOriginalSize.Should().Be(0);
        stats.TotalCompressedSize.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistics_AfterCompression_ShouldUpdateCounters()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        await service.CompressAsync(LongContent, CompressionLevel.High);
        var stats = service.GetStatistics();

        // Assert
        stats.TotalArtifacts.Should().Be(1);
        stats.TotalOriginalSize.Should().BeGreaterThan(0);
        stats.TotalCompressedSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetStatistics_ShouldCalculateAverageCompressionRatio()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        await service.CompressAsync(LongContent, CompressionLevel.High);
        await service.CompressAsync(SampleContent, CompressionLevel.High);
        var stats = service.GetStatistics();

        // Assert
        stats.AverageCompressionRatio.Should().BeGreaterThan(0);
        stats.AverageCompressionRatio.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetStatistics_ShouldCalculateTotalSavedBytes()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        await service.CompressAsync(LongContent, CompressionLevel.High);
        var stats = service.GetStatistics();

        // Assert
        stats.TotalSavedBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetStatistics_ShouldCalculateTotalSavedPercentage()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        await service.CompressAsync(LongContent, CompressionLevel.High);
        var stats = service.GetStatistics();

        // Assert
        stats.TotalSavedPercentage.Should().BeGreaterThan(0);
        stats.TotalSavedPercentage.Should().BeLessThan(100);
    }

    [Fact]
    public void ResetStatistics_ShouldClearAllCounters()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var _ = service.CompressAsync(LongContent).Result;

        // Act
        service.ResetStatistics();
        var stats = service.GetStatistics();

        // Assert
        stats.TotalArtifacts.Should().Be(0);
        stats.TotalOriginalSize.Should().Be(0);
        stats.TotalCompressedSize.Should().Be(0);
    }

    [Fact]
    public void Configure_ShouldUpdateConfiguration()
    {
        // Arrange
        var service = new CavemanCompressionService();
        var config = new CompressionConfiguration
        {
            DefaultLevel = CompressionLevel.Medium,
            MinimumContentLengthToCompress = 200
        };

        // Act
        service.Configure(config);

        // Assert - service should use new config
        var shortContent = "Short";
        var result = service.CompressAsync(shortContent).Result;
        result.CompressedContent.Should().Be(shortContent); // Should not compress short content
    }

    [Fact]
    public async Task CompressionResult_ShouldPreserveOriginalContent()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var result = await service.CompressAsync(LongContent);

        // Assert
        result.OriginalContent.Should().Be(LongContent);
    }

    [Fact]
    public async Task CompressionLevel_Low_ShouldCompressLess()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var resultLow = await service.CompressAsync(LongContent, CompressionLevel.Low);

        // Assert
        resultLow.EstimatedReduction.Should().BeLessThan(40); // Low should be 15-30%
    }

    [Fact]
    public async Task CompressionLevel_Medium_ShouldCompressMedium()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var resultMedium = await service.CompressAsync(LongContent, CompressionLevel.Medium);

        // Assert
        resultMedium.EstimatedReduction.Should().BeGreaterThan(15);
        resultMedium.EstimatedReduction.Should().BeLessThan(35);
    }

    [Fact]
    public async Task CompressionLevel_High_ShouldCompressMore()
    {
        // Arrange
        var service = new CavemanCompressionService();

        // Act
        var resultHigh = await service.CompressAsync(LongContent, CompressionLevel.High);

        // Assert
        resultHigh.EstimatedReduction.Should().BeGreaterThan(30); // High should be 40-58%
    }
}
