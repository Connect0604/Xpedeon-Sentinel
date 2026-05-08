namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Orchestrates Phase 1 — Discovery &amp; Planning.
/// Coordinates code analysis, schema discovery, and expert interview recording,
/// then produces the DiscoveryReport phase deliverable.
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly IBusinessLogicExtractor _extractor;
    private readonly ISchemaAnalyzer _schemaAnalyzer;
    private readonly IValidationMemoryService _memory;
    private readonly ILogger _logger;
    private DiscoveryServiceConfig _config;

    // In-memory store keyed by "sessionId:clientId"
    private readonly Dictionary<string, List<BusinessLogicItem>> _findingsStore = new();
    private readonly Dictionary<string, List<ExpertInterviewRecord>> _interviewStore = new();
    private readonly Dictionary<string, double> _progressStore = new();

    public DiscoveryService(
        IBusinessLogicExtractor extractor,
        ISchemaAnalyzer schemaAnalyzer,
        IValidationMemoryService memory,
        ILogger? logger = null)
    {
        _extractor = extractor;
        _schemaAnalyzer = schemaAnalyzer;
        _memory = memory;
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new DiscoveryServiceConfig();
        _logger.Information("DiscoveryService initialized");
    }

    public async Task<DiscoveryReport> RunDiscoveryAsync(
        string sessionId,
        string clientId,
        DiscoveryConfig config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.Information("Phase 1 Discovery starting for session {Session}", sessionId);

        var report = new DiscoveryReport
        {
            SessionId = sessionId,
            ClientId = clientId
        };

        try
        {
            SetProgress(sessionId, clientId, 0);

            // Step 1: Load source code files
            _logger.Information("Loading source files from {Path}", config.SourceCodePath);
            var extractorConfig = BuildExtractorConfig(config);
            var files = await LoadCodeFilesAsync(config.SourceCodePath, config);
            report.AnalyzedFiles = files;

            SetProgress(sessionId, clientId, 10);

            // Step 2: Extract business logic
            _logger.Information("Extracting business logic from {Count} files", files.Count);
            var progress = new Progress<int>(p => SetProgress(sessionId, clientId, 10 + p * 0.6));
            var items = await AnalyzeSourceCodeAsync(sessionId, clientId, files, progress, cancellationToken);

            await StoreDiscoveryFindingsAsync(sessionId, clientId, items);
            report.AllLogicItems = items;

            SetProgress(sessionId, clientId, 70);

            // Step 3: Update DiscoveryFindings in ValidationMemoryService
            var findings = BuildDiscoveryFindings(items, files);
            await _memory.UpdateSessionPhaseStatusAsync(sessionId, new SessionPhaseStatus
            {
                Phase = ValidationPhase.Discovery,
                State = PhaseState.InProgress,
                ProgressPercentage = 70,
                ItemsProcessed = items.Count
            });

            SetProgress(sessionId, clientId, 80);

            // Step 4: Compute module risks
            report.ModuleRisks = ComputeModuleRisks(items);
            report.ModulesDiscovered = report.ModuleRisks.Count;
            report.PatternsIdentified = items.Select(i => i.Type).Distinct().Count();
            report.EstimatedTestCasesRequired = EstimateTestCases(items);

            SetProgress(sessionId, clientId, 90);

            // Step 5: Build discovery summary
            report.CriticalFindings = items
                .Where(i => i.Risk == BusinessLogicRisk.Critical)
                .Select(i => $"[{i.Module}] {i.Name}: {i.Description}")
                .Take(20)
                .ToList();

            report.KeyRisks = report.ModuleRisks
                .Where(m => m.RiskScore >= 70)
                .Select(m => $"{m.Module}: {m.RiskLevel} risk ({m.CriticalItems} critical items)")
                .ToList();

            report.Recommendations = BuildRecommendations(report.ModuleRisks, items);
            report.ExecutiveSummary = BuildExecutiveSummary(report);
            report.Success = true;

            // Mark phase complete in memory
            stopwatch.Stop();
            report.DiscoveryDurationMs = stopwatch.ElapsedMilliseconds;

            await _memory.UpdateSessionPhaseStatusAsync(sessionId, new SessionPhaseStatus
            {
                Phase = ValidationPhase.Discovery,
                State = PhaseState.Completed,
                ProgressPercentage = 100,
                ItemsProcessed = items.Count,
                DurationMs = stopwatch.ElapsedMilliseconds
            });

            SetProgress(sessionId, clientId, 100);
            _logger.Information(
                "Phase 1 Discovery completed: {Items} items, {Modules} modules, {Duration}ms",
                items.Count, report.ModulesDiscovered, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            report.Success = false;
            report.Error = ex.Message;
            _logger.Error(ex, "Phase 1 Discovery failed for session {Session}", sessionId);
        }
        finally
        {
            stopwatch.Stop();
        }

        return report;
    }

    public async Task<DiscoveryReport> ResumeDiscoveryAsync(
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Resuming discovery for session {Session}", sessionId);

        var existing = await GetDiscoveryFindingsAsync(sessionId, clientId);
        var interviews = await GetExpertInterviewsAsync(sessionId, clientId);

        var report = await GenerateDiscoveryReportAsync(sessionId, clientId);
        report.ExpertInterviews = interviews;

        return report;
    }

    public async Task<List<BusinessLogicItem>> AnalyzeSourceCodeAsync(
        string sessionId,
        string clientId,
        List<CodeFile> files,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var extractorConfig = new BusinessLogicExtractionConfig
        {
            UseAiExtraction = _config.TargetModules.Count == 0 || true,
            IncludeCodeSnippets = true,
            GeneratePseudoCode = true
        };
        _extractor.Configure(extractorConfig);

        return await _extractor.ExtractFromCodebaseAsync(
            files, sessionId, clientId, progress, cancellationToken);
    }

    public async Task<List<CodeFile>> LoadCodeFilesAsync(
        string sourcePath,
        DiscoveryConfig config)
    {
        var extractorConfig = BuildExtractorConfig(config);
        return await _extractor.DiscoverCodeFilesAsync(sourcePath, extractorConfig);
    }

    public async Task<ExpertInterviewRecord> RecordExpertInterviewAsync(
        string sessionId,
        string clientId,
        string expertName,
        string expertRole,
        List<ExpertFinding> findings,
        string? rawNotes = null)
    {
        var record = new ExpertInterviewRecord
        {
            SessionId = sessionId,
            ExpertName = expertName,
            ExpertRole = expertRole,
            Findings = findings,
            Modules = findings.Select(f => f.Module).Distinct().ToList(),
            RawNotes = rawNotes
        };

        var key = StoreKey(sessionId, clientId);
        if (!_interviewStore.ContainsKey(key))
            _interviewStore[key] = new();

        _interviewStore[key].Add(record);

        _logger.Information(
            "Recorded expert interview with {Expert} ({Role}): {Count} findings",
            expertName, expertRole, findings.Count);

        await ApplyExpertFeedbackAsync(sessionId, clientId, record);
        return record;
    }

    public Task<List<ExpertInterviewRecord>> GetExpertInterviewsAsync(
        string sessionId,
        string clientId)
    {
        var key = StoreKey(sessionId, clientId);
        return Task.FromResult(_interviewStore.GetValueOrDefault(key) ?? new());
    }

    public async Task ApplyExpertFeedbackAsync(
        string sessionId,
        string clientId,
        ExpertInterviewRecord interview)
    {
        var items = await GetDiscoveryFindingsAsync(sessionId, clientId);

        // Mark confirmed items
        foreach (var id in interview.ConfirmedLogicIds)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null)
                item.ExpertConfirmation = $"Confirmed by {interview.ExpertName}";
        }

        // Upgrade disputed items to tribal knowledge
        foreach (var id in interview.DisputedLogicIds)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item != null)
                item.IsTribalKnowledge = true;
        }

        // Add newly discovered items from expert findings
        var newItems = interview.Findings
            .Where(f => f.FindingType == "TribalKnowledge" || f.FindingType == "EdgeCase")
            .Select(f => new BusinessLogicItem
            {
                Module = f.Module,
                Name = $"Expert finding: {f.FindingType}",
                Description = f.Description,
                Type = f.FindingType == "EdgeCase" ? BusinessLogicType.EdgeCase : BusinessLogicType.BusinessRule,
                Risk = f.Risk,
                IsTribalKnowledge = f.FindingType == "TribalKnowledge",
                ExpertConfirmation = $"Reported by {interview.ExpertName}",
                IsCritical = f.Risk == BusinessLogicRisk.Critical
            })
            .ToList();

        if (newItems.Count > 0)
        {
            items.AddRange(newItems);
            await StoreDiscoveryFindingsAsync(sessionId, clientId, items);
        }
    }

    public Task StoreDiscoveryFindingsAsync(
        string sessionId,
        string clientId,
        List<BusinessLogicItem> items)
    {
        _findingsStore[StoreKey(sessionId, clientId)] = items;
        return Task.CompletedTask;
    }

    public Task<List<BusinessLogicItem>> GetDiscoveryFindingsAsync(
        string sessionId,
        string clientId)
    {
        return Task.FromResult(
            _findingsStore.GetValueOrDefault(StoreKey(sessionId, clientId)) ?? new());
    }

    public async Task AddFindingAsync(
        string sessionId,
        string clientId,
        BusinessLogicItem item)
    {
        var items = await GetDiscoveryFindingsAsync(sessionId, clientId);
        items.Add(item);
        await StoreDiscoveryFindingsAsync(sessionId, clientId, items);
    }

    public async Task<DiscoveryReport> GenerateDiscoveryReportAsync(
        string sessionId,
        string clientId)
    {
        var items = await GetDiscoveryFindingsAsync(sessionId, clientId);
        var interviews = await GetExpertInterviewsAsync(sessionId, clientId);

        var report = new DiscoveryReport
        {
            SessionId = sessionId,
            ClientId = clientId,
            AllLogicItems = items,
            ModuleRisks = ComputeModuleRisks(items),
            ExpertInterviews = interviews,
            ModulesDiscovered = items.Select(i => i.Module).Distinct().Count(),
            PatternsIdentified = items.Select(i => i.Type).Distinct().Count(),
            EstimatedTestCasesRequired = EstimateTestCases(items),
            CriticalFindings = items
                .Where(i => i.Risk == BusinessLogicRisk.Critical)
                .Select(i => $"[{i.Module}] {i.Name}")
                .Take(20)
                .ToList(),
            Success = true,
            GeneratedAt = DateTime.UtcNow
        };

        report.Recommendations = BuildRecommendations(report.ModuleRisks, items);
        report.ExecutiveSummary = BuildExecutiveSummary(report);

        return report;
    }

    public List<ModuleRiskSummary> ComputeModuleRisks(List<BusinessLogicItem> items)
    {
        return items
            .GroupBy(i => i.Module)
            .Select(g =>
            {
                var total = g.Count();
                var critical = g.Count(i => i.Risk == BusinessLogicRisk.Critical);
                var high = g.Count(i => i.Risk == BusinessLogicRisk.High);
                var undoc = g.Count(i => i.IsTribalKnowledge);

                var score = Math.Min(100,
                    critical * 20.0 + high * 8.0 + undoc * 5.0);
                var level = score >= 80 ? "Critical"
                    : score >= 60 ? "High"
                    : score >= 30 ? "Medium"
                    : "Low";

                return new ModuleRiskSummary
                {
                    Module = g.Key,
                    TotalLogicItems = total,
                    CriticalItems = critical,
                    HighRiskItems = high,
                    UndocumentedItems = undoc,
                    ExpertConfirmedItems = g.Count(i => i.ExpertConfirmation != null),
                    RiskScore = score,
                    RiskLevel = level,
                    TopRisks = g
                        .Where(i => i.Risk <= BusinessLogicRisk.High)
                        .OrderBy(i => i.Risk)
                        .Take(3)
                        .Select(i => i.Name)
                        .ToList(),
                    RecommendedActions = BuildModuleActions(g.Key, score, critical)
                };
            })
            .OrderByDescending(m => m.RiskScore)
            .ToList();
    }

    public Task<double> GetDiscoveryProgressAsync(string sessionId, string clientId)
    {
        return Task.FromResult(_progressStore.GetValueOrDefault(StoreKey(sessionId, clientId), 0.0));
    }

    public void Configure(DiscoveryServiceConfig config)
    {
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string StoreKey(string sessionId, string clientId) => $"{sessionId}:{clientId}";

    private void SetProgress(string sessionId, string clientId, double percent)
    {
        _progressStore[StoreKey(sessionId, clientId)] = percent;
    }

    private static DiscoveryFindings BuildDiscoveryFindings(
        List<BusinessLogicItem> items, List<CodeFile> files)
    {
        return new DiscoveryFindings
        {
            TotalBusinessLogicItems = items.Count,
            UndocumentedLogicItems = items.Count(i => i.IsTribalKnowledge),
            CriticalModules = items
                .Where(i => i.IsCritical)
                .Select(i => i.Module)
                .Distinct()
                .ToList(),
            IdentifiedPatterns = items
                .Select(i => i.Type.ToString())
                .Distinct()
                .ToList(),
            LogicByModule = items
                .GroupBy(i => i.Module)
                .ToDictionary(g => g.Key, g => g.Count()),
            TablesAnalyzed = files.Count,
            RulesIdentified = items.Count(i => i.Type == BusinessLogicType.BusinessRule),
            KeyFindings = items
                .Where(i => i.Risk == BusinessLogicRisk.Critical)
                .Take(5)
                .Select(i => i.Description)
                .ToList()
        };
    }

    private static int EstimateTestCases(List<BusinessLogicItem> items)
    {
        return items.Sum(i => i.Risk switch
        {
            BusinessLogicRisk.Critical => 10,
            BusinessLogicRisk.High => 5,
            BusinessLogicRisk.Medium => 3,
            _ => 1
        });
    }

    private static List<string> BuildRecommendations(
        List<ModuleRiskSummary> moduleRisks,
        List<BusinessLogicItem> items)
    {
        var recs = new List<string>();

        var critical = moduleRisks.Where(m => m.RiskLevel == "Critical").ToList();
        if (critical.Count > 0)
            recs.Add($"CRITICAL: Prioritize expert review for {string.Join(", ", critical.Select(m => m.Module))} — migration should not proceed without sign-off.");

        var undocTotal = items.Count(i => i.IsTribalKnowledge);
        if (undocTotal > 10)
            recs.Add($"Schedule expert interviews to document {undocTotal} undocumented tribal knowledge items before test generation.");

        if (items.Count(i => i.Type == BusinessLogicType.Calculation && i.Module is "Finance" or "Accounting") > 0)
            recs.Add("Financial calculations must be validated to exact decimal precision — use tolerance of 0.00 for money fields.");

        recs.Add("Generate test cases for all Critical and High risk items before proceeding to Phase 2.");

        return recs;
    }

    private static string BuildExecutiveSummary(DiscoveryReport report)
    {
        var criticalModules = report.ModuleRisks.Where(m => m.RiskLevel == "Critical").ToList();
        var goNogo = criticalModules.Count == 0 ? "PROCEED" : "HOLD";

        return $"""
            Phase 1 Discovery Complete. Analyzed {report.FilesAnalyzed} source files across {report.ModulesDiscovered} modules.
            Found {report.TotalLogicItems} business logic items ({report.CriticalItems} Critical, {report.UndocumentedItems} undocumented).
            Estimated {report.EstimatedTestCasesRequired} test cases required for Phase 2.
            Highest-risk modules: {string.Join(", ", report.ModuleRisks.Take(3).Select(m => $"{m.Module} ({m.RiskLevel})"))}.
            Recommendation: {goNogo} to Phase 2{(criticalModules.Count > 0 ? $" after resolving {criticalModules.Count} critical module(s)" : "")}.
            """;
    }

    private static BusinessLogicExtractionConfig BuildExtractorConfig(DiscoveryConfig config)
    {
        return new BusinessLogicExtractionConfig
        {
            FileExtensions = config.FileExtensions,
            ExcludePaths = config.ExcludePaths,
            PriorityModules = config.PriorityModules,
            UseAiExtraction = config.UseAiExtraction,
            IncludeCodeSnippets = config.IncludeCodeSnippets,
            GeneratePseudoCode = config.GeneratePseudoCode,
            MaxFiles = config.MaxFilesToAnalyze
        };
    }

    private static List<string> BuildModuleActions(string module, double riskScore, int criticalCount)
    {
        var actions = new List<string>();

        if (criticalCount > 0)
            actions.Add($"Conduct expert review for {criticalCount} critical logic item(s) in {module}");

        if (riskScore >= 60)
            actions.Add($"Increase test coverage for {module} — target 100% of Critical and High items");

        actions.Add($"Document all tribal knowledge items in {module} before Phase 2");
        return actions;
    }
}
