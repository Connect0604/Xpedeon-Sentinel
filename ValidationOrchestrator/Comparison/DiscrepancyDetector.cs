namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;
using Serilog;

/// <summary>
/// Detects and analyzes discrepancies from comparison results
/// Synthesizes findings into actionable insights for migration validation
/// </summary>
public class DiscrepancyDetector : IDiscrepancyDetector
{
    private readonly ILogger _logger = Log.ForContext<DiscrepancyDetector>();
    private DiscrepancyDetectionConfig _config = new();
    private List<DiscrepancyPattern> _patterns = new();

    public DiscrepancyDetector()
    {
        InitializeDefaultPatterns();
    }

    public async Task<DiscrepancyAnalysisResult> DetectFromTableComparisonAsync(
        TableComparisonResult comparisonResult,
        string sessionId,
        string clientId)
    {
        _logger.Information("Detecting discrepancies from table comparison: {Table}", comparisonResult.TableName);

        var discrepancies = new List<DetailedDiscrepancy>();

        // Detect data value mismatches
        if (comparisonResult.MatchPercentage < 100)
        {
            var impactPercentage = 100 - comparisonResult.MatchPercentage;
            var severity = DetermineSeverity(impactPercentage, (int)comparisonResult.ModifiedRecords);

            discrepancies.Add(CreateDataDiscrepancy(
                module: "DataComparison",
                table: comparisonResult.TableName,
                category: "DataValue",
                description: $"Data mismatch detected: {comparisonResult.MatchPercentage:F1}% records match",
                severity: severity,
                affectedRecordCount: (int)comparisonResult.ModifiedRecords,
                impactPercentage: impactPercentage
            ));
        }

        // Detect missing rows
        if (comparisonResult.MissingRecords > 0)
        {
            var impactPercentage = (double)comparisonResult.MissingRecords / comparisonResult.LegacyRecordCount * 100;
            discrepancies.Add(CreateDataDiscrepancy(
                module: "DataComparison",
                table: comparisonResult.TableName,
                category: "Completeness",
                description: $"Missing rows in Blazor system: {comparisonResult.MissingRecords} records not found",
                severity: DetermineSeverity(impactPercentage, (int)comparisonResult.MissingRecords),
                affectedRecordCount: (int)comparisonResult.MissingRecords,
                impactPercentage: impactPercentage
            ));
        }

        // Detect extra rows
        if (comparisonResult.ExtraRecords > 0)
        {
            var impactPercentage = (double)comparisonResult.ExtraRecords / comparisonResult.BlazonRecordCount * 100;
            discrepancies.Add(CreateDataDiscrepancy(
                module: "DataComparison",
                table: comparisonResult.TableName,
                category: "Consistency",
                description: $"Extra rows in Blazor system: {comparisonResult.ExtraRecords} records not in legacy",
                severity: DetermineSeverity(impactPercentage, (int)comparisonResult.ExtraRecords),
                affectedRecordCount: (int)comparisonResult.ExtraRecords,
                impactPercentage: impactPercentage
            ));
        }

        // Detect column-specific mismatches
        foreach (var columnComparison in comparisonResult.ColumnComparisons)
        {
            if (columnComparison.MatchPercentage < _config.AcceptableMatchPercentage)
            {
                var impactPercentage = 100 - columnComparison.MatchPercentage;
                discrepancies.Add(new DetailedDiscrepancy
                {
                    Module = "DataComparison",
                    Table = comparisonResult.TableName,
                    Column = columnComparison.ColumnName,
                    Category = columnComparison.TypeConversionIssues > 0 ? "DataType" : "DataValue",
                    Description = $"Column mismatch: {columnComparison.ColumnName} ({columnComparison.MatchPercentage:F1}% match)",
                    Severity = DetermineSeverity(impactPercentage, (int)columnComparison.DifferentValues),
                    AffectedRecordCount = (int)columnComparison.DifferentValues,
                    ImpactPercentage = impactPercentage,
                    Evidence = columnComparison.SampleDifferences.Take(3).ToList(),
                    IsResolvable = true
                });
            }
        }

        // Apply grouping if enabled
        var groups = _config.EnableGrouping ? GroupDiscrepancies(discrepancies) : new();

        // Generate summary
        var summary = GenerateSummary(discrepancies);

        // Assess module impacts
        var impacts = AssessModuleImpacts(discrepancies);

        var result = new DiscrepancyAnalysisResult
        {
            SessionId = sessionId,
            ClientId = clientId,
            Discrepancies = discrepancies,
            GroupedDiscrepancies = groups,
            Summary = summary,
            ModuleImpacts = impacts,
            AffectedModules = impacts.Keys.ToList(),
            Success = true,
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.Information(
            "Detected {Count} discrepancies from table comparison: {Table}",
            discrepancies.Count,
            comparisonResult.TableName);

        return await Task.FromResult(result);
    }

    public async Task<DiscrepancyAnalysisResult> DetectFromSchemaAnalysisAsync(
        SchemaAnalysisResult schemaAnalysis,
        string sessionId,
        string clientId)
    {
        _logger.Information("Detecting discrepancies from schema analysis");

        var discrepancies = new List<DetailedDiscrepancy>();

        if (schemaAnalysis.Differences != null)
        {
            foreach (var diff in schemaAnalysis.Differences)
            {
                var severity = diff.Severity switch
                {
                    DifferenceSeverity.Critical => DiscrepancySeverity.Critical,
                    DifferenceSeverity.High => DiscrepancySeverity.High,
                    DifferenceSeverity.Medium => DiscrepancySeverity.Medium,
                    _ => DiscrepancySeverity.Low
                };

                discrepancies.Add(CreateSchemaDiscrepancy(
                    module: "SchemaAnalysis",
                    table: diff.Table ?? "Unknown",
                    column: diff.Column,
                    description: $"{diff.DifferenceType}: {diff.LegacyValue} → {diff.BlazonValue}",
                    severity: severity,
                    isBlocker: severity == DiscrepancySeverity.Critical
                ));
            }
        }

        // Check compatibility
        if (schemaAnalysis.Compatibility != SchemaCompatibility.FullyCompatible)
        {
            var severity = schemaAnalysis.Compatibility switch
            {
                SchemaCompatibility.Incompatible => DiscrepancySeverity.Critical,
                SchemaCompatibility.Partial => DiscrepancySeverity.High,
                _ => DiscrepancySeverity.Medium
            };

            discrepancies.Add(new DetailedDiscrepancy
            {
                Module = "SchemaAnalysis",
                Table = "Schema",
                Category = "SchemaStructure",
                Description = $"Schema compatibility: {schemaAnalysis.Compatibility}",
                Severity = severity,
                IsBlocker = severity == DiscrepancySeverity.Critical,
                IsResolvable = false
            });
        }

        var groups = _config.EnableGrouping ? GroupDiscrepancies(discrepancies) : new();
        var summary = GenerateSummary(discrepancies);
        var impacts = AssessModuleImpacts(discrepancies);

        var result = new DiscrepancyAnalysisResult
        {
            SessionId = sessionId,
            ClientId = clientId,
            Discrepancies = discrepancies,
            GroupedDiscrepancies = groups,
            Summary = summary,
            ModuleImpacts = impacts,
            AffectedModules = impacts.Keys.ToList(),
            Success = true,
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.Information("Detected {Count} discrepancies from schema analysis", discrepancies.Count);

        return await Task.FromResult(result);
    }

    public async Task<DiscrepancyAnalysisResult> AnalyzeAllComparisonsAsync(
        List<TableComparisonResult> tableComparisons,
        SchemaAnalysisResult schemaAnalysis,
        string sessionId,
        string clientId)
    {
        _logger.Information("Analyzing all comparisons: {Tables} tables", tableComparisons.Count);

        var allDiscrepancies = new List<DetailedDiscrepancy>();
        var startTime = DateTime.UtcNow;

        // Analyze schema first
        var schemaResult = await DetectFromSchemaAnalysisAsync(schemaAnalysis, sessionId, clientId);
        allDiscrepancies.AddRange(schemaResult.Discrepancies);

        // Analyze each table comparison
        foreach (var comparison in tableComparisons)
        {
            var tableResult = await DetectFromTableComparisonAsync(comparison, sessionId, clientId);
            allDiscrepancies.AddRange(tableResult.Discrepancies);
        }

        // Correlate related discrepancies
        foreach (var discrepancy in allDiscrepancies)
        {
            var related = FindRelatedDiscrepancies(discrepancy, allDiscrepancies);
            if (related.Any())
            {
                discrepancy.RelatedDiscrepancyId = related.First().Id;
            }
        }

        // Group and summarize
        var groups = _config.EnableGrouping ? GroupDiscrepancies(allDiscrepancies) : new();
        var summary = GenerateSummary(allDiscrepancies);
        var impacts = AssessModuleImpacts(allDiscrepancies);
        var metrics = GenerateMetrics(allDiscrepancies, summary);

        var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        var result = new DiscrepancyAnalysisResult
        {
            SessionId = sessionId,
            ClientId = clientId,
            Discrepancies = allDiscrepancies,
            GroupedDiscrepancies = groups,
            Summary = summary,
            ModuleImpacts = impacts,
            AffectedModules = impacts.Keys.ToList(),
            ExecutionTimeMs = executionTime,
            Success = true,
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.Information(
            "Analysis complete: {Total} discrepancies, {Critical} critical, {Blocked} blocking in {Ms}ms",
            allDiscrepancies.Count,
            summary.CriticalCount,
            summary.BlockerCount,
            executionTime);

        return await Task.FromResult(result);
    }

    public DetailedDiscrepancy CreateDataDiscrepancy(
        string module,
        string table,
        string category,
        string description,
        DiscrepancySeverity severity,
        int affectedRecordCount,
        double impactPercentage)
    {
        var isBlocker = severity == DiscrepancySeverity.Critical && impactPercentage > 5;

        return new DetailedDiscrepancy
        {
            Module = module,
            Table = table,
            Category = category,
            Description = description,
            Severity = severity,
            AffectedRecordCount = affectedRecordCount,
            ImpactPercentage = impactPercentage,
            IsBlocker = isBlocker,
            IsResolvable = true,
            DetectedAt = DateTime.UtcNow,
            RootCause = "Data comparison revealed differences",
            Recommendations = GenerateRecommendations(new DetailedDiscrepancy
            {
                Category = category,
                Severity = severity,
                AffectedRecordCount = affectedRecordCount
            }).Select(r => r.Title).ToList()
        };
    }

    public DetailedDiscrepancy CreateSchemaDiscrepancy(
        string module,
        string table,
        string? column,
        string description,
        DiscrepancySeverity severity,
        bool isBlocker = false)
    {
        return new DetailedDiscrepancy
        {
            Module = module,
            Table = table,
            Column = column,
            Category = "SchemaStructure",
            Description = description,
            Severity = severity,
            IsBlocker = isBlocker,
            IsResolvable = severity != DiscrepancySeverity.Critical,
            DetectedAt = DateTime.UtcNow,
            RootCause = "Schema analysis revealed structural differences"
        };
    }

    public List<DiscrepancyGroup> GroupDiscrepancies(List<DetailedDiscrepancy> discrepancies)
    {
        var groups = new List<DiscrepancyGroup>();
        var grouped = discrepancies.GroupBy(d => $"{d.Module}:{d.Table}");

        foreach (var group in grouped)
        {
            var discrepancyList = group.ToList();
            var maxSeverity = discrepancyList.Max(d => d.Severity);
            var moduleGroups = discrepancyList.GroupBy(d => d.Category);

            foreach (var moduleGroup in moduleGroups)
            {
                var categoryDiscrepancies = moduleGroup.ToList();
                groups.Add(new DiscrepancyGroup
                {
                    Title = $"{group.Key} - {moduleGroup.Key}",
                    Description = $"{categoryDiscrepancies.Count} {moduleGroup.Key} discrepancies",
                    Discrepancies = categoryDiscrepancies,
                    MaxSeverity = categoryDiscrepancies.Max(d => d.Severity),
                    Module = group.Key.Split(':')[0],
                    CombinedImpact = categoryDiscrepancies.Sum(d => d.ImpactPercentage) / categoryDiscrepancies.Count
                });
            }
        }

        return groups;
    }

    public List<DetailedDiscrepancy> FindRelatedDiscrepancies(
        DetailedDiscrepancy discrepancy,
        List<DetailedDiscrepancy> allDiscrepancies)
    {
        return allDiscrepancies
            .Where(d => d.Id != discrepancy.Id &&
                   d.Table == discrepancy.Table &&
                   d.Category == discrepancy.Category &&
                   d.Severity >= DiscrepancySeverity.High)
            .ToList();
    }

    public DiscrepancySeverity DetermineSeverity(
        double impactPercentage,
        int affectedRecordCount)
    {
        if (impactPercentage > 20 || affectedRecordCount > 1000)
            return DiscrepancySeverity.Critical;

        if (impactPercentage > 10 || affectedRecordCount > 100)
            return DiscrepancySeverity.High;

        if (impactPercentage > 5 || affectedRecordCount > 10)
            return DiscrepancySeverity.Medium;

        return DiscrepancySeverity.Low;
    }

    public double CalculateSeverityScore(DetailedDiscrepancy discrepancy)
    {
        var severityBase = (int)discrepancy.Severity * 25;
        var impactScore = discrepancy.ImpactPercentage * 2;
        var blockScore = discrepancy.IsBlocker ? 100 : 0;

        return (severityBase + impactScore + blockScore) / 150 * 100;
    }

    public Dictionary<string, ImpactAssessment> AssessModuleImpacts(
        List<DetailedDiscrepancy> discrepancies)
    {
        var impacts = new Dictionary<string, ImpactAssessment>();

        var byModule = discrepancies.GroupBy(d => d.Module);

        foreach (var moduleGroup in byModule)
        {
            var module = moduleGroup.Key;
            var discrepancyList = moduleGroup.ToList();
            var maxSeverity = discrepancyList.Max(d => d.Severity);
            var blockers = discrepancyList.Count(d => d.IsBlocker);
            var overallImpact = discrepancyList.Average(d => d.ImpactPercentage);

            var riskLevel = maxSeverity switch
            {
                DiscrepancySeverity.Critical => "Critical",
                DiscrepancySeverity.High => "High",
                DiscrepancySeverity.Medium => "Medium",
                _ => "Low"
            };

            impacts[module] = new ImpactAssessment
            {
                ModuleName = module,
                DiscrepancyCount = discrepancyList.Count,
                ImpactPercentage = overallImpact,
                MaxSeverity = maxSeverity,
                IsBlocking = blockers > 0,
                RiskLevel = riskLevel,
                AffectedFeatures = discrepancyList
                    .Select(d => $"{d.Table}{(d.Column != null ? "." + d.Column : "")}")
                    .Distinct()
                    .ToList(),
                RequiredFixes = discrepancyList
                    .Where(d => d.IsResolvable)
                    .Select(d => d.Description)
                    .ToList()
            };
        }

        return impacts;
    }

    public double CalculateOverallImpact(List<DetailedDiscrepancy> discrepancies)
    {
        if (discrepancies.Count == 0)
            return 0;

        var critical = discrepancies.Count(d => d.Severity == DiscrepancySeverity.Critical);
        var high = discrepancies.Count(d => d.Severity == DiscrepancySeverity.High);
        var avgImpact = discrepancies.Average(d => d.ImpactPercentage);

        return (critical * 5 + high * 2) / discrepancies.Count + avgImpact;
    }

    public List<DetailedDiscrepancy> FindBlockingIssues(
        List<DetailedDiscrepancy> discrepancies)
    {
        return discrepancies
            .Where(d => d.IsBlocker)
            .OrderByDescending(d => CalculateSeverityScore(d))
            .ToList();
    }

    public DiscrepancySummary GenerateSummary(List<DetailedDiscrepancy> discrepancies)
    {
        var summary = new DiscrepancySummary
        {
            TotalDiscrepancies = discrepancies.Count,
            CriticalCount = discrepancies.Count(d => d.Severity == DiscrepancySeverity.Critical),
            HighCount = discrepancies.Count(d => d.Severity == DiscrepancySeverity.High),
            MediumCount = discrepancies.Count(d => d.Severity == DiscrepancySeverity.Medium),
            LowCount = discrepancies.Count(d => d.Severity == DiscrepancySeverity.Low),
            BlockerCount = discrepancies.Count(d => d.IsBlocker),
            ResolvableCount = discrepancies.Count(d => d.IsResolvable),
            CriticalFindings = discrepancies
                .Where(d => d.Severity == DiscrepancySeverity.Critical)
                .Select(d => d.Description)
                .ToList(),
            OverallImpactPercentage = CalculateOverallImpact(discrepancies),
            DiscrepanciesByModule = discrepancies
                .GroupBy(d => d.Module)
                .ToDictionary(g => g.Key, g => g.Count()),
            DiscrepanciesByCategory = discrepancies
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return summary;
    }

    public DiscrepancyMetrics GenerateMetrics(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary)
    {
        var severityScores = discrepancies.Select(d => (int)d.Severity).ToList();
        var avgSeverity = severityScores.Count > 0 ? severityScores.Average() : 0;

        return new DiscrepancyMetrics
        {
            TotalDiscrepancies = summary.TotalDiscrepancies,
            SeverityDistribution_Critical = summary.CriticalCount,
            SeverityDistribution_High = summary.HighCount,
            SeverityDistribution_Medium = summary.MediumCount,
            SeverityDistribution_Low = summary.LowCount,
            AverageSeverityScore = avgSeverity,
            TotalImpactPercentage = summary.OverallImpactPercentage,
            BlockingIssueCount = summary.BlockerCount,
            ResolvableCount = summary.ResolvableCount,
            IssuesByModule = summary.DiscrepanciesByModule,
            IssuesByCategory = summary.DiscrepanciesByCategory
        };
    }

    public string IdentifyRootCause(
        DetailedDiscrepancy discrepancy,
        SchemaAnalysisResult? schemaAnalysis = null)
    {
        // Pattern-based root cause identification
        if (_config.EnablePatternMatching)
        {
            var matchingPatterns = _patterns
                .Where(p => p.Category.ToString() == discrepancy.Category)
                .ToList();

            if (matchingPatterns.Any())
            {
                var pattern = matchingPatterns.First();
                return pattern.CommonRootCauses.FirstOrDefault() ?? "Pattern-based cause identified";
            }
        }

        // Default cause based on category
        return discrepancy.Category switch
        {
            "DataValue" => "Data transformation logic differs between systems",
            "DataType" => "Type conversion or mapping issue during migration",
            "SchemaStructure" => "Schema was not properly mapped during migration",
            "DataIntegrity" => "Foreign key or constraint mapping is incomplete",
            "Performance" => "Performance optimization differs between implementations",
            "Completeness" => "Data not migrated to new system",
            "Consistency" => "Data consistency logic differs between systems",
            "BusinessLogic" => "Business logic implementation differs",
            _ => "Root cause requires manual investigation"
        };
    }

    public List<RemediationRecommendation> GenerateRecommendations(
        DetailedDiscrepancy discrepancy)
    {
        var recommendations = new List<RemediationRecommendation>();

        if (discrepancy.Category == "DataValue")
        {
            recommendations.Add(new RemediationRecommendation
            {
                Title = "Review data transformation logic",
                Description = "Compare transformation rules between legacy and Blazor implementations",
                Steps = new List<string>
                {
                    "Identify transformation logic in legacy system",
                    "Verify implementation in Blazor system",
                    "Test with sample data",
                    "Update Blazor logic if needed"
                },
                EstimatedEffortHours = 4,
                SuccessProbability = 0.85
            });
        }

        if (discrepancy.Category == "SchemaStructure")
        {
            recommendations.Add(new RemediationRecommendation
            {
                Title = "Update schema mapping",
                Description = "Adjust column or table mappings to match discovered differences",
                Steps = new List<string>
                {
                    "Document current mapping",
                    "Identify correct mapping from business requirements",
                    "Update migration scripts",
                    "Re-migrate affected data"
                },
                EstimatedEffortHours = 6,
                IsAutomatic = false,
                SuccessProbability = 0.90
            });
        }

        if (discrepancy.Severity == DiscrepancySeverity.Critical)
        {
            recommendations.Add(new RemediationRecommendation
            {
                Title = "Escalate to migration team",
                Description = "This critical issue requires immediate expert review",
                RequiredRole = "Migration Lead",
                EstimatedEffortHours = 2,
                SuccessProbability = 1.0
            });
        }

        return recommendations;
    }

    public List<DiscrepancyPattern> AnalyzePatterns(
        List<DetailedDiscrepancy> discrepancies)
    {
        var patterns = new List<DiscrepancyPattern>();
        var byCategory = discrepancies.GroupBy(d => d.Category);

        foreach (var categoryGroup in byCategory)
        {
            var categoryDiscrepancies = categoryGroup.ToList();
            if (categoryDiscrepancies.Count >= 2)
            {
                var commonTables = categoryDiscrepancies
                    .GroupBy(d => d.Table)
                    .Where(g => g.Count() >= 2)
                    .Select(g => g.Key)
                    .ToList();

                patterns.Add(new DiscrepancyPattern
                {
                    Name = $"{categoryGroup.Key} Pattern",
                    Description = $"Multiple {categoryGroup.Key} issues detected",
                    Indicators = commonTables,
                    Category = (DiscrepancyCategory)Enum.Parse(typeof(DiscrepancyCategory), categoryGroup.Key, true),
                    DefaultSeverity = categoryDiscrepancies.Max(d => d.Severity),
                    CommonRootCauses = new List<string>
                    {
                        IdentifyRootCause(categoryDiscrepancies[0])
                    },
                    SuggestedFixes = GenerateRecommendations(categoryDiscrepancies[0])
                        .Select(r => r.Title)
                        .ToList()
                });
            }
        }

        return patterns;
    }

    public List<string> CollectEvidence(
        DetailedDiscrepancy discrepancy,
        TableComparisonResult? comparisonResult = null)
    {
        var evidence = new List<string>(discrepancy.Evidence ?? new());

        if (comparisonResult != null)
        {
            evidence.Add($"Table: {comparisonResult.TableName}");
            evidence.Add($"Match percentage: {comparisonResult.MatchPercentage:F1}%");
            evidence.Add($"Affected records: {comparisonResult.ModifiedRecords}");
        }

        if (discrepancy.AffectedRecordCount > 0)
        {
            evidence.Add($"Affected records: {discrepancy.AffectedRecordCount}");
        }

        if (discrepancy.ImpactPercentage > 0)
        {
            evidence.Add($"Impact: {discrepancy.ImpactPercentage:F1}%");
        }

        return evidence.Distinct().ToList();
    }

    public void Configure(DiscrepancyDetectionConfig config)
    {
        _config = config;
        _logger.Information("Discrepancy detector configured");
    }

    public void EnablePatternMatching(List<DiscrepancyPattern> patterns)
    {
        _patterns = patterns;
        _config.EnablePatternMatching = true;
        _logger.Information("Pattern matching enabled with {Count} patterns", patterns.Count);
    }

    public void AddCustomPattern(DiscrepancyPattern pattern)
    {
        _patterns.Add(pattern);
        _config.CustomPatterns.Add(pattern);
        _logger.Information("Custom pattern added: {Name}", pattern.Name);
    }

    private void InitializeDefaultPatterns()
    {
        _patterns = new List<DiscrepancyPattern>
        {
            new DiscrepancyPattern
            {
                Name = "Data Type Mismatch",
                Description = "Data type conversions causing mismatches",
                Category = DiscrepancyCategory.DataType,
                DefaultSeverity = DiscrepancySeverity.High,
                CommonRootCauses = new List<string>
                {
                    "Type mapping error during schema definition",
                    "Precision loss during conversion",
                    "Enum to string conversion issue"
                },
                SuggestedFixes = new List<string>
                {
                    "Review type mappings in migration scripts",
                    "Verify EF Core type configurations",
                    "Test with edge case values"
                }
            },
            new DiscrepancyPattern
            {
                Name = "Missing Column Data",
                Description = "Columns migrated but without proper data transformation",
                Category = DiscrepancyCategory.Completeness,
                DefaultSeverity = DiscrepancySeverity.High,
                CommonRootCauses = new List<string>
                {
                    "Transformation logic not implemented",
                    "Column mapping missing",
                    "Conditional logic not ported"
                },
                SuggestedFixes = new List<string>
                {
                    "Implement data transformation in migration script",
                    "Verify column mapping configuration",
                    "Add missing conditional logic"
                }
            }
        };
    }
}
