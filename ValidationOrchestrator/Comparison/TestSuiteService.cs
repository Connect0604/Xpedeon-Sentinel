namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using System.Text;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Orchestrates Phase 2 — Test Case Generation.
/// Coordinates the test case generator, data service, and memory service
/// to produce and persist the complete test suite deliverable.
/// </summary>
public class TestSuiteService : ITestSuiteService
{
    private readonly ITestCaseGeneratorService _generator;
    private readonly ITestDataService _dataService;
    private readonly IValidationMemoryService _memory;
    private readonly ILogger _logger;
    private TestSuiteServiceConfig _config;

    // In-memory store keyed by "sessionId:clientId"
    private readonly Dictionary<string, TestSuite> _suiteStore = new();
    private readonly Dictionary<string, double> _progressStore = new();

    public TestSuiteService(
        ITestCaseGeneratorService generator,
        ITestDataService dataService,
        IValidationMemoryService memory,
        ILogger? logger = null)
    {
        _generator = generator;
        _dataService = dataService;
        _memory = memory;
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new TestSuiteServiceConfig();
        _logger.Information("TestSuiteService initialized");
    }

    public async Task<TestSuite> RunTestGenerationAsync(
        string sessionId,
        string clientId,
        ValidationMatrix matrix,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.Information("Phase 2 Test Generation starting for session {Session}", sessionId);

        var suite = new TestSuite
        {
            SessionId = sessionId,
            ClientId = clientId,
            Name = $"Xpedeon Validation Test Suite — {DateTime.UtcNow:yyyy-MM-dd}",
            Status = TestSuiteStatus.Building
        };

        try
        {
            SetProgress(sessionId, clientId, 0);

            // Step 1: Generate test cases from validation matrix
            _logger.Information("Generating test cases from {Entries} matrix entries", matrix.TotalEntries);
            var caseProgress = new Progress<int>(p => SetProgress(sessionId, clientId, p * 0.5));

            _generator.Configure(config);
            var testCases = await _generator.GenerateForMatrixAsync(
                matrix, logicItems, config, caseProgress, cancellationToken);

            suite.TestCases = testCases;
            SetProgress(sessionId, clientId, 50);

            // Step 2: Assemble scenarios
            suite.Scenarios = _generator.AssembleScenarios(testCases, logicItems);

            // Step 3: Build success criteria
            suite.GlobalCriteria = _generator.BuildGlobalCriteria(logicItems, config);

            SetProgress(sessionId, clientId, 60);

            // Step 4: Generate datasets
            _logger.Information("Generating test datasets");
            suite.CoveredModules = testCases.Select(c => c.Module).Distinct().ToList();
            suite.CoveredDimensions = testCases.Select(c => c.Dimension).Where(d => !string.IsNullOrEmpty(d)).Distinct().ToList();

            var dataProgress = new Progress<int>(p => SetProgress(sessionId, clientId, 60 + p * 0.3));
            _dataService.Configure(config);
            suite.DataSets = await _dataService.GenerateAllDatasetsAsync(
                suite, logicItems, config, dataProgress, cancellationToken);

            SetProgress(sessionId, clientId, 90);

            // Step 5: Compute coverage metrics
            suite.MatrixCoverage = CalculateMatrixCoverage(suite, matrix);
            suite.UncoveredAreas = FindUncoveredEntries(suite, matrix)
                .Select(e => $"{e.Module} × {e.Dimension}")
                .Take(20)
                .ToList();
            suite.EstimatedExecutionMinutes = EstimateExecutionTime(suite);

            // Step 6: Finalize or mark as ready
            suite.Status = suite.MatrixCoverage >= _config.MinimumCoverageThreshold
                ? TestSuiteStatus.Ready
                : TestSuiteStatus.Building;

            suite.Success = true;
            stopwatch.Stop();
            suite.GenerationDurationMs = stopwatch.ElapsedMilliseconds;

            // Persist to session memory
            await StoreSuiteAsync(sessionId, clientId, suite);

            await _memory.UpdateSessionPhaseStatusAsync(sessionId, new SessionPhaseStatus
            {
                Phase = ValidationPhase.TestGeneration,
                State = PhaseState.Completed,
                ProgressPercentage = 100,
                ItemsProcessed = suite.TotalTestCases,
                DurationMs = stopwatch.ElapsedMilliseconds
            });

            SetProgress(sessionId, clientId, 100);

            _logger.Information(
                "Phase 2 complete: {Cases} test cases, {Scenarios} scenarios, {DataSets} datasets, {Coverage:F1}% coverage in {Duration}ms",
                suite.TotalTestCases, suite.TotalScenarios, suite.DataSets.Count,
                suite.MatrixCoverage, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            suite.Success = false;
            suite.Error = ex.Message;
            suite.Status = TestSuiteStatus.Building;
            _logger.Error(ex, "Phase 2 Test Generation failed for session {Session}", sessionId);
        }
        finally
        {
            stopwatch.Stop();
        }

        return suite;
    }

    public Task StoreSuiteAsync(string sessionId, string clientId, TestSuite suite)
    {
        _suiteStore[StoreKey(sessionId, clientId)] = suite;
        return Task.CompletedTask;
    }

    public Task<TestSuite?> GetSuiteAsync(string sessionId, string clientId)
    {
        return Task.FromResult(_suiteStore.GetValueOrDefault(StoreKey(sessionId, clientId)));
    }

    public async Task AddTestCasesAsync(string sessionId, string clientId, List<TestCase> cases)
    {
        var suite = await GetSuiteAsync(sessionId, clientId)
                    ?? new TestSuite { SessionId = sessionId, ClientId = clientId };
        suite.TestCases.AddRange(cases);
        await StoreSuiteAsync(sessionId, clientId, suite);
    }

    public async Task AddDataSetsAsync(string sessionId, string clientId, List<TestDataSet> dataSets)
    {
        var suite = await GetSuiteAsync(sessionId, clientId)
                    ?? new TestSuite { SessionId = sessionId, ClientId = clientId };
        suite.DataSets.AddRange(dataSets);
        await StoreSuiteAsync(sessionId, clientId, suite);
    }

    public async Task<TestSuite> FinalizeSuiteAsync(string sessionId, string clientId)
    {
        var suite = await GetSuiteAsync(sessionId, clientId)
                    ?? throw new InvalidOperationException($"No suite found for session {sessionId}");

        suite.Status = TestSuiteStatus.Ready;
        suite.FinalizedAt = DateTime.UtcNow;
        suite.ValidationNotes = $"Finalized with {suite.TotalTestCases} test cases and {suite.MatrixCoverage:F1}% matrix coverage";

        await StoreSuiteAsync(sessionId, clientId, suite);
        _logger.Information("Test suite finalized for session {Session}", sessionId);
        return suite;
    }

    public double CalculateMatrixCoverage(TestSuite suite, ValidationMatrix matrix)
    {
        if (matrix.TotalEntries == 0) return 0;

        var coveredEntryIds = suite.TestCases
            .Where(c => c.MatrixEntryId != null)
            .Select(c => c.MatrixEntryId!)
            .Distinct()
            .ToHashSet();

        var covered = matrix.Entries.Count(e => coveredEntryIds.Contains(e.Id)
            || (suite.CoveredModules.Contains(e.Module) && suite.CoveredDimensions.Contains(e.Dimension)));

        return Math.Round((double)covered / matrix.TotalEntries * 100, 1);
    }

    public List<ValidationMatrixEntry> FindUncoveredEntries(TestSuite suite, ValidationMatrix matrix)
    {
        var coveredEntryIds = suite.TestCases
            .Where(c => c.MatrixEntryId != null)
            .Select(c => c.MatrixEntryId!)
            .Distinct()
            .ToHashSet();

        return matrix.Entries
            .Where(e => !coveredEntryIds.Contains(e.Id)
                && !(suite.CoveredModules.Contains(e.Module) && suite.CoveredDimensions.Contains(e.Dimension)))
            .OrderBy(e => e.Priority)
            .ToList();
    }

    public int EstimateExecutionTime(TestSuite suite)
    {
        // Rough estimate: critical=2min, high=1min, medium=30s, low=10s per test case
        var totalSeconds = suite.TestCases.Sum(t => t.Priority switch
        {
            TestCasePriority.Critical => 120,
            TestCasePriority.High => 60,
            TestCasePriority.Medium => 30,
            _ => 10
        });

        return (int)Math.Ceiling(totalSeconds / 60.0);
    }

    public string GenerateSuiteReport(TestSuite suite)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Test Suite Report — Phase 2");
        sb.AppendLine($"Session: {suite.SessionId} | Generated: {suite.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Status: {suite.Status} | Coverage: {suite.MatrixCoverage:F1}%");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Total Test Cases | {suite.TotalTestCases:N0} |");
        sb.AppendLine($"| Critical Test Cases | {suite.CriticalTestCases:N0} |");
        sb.AppendLine($"| Test Scenarios | {suite.TotalScenarios:N0} |");
        sb.AppendLine($"| Datasets | {suite.DataSets.Count:N0} |");
        sb.AppendLine($"| Matrix Coverage | {suite.MatrixCoverage:F1}% |");
        sb.AppendLine($"| Estimated Execution | {suite.EstimatedExecutionMinutes} minutes |");
        sb.AppendLine();

        sb.AppendLine("## Module Breakdown");
        var byModule = suite.TestCases.GroupBy(c => c.Module);
        sb.AppendLine("| Module | Total | Critical | High | Edge Cases |");
        sb.AppendLine("|--------|-------|----------|------|-----------|");
        foreach (var g in byModule.OrderByDescending(g => g.Count(c => c.Priority == TestCasePriority.Critical)))
        {
            sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Count(c => c.Priority == TestCasePriority.Critical)} | {g.Count(c => c.Priority == TestCasePriority.High)} | {g.Count(c => c.Type == TestCaseType.EdgeCase)} |");
        }

        if (suite.UncoveredAreas.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Uncovered Areas");
            foreach (var area in suite.UncoveredAreas.Take(10))
                sb.AppendLine($"- {area}");
        }

        return sb.ToString();
    }

    public Task<double> GetProgressAsync(string sessionId, string clientId)
    {
        return Task.FromResult(_progressStore.GetValueOrDefault(StoreKey(sessionId, clientId), 0.0));
    }

    public void Configure(TestSuiteServiceConfig config) => _config = config;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string StoreKey(string sessionId, string clientId) => $"{sessionId}:{clientId}";

    private void SetProgress(string sessionId, string clientId, double percent)
    {
        _progressStore[StoreKey(sessionId, clientId)] = percent;
    }
}
