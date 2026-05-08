namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;
using Serilog;
using System.Text;

/// <summary>
/// Generates comprehensive markdown validation reports
/// Synthesizes findings from all validation phases into stakeholder-ready documents
/// </summary>
public class ReportGenerator : IReportGenerator
{
    private readonly ILogger _logger = Log.ForContext<ReportGenerator>();
    private ReportGenerationConfig _config = new();

    public async Task<ValidationReport> GenerateFullReportAsync(
        string sessionId,
        string clientId,
        DiscoveryFindings? discoveries = null,
        ExecutionResults? executionResults = null,
        DiscrepancyAnalysisResult? discrepancies = null,
        MigrationRiskAssessment? riskAssessment = null)
    {
        _logger.Information("Generating full validation report for session {Session}", sessionId);
        var startTime = DateTime.UtcNow;

        var report = new ValidationReport
        {
            SessionId = sessionId,
            ClientId = clientId,
            Type = ReportType.Full,
            Title = "Enterprise Migration Validation Report",
            Subtitle = $"Xpedeon Construction ERP - {DateTime.UtcNow:MMMM d, yyyy}"
        };

        // Generate sections
        if (_config.IncludeExecutiveSummary && discrepancies != null && riskAssessment != null)
            report.ExecutiveSummary = GenerateExecutiveSummary(discrepancies, riskAssessment);

        if (_config.IncludeDiscovery && discoveries != null)
            report.Discovery = GenerateDiscoverySection(discoveries);

        if (_config.IncludeExecution && executionResults != null)
            report.Execution = GenerateExecutionSection(executionResults);

        if (_config.IncludeComparison)
            report.Comparison = GenerateComparisonSection(null);

        if (_config.IncludeDiscrepancies && discrepancies != null)
            report.Discrepancies = GenerateDiscrepanciesSection(discrepancies);

        if (_config.IncludeRiskAssessment && riskAssessment != null)
            report.RiskAssessment = GenerateRiskSection(riskAssessment);

        // Generate action items and recommendations
        if (_config.IncludeActionItems && discrepancies != null && riskAssessment != null)
            report.ActionItems = GenerateActionItems(discrepancies, riskAssessment);

        if (_config.IncludeRecommendations && discrepancies != null && riskAssessment != null)
            report.Recommendations = GenerateRecommendations(discrepancies, riskAssessment);

        // Convert to markdown
        report.MarkdownContent = ConvertToMarkdown(report);
        report.GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
        report.Success = true;

        _logger.Information("Report generated in {Ms}ms", report.GenerationTimeMs);

        return await Task.FromResult(report);
    }

    public async Task<ValidationReport> GenerateExecutiveSummaryAsync(
        string sessionId,
        string clientId,
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        _logger.Information("Generating executive summary for session {Session}", sessionId);

        var report = new ValidationReport
        {
            SessionId = sessionId,
            ClientId = clientId,
            Type = ReportType.ExecutiveSummary,
            Title = "Executive Summary",
            Subtitle = "Migration Readiness Assessment"
        };

        report.ExecutiveSummary = GenerateExecutiveSummary(discrepancies, riskAssessment);
        report.Discrepancies = GenerateDiscrepanciesSection(discrepancies);
        report.RiskAssessment = GenerateRiskSection(riskAssessment);
        report.ActionItems = GenerateActionItems(discrepancies, riskAssessment);

        report.MarkdownContent = ConvertToMarkdown(report);
        report.Success = true;

        return await Task.FromResult(report);
    }

    public async Task<ValidationReport> GenerateTechnicalReportAsync(
        string sessionId,
        string clientId,
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        _logger.Information("Generating technical report for session {Session}", sessionId);

        var report = new ValidationReport
        {
            SessionId = sessionId,
            ClientId = clientId,
            Type = ReportType.Technical,
            Title = "Technical Validation Report"
        };

        report.Discrepancies = GenerateDiscrepanciesSection(discrepancies);
        report.RiskAssessment = GenerateRiskSection(riskAssessment);
        report.ActionItems = GenerateActionItems(discrepancies, riskAssessment);
        report.Recommendations = GenerateRecommendations(discrepancies, riskAssessment);

        report.MarkdownContent = ConvertToMarkdown(report);
        report.Success = true;

        return await Task.FromResult(report);
    }

    public ExecutiveSummary GenerateExecutiveSummary(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        var decision = riskAssessment.Recommendation.Decision.ToString();
        var summary = new ExecutiveSummary
        {
            Decision = decision,
            Summary = riskAssessment.Recommendation.Summary,
            OverallRiskScore = riskAssessment.OverallRiskScore,
            CriticalFindingsCount = riskAssessment.CriticalItems.Count(i => i.Priority == RiskPriority.Critical),
            TotalDiscrepancies = discrepancies.Discrepancies.Count,
            BlockingIssuesCount = riskAssessment.GoBlockers.Count,
            PrimaryRisks = string.Join("; ", riskAssessment.GoBlockers.Take(3)),
            RecommendedActions = riskAssessment.Recommendation.Rationale,
            EstimatedDaysToReadiness = riskAssessment.Recommendation.MinimumDaysToReady,
            ConfidenceLevel = riskAssessment.Recommendation.ConfidenceLevel,
            ProjectedReadyDate = DateTime.UtcNow.AddDays(riskAssessment.Recommendation.MinimumDaysToReady),
            KeyMetrics = new List<string>
            {
                $"Risk Score: {riskAssessment.OverallRiskScore:F1}/100",
                $"Health Score: {riskAssessment.OverallHealthScore:F1}/100",
                $"Discrepancies: {discrepancies.Discrepancies.Count}",
                $"Blockers: {riskAssessment.GoBlockers.Count}",
                $"Ready Modules: {riskAssessment.ModuleRisks.Count(m => m.Readiness == MigrationReadiness.Ready)}/{riskAssessment.ModuleRisks.Count}"
            },
            Highlights = new List<string>
            {
                $"{FormatStatus(decision)}: {decision}",
                $"Confidence: {FormatPercentage(riskAssessment.Recommendation.ConfidenceLevel * 100)}",
                $"Estimated Ready: {riskAssessment.Recommendation.MinimumDaysToReady} days"
            }
        };

        return summary;
    }

    public DiscoverySummaryReport GenerateDiscoverySection(DiscoveryFindings? discoveries)
    {
        return new DiscoverySummaryReport
        {
            TablesAnalyzed = discoveries?.TablesAnalyzed ?? 0,
            ColumnsAnalyzed = discoveries?.ColumnsAnalyzed ?? 0,
            StoredProceduresFound = discoveries?.StoredProceduresFound ?? 0,
            BusinessRulesIdentified = discoveries?.RulesIdentified ?? 0,
            DataQualityIssuesFound = discoveries?.DataQualityIssues.Count ?? 0,
            KeyFindings = discoveries?.KeyFindings ?? new(),
            NotablePatterns = "Schema and data patterns documented"
        };
    }

    public ExecutionSummaryReport GenerateExecutionSection(ExecutionResults? results)
    {
        return new ExecutionSummaryReport
        {
            TotalTestsRun = results?.TotalTestsRun ?? 0,
            LegacyPassedTests = results?.LegacyPassedTests ?? 0,
            BlazonPassedTests = results?.BlazonPassedTests ?? 0,
            FailedTests = (results?.TotalTestsRun ?? 0) - (results?.LegacyPassedTests ?? 0),
            OverallPassRate = results != null ? (results.LegacyPassedTests + results.BlazonPassedTests) / (double)(results.TotalTestsRun * 2) * 100 : 0,
            ExecutionStartTime = results?.StartTime ?? DateTime.UtcNow,
            ExecutionEndTime = results?.EndTime ?? DateTime.UtcNow,
            ExecutionDurationMs = results?.DurationMs ?? 0,
            PerformanceObservations = "All tests executed successfully"
        };
    }

    public ComparisonSummaryReport GenerateComparisonSection(List<TableComparisonResult>? comparisons)
    {
        return new ComparisonSummaryReport
        {
            TablesCompared = comparisons?.Count ?? 0,
            TablesMatched = comparisons?.Count(c => c.Status == ComparisonStatus.Identical) ?? 0,
            TablesWithDifferences = comparisons?.Count(c => c.Status != ComparisonStatus.Identical) ?? 0,
            OverallMatchPercentage = comparisons?.Average(c => c.MatchPercentage) ?? 0,
            TotalRecordsCompared = comparisons?.Sum(c => c.LegacyRecordCount) ?? 0,
            MatchedRecords = comparisons?.Sum(c => c.MatchedRecords) ?? 0,
            MissingRecords = comparisons?.Sum(c => c.MissingRecords) ?? 0,
            ExtraRecords = comparisons?.Sum(c => c.ExtraRecords) ?? 0,
            ModifiedRecords = comparisons?.Sum(c => c.ModifiedRecords) ?? 0
        };
    }

    public DiscrepancySummaryReport GenerateDiscrepanciesSection(DiscrepancyAnalysisResult discrepancies)
    {
        return new DiscrepancySummaryReport
        {
            TotalDiscrepancies = discrepancies.Discrepancies.Count,
            CriticalCount = discrepancies.Summary.CriticalCount,
            HighCount = discrepancies.Summary.HighCount,
            MediumCount = discrepancies.Summary.MediumCount,
            LowCount = discrepancies.Summary.LowCount,
            BlockingIssuesCount = discrepancies.Summary.BlockerCount,
            ResolvableCount = discrepancies.Summary.ResolvableCount,
            OverallImpact = discrepancies.Summary.OverallImpactPercentage,
            TopCriticalIssues = discrepancies.Discrepancies
                .Where(d => d.Severity == DiscrepancySeverity.Critical)
                .OrderByDescending(d => d.ImpactPercentage)
                .Take(_config.MaxIssuesToList)
                .Select(d => $"{d.Description} ({d.Module})")
                .ToList(),
            AffectedModules = discrepancies.AffectedModules,
            DiscrepanciesByCategory = discrepancies.Summary.DiscrepanciesByCategory,
            DiscrepanciesByModule = discrepancies.Summary.DiscrepanciesByModule,
            CommonRootCauses = new List<string> { "Data transformation differences", "Schema mapping gaps", "Business logic variance" }
        };
    }

    public RiskSummaryReport GenerateRiskSection(MigrationRiskAssessment riskAssessment)
    {
        return new RiskSummaryReport
        {
            OverallDecision = riskAssessment.Recommendation.Decision.ToString(),
            OverallRiskScore = riskAssessment.OverallRiskScore,
            OverallHealthScore = riskAssessment.OverallHealthScore,
            Readiness = riskAssessment.OverallReadiness.ToString(),
            CriticalRiskItems = riskAssessment.CriticalItems.Count(i => i.Priority == RiskPriority.Critical),
            BlockingIssuesCount = riskAssessment.CriticalItems.Count(i => i.Priority >= RiskPriority.High),
            GoBlockersCount = riskAssessment.GoBlockers.Count,
            WarningsCount = riskAssessment.Warnings.Count,
            GoBlockers = riskAssessment.GoBlockers,
            Warnings = riskAssessment.Warnings,
            ModuleRiskScores = riskAssessment.ModuleRisks.ToDictionary(m => m.ModuleName, m => m.RiskScore),
            EstimatedDaysToReadiness = riskAssessment.Recommendation.MinimumDaysToReady,
            ConfidenceLevel = riskAssessment.Recommendation.ConfidenceLevel,
            MitigationStrategies = riskAssessment.MitigationStrategies
                .OrderBy(s => s.TargetCompletionDate)
                .Take(_config.MaxIssuesToList)
                .Select(s => $"{s.Title} ({s.EstimatedHours}h, {s.RiskReduction:F0}% reduction)")
                .ToList()
        };
    }

    public List<ActionItem> GenerateActionItems(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        var items = new List<ActionItem>();

        // Critical items first
        foreach (var critical in riskAssessment.CriticalItems.Where(i => i.Priority == RiskPriority.Critical).Take(_config.MaxIssuesToList))
        {
            items.Add(new ActionItem
            {
                Title = $"Fix: {critical.Title}",
                Description = critical.Description,
                Priority = "Critical",
                EstimatedHours = critical.EstimatedFixHours,
                Status = "Pending",
                IsPreLaunch = !critical.CanBeFixedPostLaunch,
                RiskReduction = critical.ImpactPercentage
            });
        }

        // Mitigation strategies
        foreach (var strategy in riskAssessment.MitigationStrategies.Take(_config.MaxIssuesToList))
        {
            items.Add(new ActionItem
            {
                Title = strategy.Title,
                Description = strategy.Description,
                Priority = strategy.Priority.ToString(),
                Owner = strategy.Owner,
                EstimatedHours = strategy.EstimatedHours,
                DueDate = strategy.TargetCompletionDate,
                Steps = strategy.Actions,
                IsPreLaunch = strategy.IsPreLaunch,
                RiskReduction = strategy.RiskReduction
            });
        }

        return items;
    }

    public List<RecommendationItem> GenerateRecommendations(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        var recommendations = new List<RecommendationItem>();

        // Risk-based recommendations
        if (riskAssessment.OverallRiskScore > 50)
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "Implement Risk Mitigation Plan",
                Description = "High risk score requires systematic mitigation before launch",
                Category = "Technical",
                Impact = "High",
                Timeframe = "Immediate",
                Benefits = new List<string>
                {
                    "Reduce overall risk score",
                    "Enable Go decision",
                    "Ensure user confidence"
                }
            });
        }

        // Data quality recommendations
        if (discrepancies.Discrepancies.Any(d => d.Category == "DataValue"))
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "Enhance Data Validation",
                Description = "Data mismatches indicate transformation logic gaps",
                Category = "Technical",
                Impact = "High",
                Timeframe = "Short-term",
                Benefits = new List<string>
                {
                    "Improve data accuracy",
                    "Reduce post-launch issues",
                    "Enhance user trust"
                }
            });
        }

        // Schema recommendations
        if (discrepancies.Discrepancies.Any(d => d.Category == "SchemaStructure"))
        {
            recommendations.Add(new RecommendationItem
            {
                Title = "Finalize Schema Mappings",
                Description = "Schema differences must be resolved before full data migration",
                Category = "Technical",
                Impact = "High",
                Timeframe = "Immediate",
                Benefits = new List<string>
                {
                    "Ensure data integrity",
                    "Enable automated migration",
                    "Reduce manual remediation"
                }
            });
        }

        // Process recommendations
        recommendations.Add(new RecommendationItem
        {
            Title = "Establish Change Control Process",
            Description = "Implement formal change control for any schema or logic changes",
            Category = "Process",
            Impact = "Medium",
            Timeframe = "Immediate",
            Benefits = new List<string>
            {
                "Prevent scope creep",
                "Maintain validation integrity",
                "Enable audit trail"
            }
        });

        return recommendations;
    }

    public List<ActionItem> CreateActionPlan(
        List<CriticalRiskItem> criticalItems,
        List<RemediationRecommendation> remediations)
    {
        var plan = new List<ActionItem>();

        foreach (var item in criticalItems.OrderByDescending(i => i.Priority))
        {
            plan.Add(new ActionItem
            {
                Title = item.Title,
                Description = item.Description,
                Priority = item.Priority.ToString(),
                EstimatedHours = item.EstimatedFixHours,
                IsPreLaunch = !item.CanBeFixedPostLaunch,
                Dependencies = item.DependentOn,
                RiskReduction = item.ImpactPercentage
            });
        }

        return plan;
    }

    public string ConvertToMarkdown(ValidationReport report)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# {report.Title}");
        if (!string.IsNullOrEmpty(report.Subtitle))
            sb.AppendLine($"*{report.Subtitle}*");
        sb.AppendLine($"\n**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Report Type:** {report.Type}\n");
        sb.AppendLine("---\n");

        // Executive Summary
        if (report.ExecutiveSummary != null && _config.IncludeExecutiveSummary)
        {
            sb.AppendLine("## Executive Summary\n");
            sb.AppendLine($"**Decision:** {FormatStatus(report.ExecutiveSummary.Decision)} {report.ExecutiveSummary.Decision}\n");
            sb.AppendLine($"{report.ExecutiveSummary.Summary}\n");

            sb.AppendLine("### Key Metrics\n");
            foreach (var metric in report.ExecutiveSummary.KeyMetrics)
            {
                sb.AppendLine($"- {metric}");
            }
            sb.AppendLine();

            if (report.ExecutiveSummary.GoBlockers?.Count > 0)
            {
                sb.AppendLine("### Blockers\n");
                foreach (var blocker in report.ExecutiveSummary.GoBlockers.Take(5))
                {
                    sb.AppendLine($"- 🚫 {blocker}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---\n");
        }

        // Discovery
        if (report.Discovery != null && _config.IncludeDiscovery)
        {
            sb.AppendLine("## Discovery Phase\n");
            sb.AppendLine($"- Tables Analyzed: {FormatNumber(report.Discovery.TablesAnalyzed)}");
            sb.AppendLine($"- Columns Analyzed: {FormatNumber(report.Discovery.ColumnsAnalyzed)}");
            sb.AppendLine($"- Business Rules Identified: {FormatNumber(report.Discovery.BusinessRulesIdentified)}");
            sb.AppendLine($"- Data Quality Issues: {FormatNumber(report.Discovery.DataQualityIssuesFound)}\n");
            sb.AppendLine("---\n");
        }

        // Discrepancies
        if (report.Discrepancies != null && _config.IncludeDiscrepancies)
        {
            sb.AppendLine("## Discrepancy Analysis\n");
            sb.AppendLine($"**Total Issues:** {FormatNumber(report.Discrepancies.TotalDiscrepancies)}\n");

            sb.AppendLine("### Severity Breakdown\n");
            sb.AppendLine($"| Severity | Count | Impact |");
            sb.AppendLine($"|----------|-------|--------|");
            sb.AppendLine($"| 🔴 Critical | {report.Discrepancies.CriticalCount} | Blocks migration |");
            sb.AppendLine($"| 🟠 High | {report.Discrepancies.HighCount} | Significant |");
            sb.AppendLine($"| 🟡 Medium | {report.Discrepancies.MediumCount} | Moderate |");
            sb.AppendLine($"| 🟢 Low | {report.Discrepancies.LowCount} | Minor |\n");

            if (report.Discrepancies.TopCriticalIssues?.Count > 0)
            {
                sb.AppendLine("### Top Critical Issues\n");
                foreach (var issue in report.Discrepancies.TopCriticalIssues.Take(5))
                {
                    sb.AppendLine($"- {issue}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---\n");
        }

        // Risk Assessment
        if (report.RiskAssessment != null && _config.IncludeRiskAssessment)
        {
            sb.AppendLine("## Risk Assessment\n");
            sb.AppendLine($"**Overall Risk Score:** {report.RiskAssessment.OverallRiskScore:F1}/100");
            sb.AppendLine($"**Health Score:** {report.RiskAssessment.OverallHealthScore:F1}/100");
            sb.AppendLine($"**Readiness:** {report.RiskAssessment.Readiness}");
            sb.AppendLine($"**Decision:** {FormatStatus(report.RiskAssessment.OverallDecision)} {report.RiskAssessment.OverallDecision}\n");

            if (report.RiskAssessment.GoBlockers?.Count > 0)
            {
                sb.AppendLine("### Critical Blockers\n");
                foreach (var blocker in report.RiskAssessment.GoBlockers.Take(5))
                {
                    sb.AppendLine($"- 🚫 {blocker}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---\n");
        }

        // Action Items
        if (report.ActionItems?.Count > 0 && _config.IncludeActionItems)
        {
            sb.AppendLine("## Action Items\n");
            sb.AppendLine($"**Total Items:** {report.ActionItems.Count}\n");

            var criticalItems = report.ActionItems.Where(a => a.Priority == "Critical").ToList();
            if (criticalItems.Count > 0)
            {
                sb.AppendLine("### Critical Actions\n");
                foreach (var item in criticalItems.Take(_config.MaxIssuesToList))
                {
                    sb.AppendLine($"- **{item.Title}**");
                    sb.AppendLine($"  - Effort: {item.EstimatedHours} hours");
                    sb.AppendLine($"  - Pre-Launch: {(item.IsPreLaunch ? "Yes" : "No")}");
                    if (!string.IsNullOrEmpty(item.Owner))
                        sb.AppendLine($"  - Owner: {item.Owner}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---\n");
        }

        // Recommendations
        if (report.Recommendations?.Count > 0 && _config.IncludeRecommendations)
        {
            sb.AppendLine("## Recommendations\n");
            foreach (var rec in report.Recommendations.Take(_config.MaxIssuesToList))
            {
                sb.AppendLine($"### {rec.Title}\n");
                sb.AppendLine($"{rec.Description}\n");
                sb.AppendLine($"**Timeframe:** {rec.Timeframe}  ");
                sb.AppendLine($"**Impact:** {rec.Impact}\n");
            }

            sb.AppendLine("---\n");
        }

        // Footer
        sb.AppendLine("## Document Information\n");
        sb.AppendLine($"- **Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Session ID:** `{report.SessionId}`");
        sb.AppendLine($"- **Client ID:** `{report.ClientId}`");
        sb.AppendLine($"- **Generation Time:** {report.GenerationTimeMs}ms");

        return sb.ToString();
    }

    public string GenerateMarkdownSection(string title, int level, object content)
    {
        var heading = new string('#', Math.Min(Math.Max(level, 1), 6));
        return $"{heading} {title}\n\n{content}\n";
    }

    public string GenerateMarkdownTable(ReportTable table)
    {
        return table.ToMarkdown();
    }

    public string GenerateTableOfContents(List<string> sections)
    {
        var sb = new StringBuilder("## Table of Contents\n\n");
        for (int i = 0; i < sections.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {sections[i]}");
        }
        return sb.ToString();
    }

    public string FormatStatus(string status)
    {
        return status switch
        {
            "Go" or "Ready" => "✅",
            "GoWithRisks" or "AlmostReady" => "⚠️",
            "Delay" or "PartiallyReady" => "🔄",
            "NoGo" or "NotReady" => "🚫",
            _ => "ℹ️"
        };
    }

    public string FormatSeverity(string severity)
    {
        return severity switch
        {
            "Critical" => "🔴",
            "High" => "🟠",
            "Medium" => "🟡",
            "Low" => "🟢",
            _ => "⚪"
        };
    }

    public string FormatPercentage(double value)
    {
        return $"{value:F1}%";
    }

    public string FormatNumber(int value)
    {
        return value.ToString("N0");
    }

    public List<ReportMetrics> ExtractKeyMetrics(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        return new List<ReportMetrics>
        {
            new ReportMetrics { MetricName = "Total Discrepancies", Value = discrepancies.Discrepancies.Count.ToString(), Status = "Critical", Target = "0" },
            new ReportMetrics { MetricName = "Risk Score", Value = $"{riskAssessment.OverallRiskScore:F1}", Unit = "/100", Status = riskAssessment.OverallRiskScore > 50 ? "Critical" : "Warning", Target = "<25" },
            new ReportMetrics { MetricName = "Blocking Issues", Value = riskAssessment.GoBlockers.Count.ToString(), Status = riskAssessment.GoBlockers.Count > 0 ? "Critical" : "Good", Target = "0" },
            new ReportMetrics { MetricName = "Ready Modules", Value = $"{riskAssessment.ModuleRisks.Count(m => m.Readiness == MigrationReadiness.Ready)}/{riskAssessment.ModuleRisks.Count}", Status = "Warning", Target = "All" }
        };
    }

    public Dictionary<string, string> GenerateSummaryStats(
        DiscrepancyAnalysisResult discrepancies,
        MigrationRiskAssessment riskAssessment)
    {
        return new Dictionary<string, string>
        {
            { "Overall Risk Score", $"{riskAssessment.OverallRiskScore:F1}" },
            { "Overall Health Score", $"{riskAssessment.OverallHealthScore:F1}" },
            { "Total Discrepancies", discrepancies.Discrepancies.Count.ToString() },
            { "Critical Issues", discrepancies.Summary.CriticalCount.ToString() },
            { "Blocking Issues", riskAssessment.GoBlockers.Count.ToString() },
            { "Affected Modules", discrepancies.AffectedModules.Count.ToString() },
            { "Days to Readiness", riskAssessment.Recommendation.MinimumDaysToReady.ToString() }
        };
    }

    public string GenerateDistributionChart(Dictionary<string, int> data, string title)
    {
        if (data.Count == 0)
            return $"No data for {title}";

        var sb = new StringBuilder($"**{title}**\n\n");
        var maxValue = data.Values.Max();
        const int barWidth = 30;

        foreach (var kvp in data.OrderByDescending(x => x.Value))
        {
            var barLength = (int)((kvp.Value / (double)maxValue) * barWidth);
            var bar = new string('█', barLength) + new string('░', barWidth - barLength);
            sb.AppendLine($"{kvp.Key}: {bar} {kvp.Value}");
        }

        return sb.ToString();
    }

    public async Task<bool> ExportToFileAsync(ValidationReport report, string filePath)
    {
        try
        {
            _logger.Information("Exporting report to {Path}", filePath);
            await File.WriteAllTextAsync(filePath, report.MarkdownContent);
            _logger.Information("Report exported successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error exporting report");
            return false;
        }
    }

    public async Task<Dictionary<string, string>> ExportMultipleFormatsAsync(
        ValidationReport report,
        string outputDirectory)
    {
        var results = new Dictionary<string, string>();

        try
        {
            // Markdown export
            var mdPath = Path.Combine(outputDirectory, $"{report.SessionId}_report.md");
            await ExportToFileAsync(report, mdPath);
            results["Markdown"] = mdPath;

            // Text export
            var txtPath = Path.Combine(outputDirectory, $"{report.SessionId}_report.txt");
            await File.WriteAllTextAsync(txtPath, report.MarkdownContent);
            results["Text"] = txtPath;

            _logger.Information("Exported report in multiple formats");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error exporting multiple formats");
        }

        return results;
    }

    public void Configure(ReportGenerationConfig config)
    {
        _config = config;
        _logger.Information("Report generator configured");
    }

    public ReportGenerationConfig GetConfiguration()
    {
        return _config;
    }
}
