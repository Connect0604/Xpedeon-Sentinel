namespace ValidationOrchestrator.Comparison;

using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Builds and manages the validation matrix mapping ERP modules to validation dimensions.
/// Drives test planning for Phase 2 by providing prioritized, coverage-tracked entries.
/// </summary>
public class ValidationMatrixService : IValidationMatrixService
{
    private readonly ILogger _logger;
    private ValidationMatrixConfig _config;

    // In-memory store keyed by "sessionId:clientId"
    private readonly Dictionary<string, ValidationMatrix> _matrixStore = new();
    private readonly Dictionary<string, List<BusinessLogicItem>> _itemStore = new();

    public ValidationMatrixService(ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new ValidationMatrixConfig();
        _logger.Information("ValidationMatrixService initialized");
    }

    public async Task<ValidationMatrix> BuildMatrixAsync(
        string sessionId,
        string clientId,
        List<BusinessLogicItem> items,
        DiscoveryConfig config)
    {
        _logger.Information(
            "Building validation matrix for {Count} logic items across {Modules} modules",
            items.Count,
            items.Select(i => i.Module).Distinct().Count());

        var key = StoreKey(sessionId, clientId);
        _itemStore[key] = items;

        var dimensions = config.ValidationDimensions.Count > 0
            ? config.ValidationDimensions
            : _config.Dimensions;

        var modules = items.Select(i => i.Module).Distinct().OrderBy(m => m).ToList();

        var matrix = new ValidationMatrix
        {
            SessionId = sessionId,
            ClientId = clientId,
            Modules = modules,
            Dimensions = dimensions,
            CreatedAt = DateTime.UtcNow
        };

        // Build one entry per module × dimension pair
        foreach (var module in modules)
        {
            var moduleItems = items.Where(i => i.Module == module).ToList();
            var priorityDimensions = _config.ModulePriorityDimensions
                .GetValueOrDefault(module, new());

            foreach (var dimension in dimensions)
            {
                var relatedItems = FilterItemsForDimension(moduleItems, dimension);
                var priority = DeterminePriority(module, dimension, relatedItems, priorityDimensions);
                var estimatedCases = EstimateTestCasesForEntry(priority, relatedItems.Count);

                matrix.Entries.Add(new ValidationMatrixEntry
                {
                    Module = module,
                    Dimension = dimension,
                    Priority = priority,
                    EstimatedTestCases = estimatedCases,
                    BusinessLogicItemIds = relatedItems.Select(i => i.Id).ToList(),
                    HasDependencies = HasCrossDimensionDependency(module, dimension),
                    AcceptanceCriteria = BuildAcceptanceCriteria(module, dimension, priority)
                });
            }
        }

        matrix.OverallCoverage = 0; // No tests run yet
        _matrixStore[key] = matrix;

        _logger.Information(
            "Validation matrix built: {Entries} entries ({Critical} Critical, {High} High)",
            matrix.TotalEntries, matrix.CriticalEntries, matrix.HighEntries);

        return await Task.FromResult(matrix);
    }

    public async Task<ValidationMatrix> RefreshMatrixAsync(
        string sessionId,
        string clientId)
    {
        var key = StoreKey(sessionId, clientId);
        var items = _itemStore.GetValueOrDefault(key) ?? new();
        var existing = _matrixStore.GetValueOrDefault(key);

        if (existing == null)
        {
            _logger.Warning("No existing matrix found for session {Session}, building new", sessionId);
            return await BuildMatrixAsync(sessionId, clientId, items, new DiscoveryConfig());
        }

        // Preserve coverage progress; rebuild entries
        var coverageMap = existing.Entries
            .ToDictionary(e => $"{e.Module}:{e.Dimension}", e => e.ActualTestCases);

        var refreshed = await BuildMatrixAsync(sessionId, clientId, items, new DiscoveryConfig
        {
            ValidationDimensions = existing.Dimensions
        });

        // Re-apply coverage
        foreach (var entry in refreshed.Entries)
        {
            var mapKey = $"{entry.Module}:{entry.Dimension}";
            if (coverageMap.TryGetValue(mapKey, out var actual))
            {
                entry.ActualTestCases = actual;
                entry.CoveragePercentage = entry.EstimatedTestCases > 0
                    ? (double)actual / entry.EstimatedTestCases * 100
                    : 0;
            }
        }

        refreshed.OverallCoverage = CalculateCoverage(refreshed);
        refreshed.LastUpdatedAt = DateTime.UtcNow;
        _matrixStore[key] = refreshed;

        return refreshed;
    }

    public Task<ValidationMatrix?> GetMatrixAsync(string sessionId, string clientId)
    {
        return Task.FromResult(_matrixStore.GetValueOrDefault(StoreKey(sessionId, clientId)));
    }

    public Task<ValidationMatrixEntry> UpsertEntryAsync(
        string sessionId,
        string clientId,
        ValidationMatrixEntry entry)
    {
        var key = StoreKey(sessionId, clientId);
        if (!_matrixStore.TryGetValue(key, out var matrix))
        {
            matrix = new ValidationMatrix { SessionId = sessionId, ClientId = clientId };
            _matrixStore[key] = matrix;
        }

        var existing = matrix.Entries.FirstOrDefault(e => e.Id == entry.Id
            || (e.Module == entry.Module && e.Dimension == entry.Dimension));

        if (existing != null)
        {
            matrix.Entries.Remove(existing);
            entry.Id = existing.Id; // preserve original ID
        }

        matrix.Entries.Add(entry);
        matrix.LastUpdatedAt = DateTime.UtcNow;

        return Task.FromResult(entry);
    }

    public Task MarkEntryCompleteAsync(
        string sessionId,
        string clientId,
        string entryId)
    {
        var matrix = _matrixStore.GetValueOrDefault(StoreKey(sessionId, clientId));
        var entry = matrix?.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            entry.IsComplete = true;
            entry.CoveragePercentage = 100;
        }
        return Task.CompletedTask;
    }

    public Task<List<ValidationMatrixEntry>> GetModuleEntriesAsync(
        string sessionId,
        string clientId,
        string module)
    {
        var matrix = _matrixStore.GetValueOrDefault(StoreKey(sessionId, clientId));
        return Task.FromResult(
            matrix?.Entries.Where(e => e.Module == module).ToList() ?? new());
    }

    public Task<List<ValidationMatrixEntry>> GetDimensionEntriesAsync(
        string sessionId,
        string clientId,
        string dimension)
    {
        var matrix = _matrixStore.GetValueOrDefault(StoreKey(sessionId, clientId));
        return Task.FromResult(
            matrix?.Entries.Where(e => e.Dimension == dimension).ToList() ?? new());
    }

    public List<ValidationMatrixEntry> PrioritizeEntries(ValidationMatrix matrix)
    {
        return matrix.Entries
            .OrderBy(e => e.Priority)
            .ThenByDescending(e => e.BusinessLogicItemIds.Count)
            .ThenByDescending(e => e.EstimatedTestCases)
            .ToList();
    }

    public List<ValidationMatrixEntry> GetCriticalPath(ValidationMatrix matrix)
    {
        return matrix.Entries
            .Where(e => e.Priority == MatrixPriority.Critical)
            .OrderByDescending(e => e.BusinessLogicItemIds.Count)
            .ToList();
    }

    public int EstimateTotalTestCases(ValidationMatrix matrix)
    {
        return matrix.Entries.Sum(e => e.EstimatedTestCases);
    }

    public async Task UpdateCoverageAsync(
        string sessionId,
        string clientId,
        string entryId,
        int actualTestCases)
    {
        var matrix = _matrixStore.GetValueOrDefault(StoreKey(sessionId, clientId));
        var entry = matrix?.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry == null) return;

        entry.ActualTestCases = actualTestCases;
        entry.CoveragePercentage = entry.EstimatedTestCases > 0
            ? Math.Min(100, (double)actualTestCases / entry.EstimatedTestCases * 100)
            : 0;

        if (matrix != null)
        {
            matrix.OverallCoverage = CalculateCoverage(matrix);
            matrix.LastUpdatedAt = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }

    public double CalculateCoverage(ValidationMatrix matrix)
    {
        if (matrix.Entries.Count == 0) return 0;

        var totalEstimated = matrix.Entries.Sum(e => e.EstimatedTestCases);
        var totalActual = matrix.Entries.Sum(e => e.ActualTestCases);

        return totalEstimated > 0
            ? Math.Min(100.0, (double)totalActual / totalEstimated * 100)
            : 0;
    }

    public List<ValidationMatrixEntry> FindCoverageGaps(
        ValidationMatrix matrix,
        double threshold = 50.0)
    {
        return matrix.Entries
            .Where(e => !e.IsComplete && e.CoveragePercentage < threshold)
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.CoveragePercentage)
            .ToList();
    }

    public string GenerateMatrixReport(ValidationMatrix matrix)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Validation Matrix Report");
        sb.AppendLine($"Session: {matrix.SessionId} | Generated: {matrix.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Coverage: {matrix.OverallCoverage:F1}% | Entries: {matrix.TotalEntries} ({matrix.CriticalEntries} Critical, {matrix.HighEntries} High)");
        sb.AppendLine();

        sb.AppendLine("## Module × Dimension Matrix");
        sb.AppendLine();

        // Header row
        sb.Append("| Module |");
        foreach (var dim in matrix.Dimensions)
            sb.Append($" {dim.Replace("Accuracy", "").Replace("Consistency", "").Replace("Integrity", "")} |");
        sb.AppendLine();

        sb.Append("|--------|");
        foreach (var _ in matrix.Dimensions)
            sb.Append("------|");
        sb.AppendLine();

        // Data rows
        foreach (var module in matrix.Modules)
        {
            sb.Append($"| **{module}** |");
            foreach (var dim in matrix.Dimensions)
            {
                var entry = matrix.Entries.FirstOrDefault(e => e.Module == module && e.Dimension == dim);
                var cell = entry == null ? "-"
                    : entry.Priority == MatrixPriority.Critical ? "🔴C"
                    : entry.Priority == MatrixPriority.High ? "🟠H"
                    : entry.Priority == MatrixPriority.Medium ? "🟡M"
                    : "🟢L";
                sb.Append($" {cell} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Legend: 🔴 Critical | 🟠 High | 🟡 Medium | 🟢 Low");
        sb.AppendLine();

        // Coverage gaps
        var gaps = FindCoverageGaps(matrix);
        if (gaps.Count > 0)
        {
            sb.AppendLine("## Coverage Gaps");
            foreach (var gap in gaps.Take(10))
                sb.AppendLine($"- [{gap.Priority}] {gap.Module} × {gap.Dimension}: {gap.CoveragePercentage:F0}% covered ({gap.ActualTestCases}/{gap.EstimatedTestCases} tests)");
        }

        return sb.ToString();
    }

    public void Configure(ValidationMatrixConfig config)
    {
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string StoreKey(string sessionId, string clientId) => $"{sessionId}:{clientId}";

    private static List<BusinessLogicItem> FilterItemsForDimension(
        List<BusinessLogicItem> moduleItems,
        string dimension)
    {
        return dimension switch
        {
            "FinancialAccuracy" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.Calculation ||
                i.Description.Contains("tax", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("invoice", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("payment", StringComparison.OrdinalIgnoreCase)).ToList(),

            "InventoryConsistency" => moduleItems.Where(i =>
                i.Description.Contains("stock", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("inventory", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("material", StringComparison.OrdinalIgnoreCase)).ToList(),

            "WorkflowIntegrity" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.Workflow ||
                i.Description.Contains("approval", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("status", StringComparison.OrdinalIgnoreCase)).ToList(),

            "DataIntegrity" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.Validation ||
                i.Type == BusinessLogicType.DataTransformation).ToList(),

            "SecurityAccessControl" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.AccessControl ||
                i.Description.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains("role", StringComparison.OrdinalIgnoreCase)).ToList(),

            "BusinessRules" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.BusinessRule ||
                i.Type == BusinessLogicType.EdgeCase).ToList(),

            "Reporting" => moduleItems.Where(i =>
                i.Type == BusinessLogicType.Reporting).ToList(),

            _ => moduleItems.Take(moduleItems.Count / 4 + 1).ToList() // default: share load
        };
    }

    private MatrixPriority DeterminePriority(
        string module,
        string dimension,
        List<BusinessLogicItem> relatedItems,
        List<string> priorityDimensions)
    {
        // Module-specific priority overrides
        if (priorityDimensions.Contains(dimension))
        {
            if (relatedItems.Any(i => i.Risk == BusinessLogicRisk.Critical))
                return MatrixPriority.Critical;
            return MatrixPriority.High;
        }

        if (!relatedItems.Any())
            return MatrixPriority.Low;

        if (relatedItems.Any(i => i.Risk == BusinessLogicRisk.Critical))
            return MatrixPriority.Critical;

        if (relatedItems.Count(i => i.Risk == BusinessLogicRisk.High) >= 3)
            return MatrixPriority.High;

        if (relatedItems.Any(i => i.Risk == BusinessLogicRisk.High))
            return MatrixPriority.Medium;

        return MatrixPriority.Low;
    }

    private int EstimateTestCasesForEntry(MatrixPriority priority, int relatedItemCount)
    {
        var baseCount = priority switch
        {
            MatrixPriority.Critical => _config.MinTestCasesForCritical,
            MatrixPriority.High => _config.MinTestCasesForHigh,
            MatrixPriority.Medium => _config.MinTestCasesForMedium,
            _ => _config.MinTestCasesForLow
        };

        // Scale with number of related logic items
        return baseCount + Math.Min(relatedItemCount * 2, baseCount * 2);
    }

    private static bool HasCrossDimensionDependency(string module, string dimension)
    {
        // Finance × DataIntegrity always depends on FinancialAccuracy
        return (module is "Finance" or "Accounting") &&
               dimension is "DataIntegrity" or "Reporting";
    }

    private static List<string> BuildAcceptanceCriteria(
        string module,
        string dimension,
        MatrixPriority priority)
    {
        var criteria = new List<string>();

        if (priority == MatrixPriority.Critical)
            criteria.Add("100% of test cases must pass with zero tolerance");

        criteria.Add(dimension switch
        {
            "FinancialAccuracy" => "All monetary values match to 2 decimal places",
            "DataIntegrity" => "No data loss, referential integrity maintained",
            "WorkflowIntegrity" => "All state transitions produce identical outcomes",
            "SecurityAccessControl" => "Role-based restrictions enforced identically",
            "Performance" => "Response time within 20% of legacy system baseline",
            _ => $"{dimension} behavior matches legacy system exactly"
        });

        return criteria;
    }
}
