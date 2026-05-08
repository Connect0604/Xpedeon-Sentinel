namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;
using Serilog;

/// <summary>
/// Calculates migration risk and provides Go/No-Go recommendations
/// Synthesizes discrepancies into actionable risk assessment and mitigation strategies
/// </summary>
public class RiskCalculator : IRiskCalculator
{
    private readonly ILogger _logger = Log.ForContext<RiskCalculator>();
    private RiskCalculationConfig _config = new();
    private RiskScoringWeights _weights = new();

    public RiskCalculator()
    {
    }

    public async Task<MigrationRiskAssessment> AssessMigrationRiskAsync(
        DiscrepancyAnalysisResult discrepancies,
        string sessionId,
        string clientId)
    {
        _logger.Information("Assessing migration risk for session {Session}", sessionId);

        var startTime = DateTime.UtcNow;

        // Calculate overall scores
        var overallRiskScore = CalculateOverallRiskScore(
            discrepancies.Discrepancies,
            discrepancies.Summary);
        var overallHealthScore = CalculateHealthScore(
            discrepancies.Discrepancies,
            discrepancies.Summary);

        // Assess each module
        var moduleRisks = new List<ModuleRiskAssessment>();
        foreach (var moduleName in discrepancies.AffectedModules)
        {
            var moduleDiscrepancies = discrepancies.Discrepancies
                .Where(d => d.Module == moduleName)
                .ToList();
            var moduleImpact = discrepancies.ModuleImpacts.ContainsKey(moduleName)
                ? discrepancies.ModuleImpacts[moduleName]
                : null;

            var moduleRisk = await AssessModuleRiskAsync(moduleName, moduleDiscrepancies, moduleImpact);
            moduleRisks.Add(moduleRisk);
        }

        // Extract and prioritize critical items
        var criticalItems = ExtractCriticalItems(discrepancies.Discrepancies);
        criticalItems = PrioritizeCriticalItems(criticalItems);

        // Analyze dependencies
        if (_config.EnableDependencyAnalysis)
        {
            AnalyzeDependencies(criticalItems);
        }

        // Determine readiness
        var readiness = DetermineMigrationReadiness(overallRiskScore);

        // Generate recommendations
        var recommendation = MakeRecommendation(new MigrationRiskAssessment
        {
            OverallRiskScore = overallRiskScore,
            OverallHealthScore = overallHealthScore,
            ModuleRisks = moduleRisks,
            CriticalItems = criticalItems
        });

        // Generate mitigation strategies
        var mitigationStrategies = _config.EnableMitigationPlanning
            ? GenerateMitigationStrategies(criticalItems, new())
            : new();

        // Analyze trends if enabled
        var trendAnalysis = _config.EnableTrendAnalysis
            ? new RiskTrendAnalysis
            {
                Snapshots = new List<RiskSnapshot>
                {
                    new RiskSnapshot
                    {
                        Timestamp = DateTime.UtcNow,
                        OverallRiskScore = overallRiskScore,
                        OverallHealthScore = overallHealthScore,
                        TotalCriticalItems = criticalItems.Count(c => c.Priority == RiskPriority.Critical),
                        TotalIssues = discrepancies.Discrepancies.Count,
                        Readiness = readiness
                    }
                },
                Trend = "Stable"
            }
            : new();

        // Identify blockers and warnings
        var assessment = new MigrationRiskAssessment
        {
            SessionId = sessionId,
            ClientId = clientId,
            OverallRiskScore = overallRiskScore,
            OverallHealthScore = overallHealthScore,
            OverallReadiness = readiness,
            Recommendation = recommendation,
            ModuleRisks = moduleRisks,
            CriticalItems = criticalItems,
            MitigationStrategies = mitigationStrategies,
            TrendAnalysis = trendAnalysis,
            Success = true,
            CalculationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
        };

        assessment.GoBlockers = IdentifyGoBlockers(assessment);
        assessment.Warnings = IdentifyWarnings(assessment);

        _logger.Information(
            "Risk assessment complete: Risk={Risk:F1}, Health={Health:F1}, Readiness={Readiness}, Blockers={Blockers}",
            overallRiskScore,
            overallHealthScore,
            readiness,
            assessment.GoBlockers.Count);

        return await Task.FromResult(assessment);
    }

    public async Task<ModuleRiskAssessment> AssessModuleRiskAsync(
        string moduleName,
        List<DetailedDiscrepancy> moduleDiscrepancies,
        ImpactAssessment? moduleImpact = null)
    {
        var riskScore = CalculateModuleRiskScore(moduleDiscrepancies);
        var healthScore = 100 - riskScore;

        var criticalItems = moduleDiscrepancies.Count(d => d.Severity == DiscrepancySeverity.Critical);
        var highItems = moduleDiscrepancies.Count(d => d.Severity == DiscrepancySeverity.High);
        var blockingItems = moduleDiscrepancies.Count(d => d.IsBlocker);
        var resolvableItems = moduleDiscrepancies.Count(d => d.IsResolvable);

        var readiness = DetermineMigrationReadiness(riskScore);

        var riskLevel = riskScore switch
        {
            < 25 => "Low",
            < 50 => "Medium",
            < 75 => "High",
            _ => "Critical"
        };

        var dataCompleteness = moduleImpact?.ImpactPercentage ?? 100 - (moduleDiscrepancies.Average(d => d.ImpactPercentage) * 0.5);
        var dataAccuracy = 100 - moduleDiscrepancies
            .Where(d => d.Category == "DataValue")
            .Average(d => d.ImpactPercentage);
        var functionalCoverage = 100 - moduleDiscrepancies
            .Where(d => d.Category == "BusinessLogic")
            .Average(d => d.ImpactPercentage);

        var estimatedFixHours = moduleDiscrepancies
            .Sum(d => (int)(d.AffectedRecordCount / 100.0 * (1 + (int)d.Severity)));

        var assessment = new ModuleRiskAssessment
        {
            ModuleName = moduleName,
            RiskScore = riskScore,
            HealthScore = healthScore,
            DiscrepancyCount = moduleDiscrepancies.Count,
            BlockingIssueCount = blockingItems,
            ResolvableIssueCount = resolvableItems,
            Readiness = readiness,
            RiskLevel = riskLevel,
            DataCompleteness = dataCompleteness,
            DataAccuracy = double.IsNaN(dataAccuracy) ? 100 : dataAccuracy,
            FunctionalCoverage = double.IsNaN(functionalCoverage) ? 100 : functionalCoverage,
            EstimatedFixHours = estimatedFixHours,
            EstimatedReadyDate = DateTime.UtcNow.AddDays(estimatedFixHours * _config.EstimatedDaysPerFixHour)
        };

        return await Task.FromResult(assessment);
    }

    public double CalculateOverallRiskScore(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary)
    {
        if (discrepancies.Count == 0)
            return 0;

        var criticalScore = summary.CriticalCount * _weights.CriticalDiscrepancyWeight;
        var highScore = summary.HighCount * _weights.HighDiscrepancyWeight;
        var mediumScore = summary.MediumCount * _weights.MediumDiscrepancyWeight;
        var lowScore = summary.LowCount * _weights.LowDiscrepancyWeight;
        var blockerScore = summary.BlockerCount * _weights.BlockingIssueWeight;

        var totalScore = criticalScore + highScore + mediumScore + lowScore + blockerScore;
        var avgImpact = discrepancies.Average(d => d.ImpactPercentage);

        // Normalize to 0-100
        var normalizedScore = Math.Min(totalScore / (discrepancies.Count * 0.5) * avgImpact, 100);

        return Math.Max(normalizedScore, 0);
    }

    public double CalculateHealthScore(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary)
    {
        if (discrepancies.Count == 0)
            return 100;

        var riskScore = CalculateOverallRiskScore(discrepancies, summary);
        return 100 - riskScore;
    }

    public double CalculateModuleRiskScore(
        List<DetailedDiscrepancy> moduleDiscrepancies)
    {
        if (moduleDiscrepancies.Count == 0)
            return 0;

        var criticality = moduleDiscrepancies
            .Sum(d => (int)d.Severity * d.ImpactPercentage / 100);

        var avgImpact = moduleDiscrepancies.Average(d => d.ImpactPercentage);
        var blockingCount = moduleDiscrepancies.Count(d => d.IsBlocker);

        var score = (criticality + avgImpact + blockingCount * 10) / 3;

        return Math.Min(score, 100);
    }

    public List<RiskComponent> CalculateRiskComponents(
        List<DetailedDiscrepancy> discrepancies)
    {
        var components = new List<RiskComponent>();

        // Data Risk
        var dataDiscrepancies = discrepancies
            .Where(d => d.Category is "DataValue" or "DataType" or "DataIntegrity")
            .ToList();
        if (dataDiscrepancies.Any())
        {
            components.Add(new RiskComponent
            {
                Name = "Data Risk",
                Category = "Data",
                Score = CalculateModuleRiskScore(dataDiscrepancies),
                Weight = _config.DataCompletenessWeight + _config.DataAccuracyWeight,
                ContributingFactors = dataDiscrepancies.Select(d => d.Description).Take(3).ToList(),
                Recommendation = "Review data transformation and validation logic"
            });
        }

        // Schema Risk
        var schemaDiscrepancies = discrepancies
            .Where(d => d.Category == "SchemaStructure")
            .ToList();
        if (schemaDiscrepancies.Any())
        {
            components.Add(new RiskComponent
            {
                Name = "Schema Risk",
                Category = "Schema",
                Score = CalculateModuleRiskScore(schemaDiscrepancies),
                Weight = _config.SchemaCompatibilityWeight,
                ContributingFactors = schemaDiscrepancies.Select(d => d.Description).Take(3).ToList(),
                Recommendation = "Verify schema mappings and table structures"
            });
        }

        // Logic Risk
        var logicDiscrepancies = discrepancies
            .Where(d => d.Category is "BusinessLogic" or "Consistency")
            .ToList();
        if (logicDiscrepancies.Any())
        {
            components.Add(new RiskComponent
            {
                Name = "Logic Risk",
                Category = "Logic",
                Score = CalculateModuleRiskScore(logicDiscrepancies),
                Weight = _config.FunctionalCoverageWeight,
                ContributingFactors = logicDiscrepancies.Select(d => d.Description).Take(3).ToList(),
                Recommendation = "Review business logic implementation and validation rules"
            });
        }

        // Performance Risk
        var performanceDiscrepancies = discrepancies
            .Where(d => d.Category == "Performance")
            .ToList();
        if (performanceDiscrepancies.Any())
        {
            components.Add(new RiskComponent
            {
                Name = "Performance Risk",
                Category = "Performance",
                Score = CalculateModuleRiskScore(performanceDiscrepancies),
                Weight = 0.1,
                ContributingFactors = performanceDiscrepancies.Select(d => d.Description).Take(3).ToList(),
                Recommendation = "Optimize queries and indexes for production workload"
            });
        }

        return components;
    }

    public MigrationRecommendation MakeRecommendation(
        MigrationRiskAssessment assessment)
    {
        var blockers = assessment.GoBlockers?.Count ?? 0;
        var criticalItems = assessment.CriticalItems?.Count(i => i.Priority == RiskPriority.Critical) ?? 0;

        MigrationDecision decision;
        string rationale;
        List<string> requiredActions = new();

        if (blockers > 0 || assessment.OverallRiskScore > _config.HighThreshold)
        {
            decision = MigrationDecision.NoGo;
            rationale = $"Critical blockers identified: {blockers} blocking issues. Risk score: {assessment.OverallRiskScore:F1}";
            requiredActions = assessment.GoBlockers ?? new();
        }
        else if (assessment.OverallRiskScore > _config.CriticalThreshold)
        {
            decision = MigrationDecision.Delay;
            rationale = $"High risk score ({assessment.OverallRiskScore:F1}) requires risk mitigation before launch";
            requiredActions = assessment.CriticalItems?
                .Where(i => i.Priority == RiskPriority.Critical)
                .Select(i => $"Fix: {i.Title}")
                .ToList() ?? new();
        }
        else if (assessment.OverallRiskScore > 0 && assessment.CriticalItems?.Any(i => !i.CanBeFixedPostLaunch) == true)
        {
            decision = MigrationDecision.GoWithRisks;
            rationale = $"Acceptable risk level ({assessment.OverallRiskScore:F1}) with mitigation. Some post-launch work required.";
            requiredActions = assessment.CriticalItems?
                .Where(i => !i.CanBeFixedPostLaunch)
                .Select(i => $"Must fix pre-launch: {i.Title}")
                .ToList() ?? new();
        }
        else
        {
            decision = MigrationDecision.Go;
            rationale = $"Risk score acceptable ({assessment.OverallRiskScore:F1}). System ready for migration.";
            requiredActions = new();
        }

        var estimatedDaysToReady = EstimateDaysToReadiness(assessment.CriticalItems ?? new());
        var confidence = CalculateConfidenceLevel(assessment);

        return new MigrationRecommendation
        {
            Decision = decision,
            Summary = $"{decision}: Migration can proceed with {(decision == MigrationDecision.Go ? "confidence" : "caution")}",
            Rationale = rationale,
            RequiredActions = requiredActions,
            RecommendedActions = assessment.CriticalItems?
                .Where(i => i.CanBeFixedPostLaunch && i.Priority == RiskPriority.High)
                .Select(i => $"Recommend fixing post-launch: {i.Title}")
                .ToList() ?? new(),
            CanBePostLaunch = assessment.CriticalItems?
                .Where(i => i.CanBeFixedPostLaunch)
                .Select(i => i.Title)
                .ToList() ?? new(),
            MinimumDaysToReady = estimatedDaysToReady,
            ConfidenceLevel = confidence
        };
    }

    public MigrationReadiness DetermineMigrationReadiness(double riskScore)
    {
        return riskScore switch
        {
            < _config.CriticalThreshold => MigrationReadiness.Ready,
            < _config.HighThreshold => MigrationReadiness.AlmostReady,
            < _config.MediumThreshold => MigrationReadiness.PartiallyReady,
            _ => MigrationReadiness.NotReady
        };
    }

    public List<string> IdentifyGoBlockers(MigrationRiskAssessment assessment)
    {
        var blockers = new List<string>();

        // Blocking issues
        var blockingIssues = assessment.CriticalItems?
            .Where(i => i.Priority == RiskPriority.Critical && !i.CanBeFixedPostLaunch)
            .ToList() ?? new();

        foreach (var issue in blockingIssues)
        {
            blockers.Add($"BLOCKER: {issue.Title} ({issue.Module}) - {issue.AffectedRecordCount} records affected");
        }

        // High-risk modules
        var criticalModules = assessment.ModuleRisks?
            .Where(m => m.RiskLevel == "Critical")
            .ToList() ?? new();

        foreach (var module in criticalModules)
        {
            if (module.BlockingIssueCount > 0)
            {
                blockers.Add($"BLOCKER: Module '{module.ModuleName}' has {module.BlockingIssueCount} blocking issues");
            }
        }

        // Data completeness
        if (assessment.CriticalItems?.Average(i => i.ImpactPercentage) > 10)
        {
            blockers.Add("BLOCKER: Data impact too high - verify completeness and accuracy before launch");
        }

        return blockers.Distinct().ToList();
    }

    public List<string> IdentifyWarnings(MigrationRiskAssessment assessment)
    {
        var warnings = new List<string>();

        // High-risk items
        var highRiskItems = assessment.CriticalItems?
            .Where(i => i.Priority == RiskPriority.High)
            .ToList() ?? new();

        if (highRiskItems.Count > 0)
        {
            warnings.Add($"WARNING: {highRiskItems.Count} high-risk items require attention");
        }

        // Modules at risk
        var riskModules = assessment.ModuleRisks?
            .Where(m => m.RiskLevel is "High" or "Critical")
            .ToList() ?? new();

        foreach (var module in riskModules)
        {
            warnings.Add($"WARNING: Module '{module.ModuleName}' has risk score {module.RiskScore:F1}");
        }

        // Data accuracy concerns
        var avgAccuracy = assessment.ModuleRisks?.Average(m => m.DataAccuracy) ?? 100;
        if (avgAccuracy < _config.MinimumDataMatch)
        {
            warnings.Add($"WARNING: Data accuracy ({avgAccuracy:F1}%) below acceptable threshold ({_config.MinimumDataMatch}%)");
        }

        return warnings;
    }

    public List<CriticalRiskItem> ExtractCriticalItems(
        List<DetailedDiscrepancy> discrepancies)
    {
        var items = new List<CriticalRiskItem>();

        foreach (var discrepancy in discrepancies.Where(d => d.Severity >= DiscrepancySeverity.High || d.IsBlocker))
        {
            items.Add(new CriticalRiskItem
            {
                Title = discrepancy.Description,
                Description = $"{discrepancy.Category}: {discrepancy.Description}",
                Module = discrepancy.Module,
                Table = discrepancy.Table,
                AffectedRecordCount = discrepancy.AffectedRecordCount,
                ImpactPercentage = discrepancy.ImpactPercentage,
                CanBeFixedPostLaunch = discrepancy.IsResolvable && discrepancy.Severity < DiscrepancySeverity.Critical,
                ResolutionPath = discrepancy.ResolutionPath,
                Priority = discrepancy.IsBlocker ? RiskPriority.Critical :
                          discrepancy.Severity == DiscrepancySeverity.Critical ? RiskPriority.Critical :
                          discrepancy.Severity == DiscrepancySeverity.High ? RiskPriority.High :
                          RiskPriority.Medium,
                EstimatedFixHours = (int)(discrepancy.AffectedRecordCount / 100.0 * (1 + (int)discrepancy.Severity))
            });
        }

        return items;
    }

    public List<CriticalRiskItem> PrioritizeCriticalItems(
        List<CriticalRiskItem> items)
    {
        return items
            .OrderByDescending(i => i.Priority)
            .ThenByDescending(i => i.ImpactPercentage)
            .ThenByDescending(i => i.AffectedRecordCount)
            .ToList();
    }

    public void AnalyzeDependencies(List<CriticalRiskItem> items)
    {
        // Simple dependency analysis: items in same table/module are related
        for (int i = 0; i < items.Count; i++)
        {
            for (int j = 0; j < items.Count; j++)
            {
                if (i != j && items[i].Table == items[j].Table)
                {
                    if (!items[i].DependentOn.Contains(items[j].Id))
                    {
                        items[i].DependentOn.Add(items[j].Id);
                    }
                }
            }
        }
    }

    public List<RiskMitigationStrategy> GenerateMitigationStrategies(
        List<CriticalRiskItem> criticalItems,
        List<RemediationRecommendation> recommendations)
    {
        var strategies = new List<RiskMitigationStrategy>();

        foreach (var item in criticalItems.Where(i => i.Priority == RiskPriority.Critical))
        {
            strategies.Add(new RiskMitigationStrategy
            {
                Title = $"Mitigate: {item.Title}",
                Description = item.Description,
                TargetsRiskItems = new List<string> { item.Id },
                Actions = new List<string>
                {
                    "Identify root cause",
                    "Implement fix",
                    "Validate fix",
                    "Test with production-like data"
                },
                EstimatedHours = item.EstimatedFixHours,
                Owner = "Migration Lead",
                TargetCompletionDate = DateTime.UtcNow.AddDays(item.EstimatedFixHours * _config.EstimatedDaysPerFixHour),
                RiskReduction = Math.Min(item.ImpactPercentage, 100),
                Priority = MitigationPriority.Immediate,
                IsPreLaunch = !item.CanBeFixedPostLaunch
            });
        }

        return strategies;
    }

    public int EstimateDaysToReadiness(
        List<CriticalRiskItem> criticalItems)
    {
        if (criticalItems.Count == 0)
            return 0;

        var prelaunchHours = criticalItems
            .Where(i => !i.CanBeFixedPostLaunch)
            .Sum(i => i.EstimatedFixHours);

        return (int)Math.Ceiling(prelaunchHours * _config.EstimatedDaysPerFixHour);
    }

    public LaunchReadinessChecklist CreateReadinessChecklist(
        MigrationRiskAssessment assessment)
    {
        var checklist = new LaunchReadinessChecklist();

        var allIssuesResolved = assessment.CriticalItems?.All(i => i.CanBeFixedPostLaunch) ?? true;
        var dataCompleteAndAccurate = assessment.ModuleRisks?.All(m => m.DataCompleteness > 95 && m.DataAccuracy > 95) ?? true;
        var schemaCompatible = assessment.ModuleRisks?.All(m => m.RiskScore < 50) ?? false;

        checklist.AllDataMigrated = dataCompleteAndAccurate;
        checklist.AllDataValidated = dataCompleteAndAccurate;
        checklist.AllSchemaMapped = schemaCompatible;
        checklist.AllFunctionalityTested = assessment.ModuleRisks?.All(m => m.FunctionalCoverage > 90) ?? false;
        checklist.AllCriticalIssuesFixed = allIssuesResolved;
        checklist.PerformanceAcceptable = assessment.ModuleRisks?.Average(m => m.RiskScore) < 50;
        checklist.UserAcceptanceTested = false; // Manual verification needed
        checklist.DataBackupComplete = false; // Manual verification needed
        checklist.RollbackPlanReady = false; // Manual verification needed
        checklist.SupportTeamTrained = false; // Manual verification needed

        checklist.TotalItems = 10;
        checklist.CompletedItems = new[]
        {
            checklist.AllDataMigrated,
            checklist.AllDataValidated,
            checklist.AllSchemaMapped,
            checklist.AllFunctionalityTested,
            checklist.AllCriticalIssuesFixed,
            checklist.PerformanceAcceptable
        }.Count(x => x);

        return checklist;
    }

    public RiskTrendAnalysis AnalyzeTrends(
        List<MigrationRiskAssessment> historicalAssessments)
    {
        var analysis = new RiskTrendAnalysis();

        if (historicalAssessments.Count < 2)
        {
            analysis.Trend = "Insufficient data";
            return analysis;
        }

        var sorted = historicalAssessments.OrderBy(a => a.AssessedAt).ToList();

        foreach (var assessment in sorted)
        {
            analysis.Snapshots.Add(new RiskSnapshot
            {
                Timestamp = assessment.AssessedAt,
                OverallRiskScore = assessment.OverallRiskScore,
                OverallHealthScore = assessment.OverallHealthScore,
                TotalCriticalItems = assessment.CriticalItems.Count,
                TotalIssues = assessment.CriticalItems.Count,
                Readiness = assessment.OverallReadiness
            });
        }

        // Calculate trend
        var first = analysis.Snapshots.First();
        var last = analysis.Snapshots.Last();
        var riskChange = last.OverallRiskScore - first.OverallRiskScore;
        var daySpan = (last.Timestamp - first.Timestamp).TotalDays;

        if (daySpan > 0)
        {
            analysis.TrendDirection = -riskChange / daySpan; // Negative = improving
            analysis.ImprovementRate = Math.Abs(analysis.TrendDirection);
        }

        analysis.Trend = analysis.TrendDirection switch
        {
            < -0.5 => "Improving",
            > 0.5 => "Deteriorating",
            _ => "Stable"
        };

        analysis.TrendObservations.Add($"Risk score: {first.OverallRiskScore:F1} → {last.OverallRiskScore:F1}");
        analysis.TrendObservations.Add($"Trend: {analysis.Trend} ({analysis.TrendDirection:F2} points/day)");

        return analysis;
    }

    public DateTime? ProjectReadinessDate(RiskTrendAnalysis trends)
    {
        if (trends.Snapshots.Count < 2 || trends.ImprovementRate == 0)
            return null;

        var lastSnapshot = trends.Snapshots.Last();
        var riskToTarget = lastSnapshot.OverallRiskScore - 25; // Target: risk score < 25

        if (riskToTarget <= 0)
            return DateTime.UtcNow; // Already ready

        var daysNeeded = riskToTarget / trends.ImprovementRate;
        return DateTime.UtcNow.AddDays(daysNeeded);
    }

    public RiskMetrics GenerateMetrics(MigrationRiskAssessment assessment)
    {
        var metrics = new RiskMetrics
        {
            OverallRiskScore = assessment.OverallRiskScore,
            OverallHealthScore = assessment.OverallHealthScore,
            TotalDiscrepancies = assessment.CriticalItems.Sum(i => 1), // Simplified
            CriticalDiscrepancies = assessment.CriticalItems.Count(i => i.Priority == RiskPriority.Critical),
            BlockingIssues = assessment.GoBlockers.Count,
            AverageModuleRiskScore = assessment.ModuleRisks.Any() ? assessment.ModuleRisks.Average(m => m.RiskScore) : 0,
            HighRiskModules = assessment.ModuleRisks.Count(m => m.RiskLevel is "High" or "Critical"),
            CriticalRiskItems = assessment.CriticalItems.Count(i => i.Priority == RiskPriority.Critical),
            DataCompleteness = assessment.ModuleRisks.Any() ? assessment.ModuleRisks.Average(m => m.DataCompleteness) : 100,
            DataAccuracy = assessment.ModuleRisks.Any() ? assessment.ModuleRisks.Average(m => m.DataAccuracy) : 100,
            SchemaCompatibility = 100 - assessment.OverallRiskScore,
            FunctionalCoverage = assessment.ModuleRisks.Any() ? assessment.ModuleRisks.Average(m => m.FunctionalCoverage) : 0,
            EstimatedTotalFixHours = assessment.CriticalItems.Sum(i => i.EstimatedFixHours),
            EstimatedDaysToReady = EstimateDaysToReadiness(assessment.CriticalItems),
            RiskByModule = assessment.ModuleRisks.ToDictionary(m => m.ModuleName, m => (int)m.RiskScore),
            IssuesByCategory = new Dictionary<string, int>()
        };

        return metrics;
    }

    public double CalculateConfidenceLevel(MigrationRiskAssessment assessment)
    {
        var factors = new List<double>();

        // No blockers = high confidence
        if (assessment.GoBlockers.Count == 0)
            factors.Add(1.0);
        else
            factors.Add(Math.Max(0, 1.0 - (assessment.GoBlockers.Count * 0.1)));

        // Low risk score = high confidence
        factors.Add(1.0 - (assessment.OverallRiskScore / 100));

        // Modules ready = high confidence
        var readyModules = assessment.ModuleRisks.Count(m => m.Readiness == MigrationReadiness.Ready);
        var totalModules = assessment.ModuleRisks.Count;
        if (totalModules > 0)
            factors.Add((double)readyModules / totalModules);

        return factors.Average();
    }

    public void Configure(RiskCalculationConfig config)
    {
        _config = config;
        _logger.Information("Risk calculator configured");
    }

    public void SetScoringWeights(RiskScoringWeights weights)
    {
        _weights = weights;
        _logger.Information("Risk scoring weights configured");
    }

    public RiskCalculationConfig GetConfiguration()
    {
        return _config;
    }
}
