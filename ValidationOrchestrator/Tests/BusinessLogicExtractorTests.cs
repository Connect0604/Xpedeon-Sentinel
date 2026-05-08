namespace ValidationOrchestrator.Tests;

using Xunit;
using FluentAssertions;
using Moq;
using ValidationOrchestrator.Comparison;
using ValidationOrchestrator.Models;

/// <summary>
/// Unit tests for BusinessLogicExtractor (Phase 1 Discovery)
/// </summary>
public class BusinessLogicExtractorTests
{
    private const string TestSessionId = "test-session-ble";
    private const string TestClientId = "test-client-ble";

    private static CodeFile MakeFile(string name, string module, string content) => new()
    {
        FilePath = $"C:/src/{module}/{name}",
        FileName = name,
        Module = module,
        Content = content,
        SizeBytes = content.Length,
        LineCount = content.Split('\n').Length
    };

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var extractor = new BusinessLogicExtractor();
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void ExtractCalculations_ShouldDetectArithmeticExpression()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("InvoiceCalc.cs", "Finance", @"
            public decimal CalculateTax(decimal amount)
            {
                decimal taxAmount = amount * 0.18m;
                return Math.Round(taxAmount, 2);
            }");

        // Act
        var items = extractor.ExtractCalculations(file);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.Type == BusinessLogicType.Calculation);
        items.Should().OnlyContain(i => i.Module == "Finance");
    }

    [Fact]
    public void ExtractValidationRules_ShouldDetectGuardClauses()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("OrderValidator.cs", "Procurement", @"
            public void ValidateOrder(Order order)
            {
                if (order.Amount <= 0)
                    throw new ArgumentException(""Amount must be positive"");
                if (string.IsNullOrEmpty(order.VendorCode))
                    throw new ArgumentException(""Vendor required"");
            }");

        // Act
        var items = extractor.ExtractValidationRules(file);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.Type == BusinessLogicType.Validation);
    }

    [Fact]
    public void ExtractWorkflows_ShouldDetectStatusSwitch()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("ApprovalWorkflow.cs", "Procurement", @"
            switch (purchaseOrder.ApprovalStatus)
            {
                case ApprovalStatus.Pending: SubmitForApproval(); break;
                case ApprovalStatus.Approved: Approve(); break;
                case ApprovalStatus.Rejected: Reject(); break;
            }");

        // Act
        var items = extractor.ExtractWorkflows(file);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.Type == BusinessLogicType.Workflow);
        items.Should().OnlyContain(i => i.IsCritical);
    }

    [Fact]
    public void ExtractBusinessRules_ShouldDetectConstants()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("BusinessRules.cs", "Finance", @"
            public class InvoiceRules
            {
                private const decimal MaxInvoiceLimit = 500000m;
                private static readonly decimal TaxRate = 0.18m;
            }");

        // Act
        var items = extractor.ExtractBusinessRules(file);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.Type == BusinessLogicType.BusinessRule);
    }

    [Fact]
    public void DetectModule_ShouldMatchByFilename()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("InvoiceService.cs", "", "// invoice logic");

        // Act
        var module = extractor.DetectModule(file);

        // Assert
        module.Should().Be("Finance");
    }

    [Fact]
    public void DetectModule_ShouldReturnGeneralWhenNoMatch()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var file = MakeFile("Utilities.cs", "", "// utilities");
        file.FilePath = "C:/src/Shared/Utilities.cs";

        // Act
        var module = extractor.DetectModule(file);

        // Assert
        module.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DeduplicateItems_ShouldRemoveSameLineItems()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var items = new List<BusinessLogicItem>
        {
            new() { SourceFile = "A.cs", LineNumber = 10, Type = BusinessLogicType.Calculation, Description = "Longer description of the calculation" },
            new() { SourceFile = "A.cs", LineNumber = 12, Type = BusinessLogicType.Calculation, Description = "Short" }, // same 5-line bucket
            new() { SourceFile = "A.cs", LineNumber = 50, Type = BusinessLogicType.Validation, Description = "Validation" }
        };

        // Act
        var deduped = extractor.DeduplicateItems(items);

        // Assert
        deduped.Count.Should().BeLessThan(items.Count);
    }

    [Fact]
    public void RankByRisk_ShouldPutCriticalFirst()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var items = new List<BusinessLogicItem>
        {
            new() { Risk = BusinessLogicRisk.Low, Name = "Low" },
            new() { Risk = BusinessLogicRisk.Critical, Name = "Critical", IsCritical = true },
            new() { Risk = BusinessLogicRisk.High, Name = "High" }
        };

        // Act
        var ranked = extractor.RankByRisk(items);

        // Assert
        ranked.First().Risk.Should().Be(BusinessLogicRisk.Critical);
        ranked.Last().Risk.Should().Be(BusinessLogicRisk.Low);
    }

    [Fact]
    public void GroupByModule_ShouldGroupCorrectly()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var items = new List<BusinessLogicItem>
        {
            new() { Module = "Finance", Name = "Tax calc" },
            new() { Module = "Finance", Name = "Invoice calc" },
            new() { Module = "HR", Name = "Payroll rule" }
        };

        // Act
        var grouped = extractor.GroupByModule(items);

        // Assert
        grouped.Should().ContainKey("Finance");
        grouped["Finance"].Should().HaveCount(2);
        grouped.Should().ContainKey("HR");
        grouped["HR"].Should().HaveCount(1);
    }

    [Fact]
    public void Configure_ShouldApplyConfig()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var config = new BusinessLogicExtractionConfig
        {
            UseAiExtraction = false,
            MaxFiles = 5
        };

        // Act & Assert — no exception thrown
        extractor.Configure(config);
    }

    [Fact]
    public async Task DiscoverCodeFilesAsync_ShouldReturnEmptyForMissingPath()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        var config = new BusinessLogicExtractionConfig();

        // Act
        var files = await extractor.DiscoverCodeFilesAsync("C:/nonexistent/path", config);

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractFromFileAsync_ShouldReturnItemsWithoutAi()
    {
        // Arrange
        var extractor = new BusinessLogicExtractor();
        extractor.Configure(new BusinessLogicExtractionConfig
        {
            UseAiExtraction = false,
            GeneratePseudoCode = false
        });

        var file = MakeFile("PayrollCalc.cs", "HR", @"
            public decimal CalculateNetSalary(decimal gross)
            {
                const decimal taxRate = 0.30m;
                decimal tax = gross * taxRate;
                return gross - tax;
            }");

        // Act
        var items = await extractor.ExtractFromFileAsync(file, TestSessionId, TestClientId);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().AllSatisfy(i => i.Module.Should().NotBeNullOrEmpty());
    }
}
