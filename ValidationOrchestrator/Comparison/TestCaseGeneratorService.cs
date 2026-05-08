namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using Anthropic;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Generates test cases from Phase 1 discovery artifacts.
/// Uses ERP-domain templates for common scenarios and Claude AI for deep edge-case generation.
/// </summary>
public class TestCaseGeneratorService : ITestCaseGeneratorService
{
    private readonly ILogger _logger;
    private readonly AnthropicApi _anthropic;
    private TestGenerationConfig _config;

    public TestCaseGeneratorService(AnthropicApi? anthropic = null, ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _anthropic = anthropic ?? new AnthropicApi();
        _config = new TestGenerationConfig();
        _logger.Information("TestCaseGeneratorService initialized");
    }

    public async Task<List<TestCase>> GenerateForLogicItemAsync(
        BusinessLogicItem item,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var cases = new List<TestCase>();

        if (config.GenerateHappyPaths)
            cases.AddRange(GenerateHappyPaths(item, config));

        if (config.GenerateEdgeCases)
            cases.AddRange(GenerateEdgeCases(item, config));

        if (config.GenerateNegativeCases)
            cases.AddRange(GenerateNegativeCases(item, config));

        if (config.GeneratePerformanceCases && item.Risk <= BusinessLogicRisk.High)
            cases.AddRange(GeneratePerformanceCases(item, config));

        if (config.UseAiGeneration && !cancellationToken.IsCancellationRequested)
        {
            var aiCases = await GenerateWithAiAsync(item, cases, config, cancellationToken);
            cases.AddRange(aiCases);
        }

        // Tag each case with source item
        cases.ForEach(c => c.BusinessLogicItemId = item.Id);

        return PrioritizeCases(DeduplicateCases(cases));
    }

    public async Task<List<TestCase>> GenerateForMatrixAsync(
        ValidationMatrix matrix,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allCases = new List<TestCase>();
        var itemById = logicItems.ToDictionary(i => i.Id);
        int done = 0;

        // Process Critical entries first
        var orderedEntries = matrix.Entries
            .OrderBy(e => e.Priority)
            .ToList();

        foreach (var entry in orderedEntries)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var relatedItems = entry.BusinessLogicItemIds
                .Where(itemById.ContainsKey)
                .Select(id => itemById[id])
                .ToList();

            var moduleCases = await GenerateForModuleAsync(
                entry.Module, relatedItems, new[] { entry }.ToList(), config, cancellationToken);

            // Tag with matrix entry
            moduleCases.ForEach(c => { c.MatrixEntryId = entry.Id; c.Dimension = entry.Dimension; });
            allCases.AddRange(moduleCases);

            done++;
            progress?.Report((int)((double)done / orderedEntries.Count * 100));
        }

        _logger.Information("Generated {Count} test cases from validation matrix", allCases.Count);
        return PrioritizeCases(DeduplicateCases(allCases));
    }

    public async Task<List<TestCase>> GenerateForModuleAsync(
        string module,
        List<BusinessLogicItem> moduleItems,
        List<ValidationMatrixEntry> matrixEntries,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var cases = new List<TestCase>();

        foreach (var item in moduleItems)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var itemCases = await GenerateForLogicItemAsync(item, config, cancellationToken);
            cases.AddRange(itemCases);
        }

        // If no logic items but matrix entries exist, generate from entry metadata
        if (!moduleItems.Any() && matrixEntries.Any())
        {
            foreach (var entry in matrixEntries)
            {
                cases.AddRange(GenerateFromMatrixEntry(entry, config));
            }
        }

        return cases;
    }

    public List<TestScenario> AssembleScenarios(
        List<TestCase> testCases,
        List<BusinessLogicItem> logicItems)
    {
        var scenarios = new List<TestScenario>();
        var byModule = testCases.GroupBy(c => c.Module);

        foreach (var moduleGroup in byModule)
        {
            var module = moduleGroup.Key;
            var moduleCases = moduleGroup.ToList();

            // End-to-end scenario: Critical + High cases for the module
            var e2eCases = moduleCases
                .Where(c => c.Priority <= TestCasePriority.High)
                .OrderBy(c => c.Priority)
                .ToList();

            if (e2eCases.Any())
            {
                scenarios.Add(new TestScenario
                {
                    Name = $"{module} — End-to-End Validation",
                    Module = module,
                    Description = $"Critical and high-priority validation of {module} module",
                    Type = ScenarioType.EndToEnd,
                    TestCases = e2eCases,
                    Priority = TestCasePriority.Critical,
                    BusinessProcess = module
                });
            }

            // Edge-case scenario
            var edgeCases = moduleCases
                .Where(c => c.Type == TestCaseType.EdgeCase || c.Type == TestCaseType.NegativeCase)
                .ToList();

            if (edgeCases.Any())
            {
                scenarios.Add(new TestScenario
                {
                    Name = $"{module} — Edge Cases & Boundary Conditions",
                    Module = module,
                    Description = $"Boundary values and error conditions for {module}",
                    Type = ScenarioType.Unit,
                    TestCases = edgeCases,
                    Priority = TestCasePriority.High
                });
            }

            // Performance scenario
            var perfCases = moduleCases
                .Where(c => c.Type == TestCaseType.Performance)
                .ToList();

            if (perfCases.Any())
            {
                scenarios.Add(new TestScenario
                {
                    Name = $"{module} — Performance Baseline",
                    Module = module,
                    Description = $"Response time and throughput validation for {module}",
                    Type = ScenarioType.Performance,
                    TestCases = perfCases,
                    Priority = TestCasePriority.Medium
                });
            }
        }

        return scenarios;
    }

    public List<TestCase> GenerateHappyPaths(BusinessLogicItem item, TestGenerationConfig config)
    {
        var cases = new List<TestCase>();
        var count = TargetCount(item.Risk, config) / 3 + 1;

        for (int i = 0; i < count; i++)
        {
            cases.Add(new TestCase
            {
                Name = $"[HAPPY] {item.Module} — {item.Name} — scenario {i + 1}",
                Module = item.Module,
                Type = TestCaseType.HappyPath,
                Priority = MapRiskToPriority(item.Risk),
                Description = $"Happy path: {item.Description}",
                SuccessCriteria = BuildSuccessCriteria(item, item.Type.ToString(), config),
                SqlQuery = BuildHappyPathQuery(item, i),
                GeneratedFrom = "Pattern",
                GenerationRationale = $"Happy path coverage for {item.Type} logic item",
                Tags = new() { item.Module, item.Type.ToString(), "HappyPath" }
            });
        }

        return cases;
    }

    public List<TestCase> GenerateEdgeCases(BusinessLogicItem item, TestGenerationConfig config)
    {
        var cases = new List<TestCase>();
        var edgeScenarios = GetEdgeScenariosFor(item);

        foreach (var (name, description, sql) in edgeScenarios)
        {
            cases.Add(new TestCase
            {
                Name = $"[EDGE] {item.Module} — {item.Name} — {name}",
                Module = item.Module,
                Type = TestCaseType.EdgeCase,
                Priority = item.Risk == BusinessLogicRisk.Critical
                    ? TestCasePriority.Critical
                    : TestCasePriority.High,
                Description = description,
                SuccessCriteria = BuildSuccessCriteria(item, item.Type.ToString(), config),
                SqlQuery = sql,
                GeneratedFrom = "Pattern",
                GenerationRationale = $"Edge case: {name}",
                Tags = new() { item.Module, item.Type.ToString(), "EdgeCase", name }
            });
        }

        return cases;
    }

    public List<TestCase> GenerateNegativeCases(BusinessLogicItem item, TestGenerationConfig config)
    {
        var cases = new List<TestCase>();

        var negativeScenarios = new[]
        {
            ("NullInput", "Null/empty required fields", "-- Test with NULL required fields"),
            ("ZeroAmount", "Zero or negative monetary amounts", "-- Test with 0 or negative amounts"),
            ("InvalidDate", "Date out of valid range", "-- Test with invalid date range"),
            ("ExceedsLimit", "Value exceeds business rule limit", "-- Test with value exceeding limit")
        };

        foreach (var (tag, desc, sql) in negativeScenarios)
        {
            if (item.Type == BusinessLogicType.AccessControl && tag == "ZeroAmount") continue;

            cases.Add(new TestCase
            {
                Name = $"[NEG] {item.Module} — {item.Name} — {tag}",
                Module = item.Module,
                Type = TestCaseType.NegativeCase,
                Priority = MapRiskToPriority(item.Risk),
                Description = $"Negative test: {desc} for {item.Description}",
                SuccessCriteria = new SuccessCriteria
                {
                    RequireExactMatch = true,
                    AcceptanceStatement = "Both systems must return identical error/rejection behaviour"
                },
                SqlQuery = sql,
                GeneratedFrom = "Pattern",
                Tags = new() { item.Module, "NegativeCase", tag }
            });
        }

        return cases;
    }

    public List<TestCase> GeneratePerformanceCases(BusinessLogicItem item, TestGenerationConfig config)
    {
        return new()
        {
            new TestCase
            {
                Name = $"[PERF] {item.Module} — {item.Name} — large dataset",
                Module = item.Module,
                Type = TestCaseType.Performance,
                Priority = TestCasePriority.Medium,
                Description = $"Performance: {item.Description} with {config.LargeVolumeRowCount:N0} rows",
                SuccessCriteria = new SuccessCriteria
                {
                    RequireExactMatch = false,
                    MaxPerformanceDegradation = config.MaxPerformanceDegradation,
                    AcceptanceStatement = $"Blazor response time must not exceed legacy by more than {config.MaxPerformanceDegradation * 100:F0}%"
                },
                SqlQuery = $"-- Performance test: {item.Name} with large volume",
                TimeoutSeconds = 120,
                GeneratedFrom = "Pattern",
                Tags = new() { item.Module, "Performance", "LargeVolume" }
            }
        };
    }

    public async Task<List<TestCase>> GenerateWithAiAsync(
        BusinessLogicItem item,
        List<TestCase> existingCases,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var cases = new List<TestCase>();

        try
        {
            var prompt = BuildAiGenerationPrompt(item, existingCases);

            var response = await _anthropic.CreateMessageAsync(
                model: "claude-sonnet-4-6",
                messages: [prompt],
                maxTokens: 2048,
                cancellationToken: cancellationToken);

            var text = response.Content.Value2?
                .Where(b => b.IsText)
                .Select(b => b.Text?.Text)
                .FirstOrDefault(t => t != null) ?? string.Empty;

            cases = ParseAiTestCases(text, item, config);

            _logger.Debug("AI generated {Count} additional test cases for {Item}", cases.Count, item.Name);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AI test generation failed for {Item}", item.Name);
        }

        return cases;
    }

    public SuccessCriteria BuildSuccessCriteria(
        BusinessLogicItem item,
        string dimension,
        TestGenerationConfig config)
    {
        var criteria = new SuccessCriteria
        {
            RequireExactMatch = true,
            IgnoredFields = new(config.GlobalIgnoredFields),
            RequireRowCountMatch = true
        };

        // Financial calculations: zero tolerance
        if (item.Module is "Finance" or "Accounting" or "Payroll" ||
            item.Type == BusinessLogicType.Calculation)
        {
            criteria.NumericTolerance = config.DefaultFinancialTolerance;
            criteria.AcceptanceStatement = "All monetary values must match to 2 decimal places";
            criteria.ExactMatchFields = new() { "Amount", "TotalAmount", "TaxAmount", "NetAmount", "GrossAmount" };
        }
        else
        {
            criteria.NumericTolerance = config.DefaultNumericTolerance;
        }

        // Performance dimension: allow degradation
        if (dimension == "Performance")
        {
            criteria.RequireExactMatch = false;
            criteria.MaxPerformanceDegradation = config.MaxPerformanceDegradation;
            criteria.AcceptanceStatement = $"Response time within {config.MaxPerformanceDegradation * 100:F0}% of legacy baseline";
        }

        return criteria;
    }

    public List<SuccessCriteria> BuildGlobalCriteria(
        List<BusinessLogicItem> items,
        TestGenerationConfig config)
    {
        return new()
        {
            new SuccessCriteria
            {
                RequireExactMatch = true,
                NumericTolerance = config.DefaultFinancialTolerance,
                MaxPerformanceDegradation = config.MaxPerformanceDegradation,
                IgnoredFields = new(config.GlobalIgnoredFields),
                AcceptanceStatement = "All critical financial calculations must match exactly",
                ExactMatchFields = new() { "Amount", "TaxAmount", "NetAmount", "GrossAmount", "TotalAmount" }
            },
            new SuccessCriteria
            {
                RequireRowCountMatch = true,
                AcceptanceStatement = "Row counts must match exactly — no data loss permitted"
            }
        };
    }

    public List<TestCase> DeduplicateCases(List<TestCase> cases)
    {
        return cases
            .GroupBy(c => $"{c.Module}:{c.Type}:{NormalizeName(c.Name)}")
            .Select(g => g.First())
            .ToList();
    }

    public List<TestCase> PrioritizeCases(List<TestCase> cases)
    {
        return cases
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Type)
            .ThenBy(c => c.Module)
            .ToList();
    }

    public void Configure(TestGenerationConfig config)
    {
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TestCasePriority MapRiskToPriority(BusinessLogicRisk risk) => risk switch
    {
        BusinessLogicRisk.Critical => TestCasePriority.Critical,
        BusinessLogicRisk.High => TestCasePriority.High,
        BusinessLogicRisk.Medium => TestCasePriority.Medium,
        _ => TestCasePriority.Low
    };

    private static int TargetCount(BusinessLogicRisk risk, TestGenerationConfig config) => risk switch
    {
        BusinessLogicRisk.Critical => config.TestCasesPerCriticalItem,
        BusinessLogicRisk.High => config.TestCasesPerHighItem,
        BusinessLogicRisk.Medium => config.TestCasesPerMediumItem,
        _ => 1
    };

    private static string BuildHappyPathQuery(BusinessLogicItem item, int index)
    {
        return $"""
            -- Happy path test #{index + 1}: {item.Name}
            -- Module: {item.Module} | Type: {item.Type}
            -- Source: {item.SourceFile ?? "N/A"}
            -- TODO: Replace with actual query based on {item.Description}
            SELECT 1 AS TestPlaceholder;
            """;
    }

    private static List<(string name, string desc, string sql)> GetEdgeScenariosFor(BusinessLogicItem item)
    {
        var scenarios = new List<(string, string, string)>();

        if (item.Type == BusinessLogicType.Calculation)
        {
            scenarios.Add(("ZeroValue", "Calculation with zero input", "-- Zero value test"));
            scenarios.Add(("MaxValue", "Calculation at maximum allowed value", "-- Max value test"));
            scenarios.Add(("NegativeInput", "Calculation with negative input", "-- Negative input test"));
            scenarios.Add(("DecimalPrecision", "Calculation with many decimal places", "-- Decimal precision test"));
        }

        if (item.Type == BusinessLogicType.Workflow)
        {
            scenarios.Add(("AllApprovalLevels", "Full approval chain traversal", "-- Full workflow test"));
            scenarios.Add(("RejectionAtFirstLevel", "Rejection at first approval level", "-- Early rejection test"));
            scenarios.Add(("StatusRollback", "Status rollback on rejection", "-- Rollback test"));
        }

        if (item.Type == BusinessLogicType.Validation)
        {
            scenarios.Add(("BoundaryMin", "Value at minimum boundary", "-- Min boundary test"));
            scenarios.Add(("BoundaryMax", "Value at maximum boundary", "-- Max boundary test"));
            scenarios.Add(("ExactlyAtLimit", "Value exactly at rule limit", "-- At-limit test"));
        }

        if (item.Type == BusinessLogicType.BusinessRule)
        {
            scenarios.Add(("ThresholdExceeded", "Value exceeds business rule threshold", "-- Threshold test"));
            scenarios.Add(("ThresholdExact", "Value exactly at threshold", "-- Exact threshold test"));
        }

        // Universal edge cases for tribal knowledge items
        if (item.IsTribalKnowledge)
        {
            scenarios.Add(("TribalKnowledgeScenario", $"Expert-identified edge case: {item.Description}", "-- Tribal knowledge test"));
        }

        // Ensure at least one edge case
        if (!scenarios.Any())
            scenarios.Add(("GenericEdge", $"Edge case for {item.Type}", "-- Generic edge case"));

        return scenarios;
    }

    private List<TestCase> GenerateFromMatrixEntry(ValidationMatrixEntry entry, TestGenerationConfig config)
    {
        var placeholder = new BusinessLogicItem
        {
            Module = entry.Module,
            Name = $"{entry.Dimension} baseline",
            Description = $"Validate {entry.Dimension} for {entry.Module}",
            Type = BusinessLogicType.BusinessRule,
            Risk = entry.Priority == MatrixPriority.Critical ? BusinessLogicRisk.Critical
                : entry.Priority == MatrixPriority.High ? BusinessLogicRisk.High
                : BusinessLogicRisk.Medium
        };

        return new List<TestCase>
        {
            new()
            {
                Name = $"[MATRIX] {entry.Module} × {entry.Dimension} — baseline",
                Module = entry.Module,
                Dimension = entry.Dimension,
                Type = TestCaseType.HappyPath,
                Priority = (TestCasePriority)(int)entry.Priority,
                Description = $"Baseline validation: {entry.Module} module against {entry.Dimension} dimension",
                SuccessCriteria = BuildSuccessCriteria(placeholder, entry.Dimension, config),
                MatrixEntryId = entry.Id,
                GeneratedFrom = "Matrix",
                Tags = new() { entry.Module, entry.Dimension }
            }
        };
    }

    private static string BuildAiGenerationPrompt(BusinessLogicItem item, List<TestCase> existingCases)
    {
        var existingNames = string.Join(", ", existingCases.Select(c => c.Name).Take(5));

        return $$"""
            You are a test engineer for an ERP migration validation system (WinForms → Blazor).
            Generate additional SQL-based test cases for the following business logic item that complement the existing ones.

            Business Logic Item:
            - Module: {{item.Module}}
            - Name: {{item.Name}}
            - Type: {{item.Type}}
            - Risk: {{item.Risk}}
            - Description: {{item.Description}}
            - Is Tribal Knowledge: {{item.IsTribalKnowledge}}
            - Pseudo Code: {{item.PseudoCode ?? "N/A"}}

            Already generated test cases (avoid duplicating these):
            {{existingNames}}

            Generate 3-5 additional test cases focusing on:
            1. Domain-specific edge cases for {{item.Module}} ERP module
            2. Scenarios where legacy WinForms and Blazor might differ subtly
            3. Fiscal year / period boundary cases if financial module
            4. Multi-record / batch scenarios

            Respond with a JSON array. Each element:
            {
              "name": "short test name",
              "type": "HappyPath|EdgeCase|NegativeCase|Performance|DataIntegrity",
              "description": "what this tests in business terms",
              "sqlHint": "brief description of what the SQL should do",
              "priority": "Critical|High|Medium|Low",
              "tags": ["tag1", "tag2"]
            }

            Respond ONLY with valid JSON. No markdown.
            """;
    }

    private List<TestCase> ParseAiTestCases(string json, BusinessLogicItem item, TestGenerationConfig config)
    {
        var cases = new List<TestCase>();

        try
        {
            json = json.Trim();
            if (json.StartsWith("```"))
                json = System.Text.RegularExpressions.Regex.Replace(json, @"```\w*\n?", "").Trim();

            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            if (parsed == null) return cases;

            foreach (var entry in parsed)
            {
                var typeStr = entry.GetValueOrDefault("type")?.ToString() ?? "HappyPath";
                var priorityStr = entry.GetValueOrDefault("priority")?.ToString() ?? "Medium";

                Enum.TryParse<TestCaseType>(typeStr, out var type);
                Enum.TryParse<TestCasePriority>(priorityStr, out var priority);

                var tagsEl = entry.GetValueOrDefault("tags");
                var tags = new List<string> { item.Module, "AI-Generated" };

                cases.Add(new TestCase
                {
                    Name = $"[AI] {item.Module} — {entry.GetValueOrDefault("name")?.ToString() ?? "AI test"}",
                    Module = item.Module,
                    Type = type,
                    Priority = priority,
                    Description = entry.GetValueOrDefault("description")?.ToString() ?? string.Empty,
                    SqlQuery = $"-- AI hint: {entry.GetValueOrDefault("sqlHint")?.ToString()}",
                    SuccessCriteria = BuildSuccessCriteria(item, item.Type.ToString(), config),
                    GeneratedFrom = "AI",
                    Tags = tags
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse AI test case response");
        }

        return cases;
    }

    private static string NormalizeName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"\d+|[\[\]\-\s]+", "_");
    }
}
