namespace ValidationOrchestrator.Comparison;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Anthropic;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Extracts business logic from WinForms C# source code.
/// Uses regex pattern matching for fast extraction, then Claude AI for deep semantic analysis.
/// </summary>
public class BusinessLogicExtractor : IBusinessLogicExtractor
{
    private readonly ILogger _logger;
    private readonly AnthropicApi _anthropic;
    private BusinessLogicExtractionConfig _config;

    public BusinessLogicExtractor(AnthropicApi? anthropic = null, ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _anthropic = anthropic ?? new AnthropicApi();
        _config = new BusinessLogicExtractionConfig();
        _logger.Information("BusinessLogicExtractor initialized");
    }

    public async Task<List<BusinessLogicItem>> ExtractFromFileAsync(
        CodeFile file,
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new List<BusinessLogicItem>();

        try
        {
            _logger.Debug("Extracting business logic from {File}", file.FileName);

            // Fast pattern-based passes
            items.AddRange(ExtractCalculations(file));
            items.AddRange(ExtractValidationRules(file));
            items.AddRange(ExtractWorkflows(file));
            items.AddRange(ExtractBusinessRules(file));

            // AI-powered deep extraction
            if (_config.UseAiExtraction && !cancellationToken.IsCancellationRequested)
            {
                var aiItems = await ExtractWithAiAsync(file, sessionId, cancellationToken);
                items.AddRange(aiItems);
            }

            // Post-process
            items = DeduplicateItems(items);

            if (_config.GeneratePseudoCode)
            {
                foreach (var item in items.Where(i => i.PseudoCode == null))
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    item.PseudoCode = await GeneratePseudoCodeAsync(item, cancellationToken);
                }
            }

            file.IsAnalyzed = true;
            file.ItemsExtracted = items.Count;

            _logger.Information(
                "Extracted {Count} business logic items from {File} in {Ms}ms",
                items.Count, file.FileName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to extract from {File}", file.FileName);
        }
        finally
        {
            stopwatch.Stop();
        }

        return items;
    }

    public async Task<List<BusinessLogicItem>> ExtractFromModuleAsync(
        List<CodeFile> files,
        string module,
        string sessionId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var allItems = new List<BusinessLogicItem>();

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested) break;

            file.Module = module;
            var items = await ExtractFromFileAsync(file, sessionId, clientId, cancellationToken);
            allItems.AddRange(items);
        }

        return RankByRisk(allItems);
    }

    public async Task<List<BusinessLogicItem>> ExtractFromCodebaseAsync(
        List<CodeFile> files,
        string sessionId,
        string clientId,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var allItems = new List<BusinessLogicItem>();
        int processed = 0;

        // Group by module for parallel processing
        var byModule = GroupByModule(files.Select(f =>
        {
            f.Module = DetectModule(f);
            return new BusinessLogicItem { Module = f.Module, SourceFile = f.FilePath };
        }).ToList());

        // Process priority modules first
        var priorityFiles = files
            .OrderByDescending(f => _config.PriorityModules.Contains(f.Module))
            .ToList();

        foreach (var file in priorityFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var items = await ExtractFromFileAsync(file, sessionId, clientId, cancellationToken);
            allItems.AddRange(items);

            processed++;
            progress?.Report((int)((double)processed / files.Count * 100));
        }

        return RankByRisk(DeduplicateItems(allItems));
    }

    public async Task<List<CodeFile>> DiscoverCodeFilesAsync(
        string rootPath,
        BusinessLogicExtractionConfig config)
    {
        var files = new List<CodeFile>();

        try
        {
            if (!Directory.Exists(rootPath))
            {
                _logger.Warning("Source path not found: {Path}", rootPath);
                return files;
            }

            var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);

            int processed = 0;
            foreach (var path in allFiles)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (!config.FileExtensions.Contains(ext)) continue;

                // Skip excluded paths
                if (config.ExcludePaths.Any(e =>
                    path.Replace('\\', '/').Contains(e, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var info = new FileInfo(path);
                if (info.Length / 1024 > config.MaxFileSizeKb) continue;

                var content = await File.ReadAllTextAsync(path);
                var file = new CodeFile
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path),
                    Content = content,
                    SizeBytes = info.Length,
                    LineCount = content.Split('\n').Length,
                    Language = ext == ".vb" ? "VB" : "CSharp",
                    LastModified = info.LastWriteTime
                };
                file.Module = DetectModule(file);
                files.Add(file);

                processed++;
                if (config.MaxFiles > 0 && processed >= config.MaxFiles) break;
            }

            _logger.Information("Discovered {Count} source files under {Path}", files.Count, rootPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to discover code files in {Path}", rootPath);
        }

        return files;
    }

    public string DetectModule(CodeFile file)
    {
        var pathAndContent = file.FilePath + " " + file.FileName;

        foreach (var kvp in _config.ModuleKeywords)
        {
            if (pathAndContent.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        // Fall back to top directory name
        var parts = file.FilePath.Replace('\\', '/').Split('/');
        if (parts.Length >= 2)
            return parts[^2];

        return "General";
    }

    public List<BusinessLogicItem> ExtractCalculations(CodeFile file)
    {
        var items = new List<BusinessLogicItem>();

        foreach (var pattern in _config.CalculationPatterns)
        {
            var matches = Regex.Matches(file.Content, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(file.Content, match.Index);
                var snippet = ExtractSnippetAround(file.Content, match.Index, 5);

                items.Add(new BusinessLogicItem
                {
                    Module = file.Module,
                    Name = $"Calculation at line {lineNumber}",
                    Description = $"Arithmetic or aggregation expression detected",
                    Type = BusinessLogicType.Calculation,
                    SourceFile = file.FilePath,
                    LineNumber = lineNumber,
                    CodeSnippet = _config.IncludeCodeSnippets ? snippet : null,
                    Risk = BusinessLogicRisk.High,
                    IsCritical = file.Module is "Finance" or "Accounting" or "Payroll"
                });
            }
        }

        return items;
    }

    public List<BusinessLogicItem> ExtractValidationRules(CodeFile file)
    {
        var items = new List<BusinessLogicItem>();
        var pattern = @"(if\s*\(.*(?:!=|==|>|<|>=|<=).*\)|throw\s+new\s+\w+Exception|\.IsNullOrEmpty\(|\.IsNullOrWhiteSpace\()";

        var matches = Regex.Matches(file.Content, pattern, RegexOptions.Multiline);
        foreach (Match match in matches)
        {
            var lineNumber = GetLineNumber(file.Content, match.Index);
            items.Add(new BusinessLogicItem
            {
                Module = file.Module,
                Name = $"Validation rule at line {lineNumber}",
                Description = "Input validation or guard clause detected",
                Type = BusinessLogicType.Validation,
                SourceFile = file.FilePath,
                LineNumber = lineNumber,
                CodeSnippet = _config.IncludeCodeSnippets
                    ? ExtractSnippetAround(file.Content, match.Index, 3)
                    : null,
                Risk = BusinessLogicRisk.Medium
            });
        }

        return items;
    }

    public List<BusinessLogicItem> ExtractWorkflows(CodeFile file)
    {
        var items = new List<BusinessLogicItem>();

        // Detect state machine patterns and approval chains
        var patterns = new[]
        {
            @"switch\s*\(\s*\w*[Ss]tatus\w*\s*\)",
            @"ApprovalStatus|WorkflowState|OrderStatus|InvoiceStatus",
            @"\.Approve\(|\.Reject\(|\.Submit\(|\.Authorize\("
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(file.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(file.Content, match.Index);
                items.Add(new BusinessLogicItem
                {
                    Module = file.Module,
                    Name = $"Workflow logic at line {lineNumber}",
                    Description = "State transition or approval workflow detected",
                    Type = BusinessLogicType.Workflow,
                    SourceFile = file.FilePath,
                    LineNumber = lineNumber,
                    CodeSnippet = _config.IncludeCodeSnippets
                        ? ExtractSnippetAround(file.Content, match.Index, 8)
                        : null,
                    Risk = BusinessLogicRisk.High,
                    IsCritical = true
                });
            }
        }

        return items;
    }

    public List<BusinessLogicItem> ExtractBusinessRules(CodeFile file)
    {
        var items = new List<BusinessLogicItem>();

        // Detect constants and thresholds
        var patterns = new[]
        {
            @"const\s+decimal\s+\w+\s*=\s*[\d\.]+",
            @"const\s+int\s+\w+(?:Limit|Threshold|Max|Min)\s*=",
            @"private\s+(?:static\s+)?readonly\s+decimal",
            @"ConfigurationManager\.AppSettings\["
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(file.Content, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(file.Content, match.Index);
                items.Add(new BusinessLogicItem
                {
                    Module = file.Module,
                    Name = $"Business rule / constant at line {lineNumber}",
                    Description = "Business rule threshold or policy constant detected",
                    Type = BusinessLogicType.BusinessRule,
                    SourceFile = file.FilePath,
                    LineNumber = lineNumber,
                    CodeSnippet = _config.IncludeCodeSnippets
                        ? ExtractSnippetAround(file.Content, match.Index, 2)
                        : null,
                    Risk = BusinessLogicRisk.High
                });
            }
        }

        return items;
    }

    public async Task<List<BusinessLogicItem>> ExtractWithAiAsync(
        CodeFile file,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<BusinessLogicItem>();

        try
        {
            // Truncate large files to fit context
            var content = file.Content.Length > 8000
                ? file.Content[..8000] + "\n// ... [truncated]"
                : file.Content;

            var prompt = BuildExtractionPrompt(file, content);

            var response = await _anthropic.CreateMessageAsync(
                model: "claude-sonnet-4-6",
                messages: [prompt],
                maxTokens: 2048,
                cancellationToken: cancellationToken);

            var text = response.Content.Value2?
                .Where(b => b.IsText)
                .Select(b => b.Text?.Text)
                .FirstOrDefault(t => t != null) ?? string.Empty;
            items = ParseAiResponse(text, file);

            _logger.Debug("AI extracted {Count} items from {File}", items.Count, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AI extraction failed for {File}, falling back to pattern-only", file.FileName);
        }

        return items;
    }

    public async Task<string> GeneratePseudoCodeAsync(
        BusinessLogicItem item,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(item.CodeSnippet))
            return item.Description;

        try
        {
            var prompt = $"""
                Convert the following C# code snippet to plain English pseudocode that a business analyst can understand.
                Be concise (2-4 sentences max). Focus on WHAT the logic does, not HOW.

                Module: {item.Module}
                Code:
                ```csharp
                {item.CodeSnippet}
                ```

                Pseudocode:
                """;

            var response = await _anthropic.CreateMessageAsync(
                model: "claude-haiku-4-5-20251001",
                messages: [prompt],
                maxTokens: 200);

            return response.Content.Value2?
                .Where(b => b.IsText)
                .Select(b => b.Text?.Text?.Trim())
                .FirstOrDefault(t => t != null)
                ?? item.Description;
        }
        catch
        {
            return item.Description;
        }
    }

    public List<BusinessLogicItem> DeduplicateItems(List<BusinessLogicItem> items)
    {
        // Group by file + line number proximity, keep highest-detail item
        return items
            .GroupBy(i => $"{i.SourceFile}:{(i.LineNumber / 5) * 5}") // bucket by 5-line windows
            .Select(g => g.OrderByDescending(i => i.Description.Length).First())
            .ToList();
    }

    public List<BusinessLogicItem> RankByRisk(List<BusinessLogicItem> items)
    {
        return items
            .OrderBy(i => i.Risk)              // Critical first
            .ThenByDescending(i => i.IsCritical)
            .ThenByDescending(i => i.IsTribalKnowledge)
            .ToList();
    }

    public Dictionary<string, List<BusinessLogicItem>> GroupByModule(List<BusinessLogicItem> items)
    {
        return items
            .GroupBy(i => i.Module)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void Configure(BusinessLogicExtractionConfig config)
    {
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int GetLineNumber(string content, int charIndex)
    {
        return content[..charIndex].Count(c => c == '\n') + 1;
    }

    private static string ExtractSnippetAround(string content, int charIndex, int contextLines)
    {
        var lines = content.Split('\n');
        var lineNumber = content[..charIndex].Count(c => c == '\n');
        var start = Math.Max(0, lineNumber - contextLines / 2);
        var end = Math.Min(lines.Length - 1, lineNumber + contextLines);
        return string.Join('\n', lines[start..(end + 1)]);
    }

    private static string BuildExtractionPrompt(CodeFile file, string content)
    {
        return $"""
            Analyze the following C# WinForms ERP code and extract all business logic items.
            For each item, respond with a JSON array. Each element should have:
            - "name": short name
            - "type": one of Calculation|Validation|Workflow|BusinessRule|DataTransformation|EdgeCase
            - "description": what this logic does in business terms
            - "risk": Critical|High|Medium|Low
            - "isTribalKnowledge": true if undocumented/surprising
            - "lineHint": approximate line number

            Module: {file.Module}
            File: {file.FileName}

            ```csharp
            {content}
            ```

            Respond ONLY with a valid JSON array. No markdown, no explanation.
            """;
    }

    private List<BusinessLogicItem> ParseAiResponse(string json, CodeFile file)
    {
        var items = new List<BusinessLogicItem>();

        try
        {
            // Strip markdown fences if present
            json = json.Trim();
            if (json.StartsWith("```")) json = Regex.Replace(json, @"```\w*\n?", "").Trim();

            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            if (parsed == null) return items;

            foreach (var entry in parsed)
            {
                var typeStr = entry.GetValueOrDefault("type")?.ToString() ?? "BusinessRule";
                var riskStr = entry.GetValueOrDefault("risk")?.ToString() ?? "Medium";

                Enum.TryParse<BusinessLogicType>(typeStr, out var type);
                Enum.TryParse<BusinessLogicRisk>(riskStr, out var risk);

                int.TryParse(entry.GetValueOrDefault("lineHint")?.ToString(), out var line);
                bool.TryParse(entry.GetValueOrDefault("isTribalKnowledge")?.ToString(), out var tribal);

                items.Add(new BusinessLogicItem
                {
                    Module = file.Module,
                    Name = entry.GetValueOrDefault("name")?.ToString() ?? "AI-discovered item",
                    Description = entry.GetValueOrDefault("description")?.ToString() ?? string.Empty,
                    Type = type,
                    SourceFile = file.FilePath,
                    LineNumber = line > 0 ? line : null,
                    Risk = risk,
                    IsTribalKnowledge = tribal,
                    IsCritical = risk == BusinessLogicRisk.Critical
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse AI extraction response");
        }

        return items;
    }
}
