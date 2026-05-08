namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Generates test cases from Phase 1 discovery artifacts.
/// Combines pattern templates for common ERP scenarios with Claude AI for
/// intelligent edge-case and domain-specific generation.
/// </summary>
public interface ITestCaseGeneratorService
{
    // Primary Generation
    /// <summary>
    /// Generate test cases for a single business logic item
    /// </summary>
    Task<List<TestCase>> GenerateForLogicItemAsync(
        BusinessLogicItem item,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate test cases for all entries in a validation matrix
    /// </summary>
    Task<List<TestCase>> GenerateForMatrixAsync(
        ValidationMatrix matrix,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate test cases for a specific module across all dimensions
    /// </summary>
    Task<List<TestCase>> GenerateForModuleAsync(
        string module,
        List<BusinessLogicItem> moduleItems,
        List<ValidationMatrixEntry> matrixEntries,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    // Scenario Assembly
    /// <summary>
    /// Group related test cases into end-to-end business process scenarios
    /// </summary>
    List<TestScenario> AssembleScenarios(
        List<TestCase> testCases,
        List<BusinessLogicItem> logicItems);

    // Type-specific Generation
    /// <summary>Generate happy-path test cases for an item</summary>
    List<TestCase> GenerateHappyPaths(BusinessLogicItem item, TestGenerationConfig config);

    /// <summary>Generate edge-case test cases for an item</summary>
    List<TestCase> GenerateEdgeCases(BusinessLogicItem item, TestGenerationConfig config);

    /// <summary>Generate negative / error-condition test cases</summary>
    List<TestCase> GenerateNegativeCases(BusinessLogicItem item, TestGenerationConfig config);

    /// <summary>Generate performance test cases for an item</summary>
    List<TestCase> GeneratePerformanceCases(BusinessLogicItem item, TestGenerationConfig config);

    /// <summary>
    /// Use Claude AI to generate additional intelligent test cases beyond templates
    /// </summary>
    Task<List<TestCase>> GenerateWithAiAsync(
        BusinessLogicItem item,
        List<TestCase> existingCases,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default);

    // Success Criteria
    /// <summary>
    /// Build success criteria appropriate to an item's type, module, and risk level
    /// </summary>
    SuccessCriteria BuildSuccessCriteria(
        BusinessLogicItem item,
        string dimension,
        TestGenerationConfig config);

    /// <summary>
    /// Build global success criteria for a test suite based on all items
    /// </summary>
    List<SuccessCriteria> BuildGlobalCriteria(
        List<BusinessLogicItem> items,
        TestGenerationConfig config);

    // Post-processing
    /// <summary>Remove duplicate and redundant test cases</summary>
    List<TestCase> DeduplicateCases(List<TestCase> cases);

    /// <summary>Order cases for optimal execution (Critical first, setup before teardown)</summary>
    List<TestCase> PrioritizeCases(List<TestCase> cases);

    // Configuration
    /// <summary>Configure the generator</summary>
    void Configure(TestGenerationConfig config);
}
