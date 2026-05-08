# Form 11: Metrics Calculation Service

## Overview

The **Metrics Service** (`IMetricsService`/`MetricsService`) provides comprehensive aggregation and calculation of validation metrics from all pipeline phases. It transforms raw comparison data into actionable insights through statistical analysis, trend detection, and health assessment.

**Key Features:**
- Schema quality metrics (table/column/datatype compatibility)
- Data integrity metrics (match percentages, loss detection, consistency)
- Discrepancy analysis with severity and category breakdown
- Performance metrics across all phases
- Quality scoring (0-100) with component breakdown
- Risk assessment aggregation from discrepancies
- Migration readiness evaluation with Go/No-Go recommendation
- Comparative analysis between legacy and Blazor systems
- Time-series snapshots for trend analysis
- Health assessment with component status
- Module-level metrics ranking
- Scenario analysis and what-if modeling
- Export to JSON/CSV/Markdown formats

**Responsibilities:**
- Calculate metrics from raw validation data
- Aggregate data across multiple tables and phases
- Track trends through historical snapshots
- Score quality, risk, and readiness
- Generate actionable recommendations
- Broadcast metric updates to subscribers
- Maintain configurable thresholds and weights

## Architecture

### Core Metric Categories

**Schema Metrics** - Database structure validation
- Total/matched table count with percentage
- Total/matched column count with percentage
- Data type compatibility percentage
- Constraint compliance (primary keys, foreign keys, indexes)
- Overall schema completeness (0-100)

**Data Metrics** - Row-by-row comparison analysis
- Row counts (legacy, Blazor, matched, mismatched, unmatched)
- Data match percentage and data loss percentage
- Column-level mismatch percentage
- Value-level mismatch percentage with null/type issues
- Data integrity score (0-100)

**Discrepancy Metrics** - Issue aggregation and categorization
- Total discrepancy count with severity breakdown (critical/high/medium/low)
- Category distribution (9 categories: DataType, DataLoss, NullHandling, ReferentialIntegrity, CalculationLogic, FormatConversion, BusinessRuleViolation, PerformanceDegradation, Other)
- Impact metrics (affected tables, columns, estimated affected users)
- Resolution tracking (resolved/unresolved/percentage)

**Performance Metrics** - Execution speed and throughput
- Phase-by-phase duration (Discovery, TestGen, Execution, Comparison, Review, Reporting)
- Throughput rates (tables/sec, rows/sec, tests/sec, discrepancies/sec)
- Resource usage (memory average/peak, CPU, thread count)
- Cache hit rate and compression ratio

**Quality Metrics** - Data quality assessment
- Accuracy scores: schema, data, logic, overall (0-100)
- Completeness scores: schema, data, test coverage, validation (0-100)
- Consistency scores: data, referential integrity, constraint compliance (0-100)
- Validation pass rate and test pass rate

**Risk Metrics** - Risk scoring and distribution
- Overall risk score (0-100) and component scores
- Risk distribution by severity (critical/high/medium/low)
- Affected modules, business processes, users
- Mitigation strategy count and coverage percentage
- Assessment confidence level (0-1) with rating

**Readiness Metrics** - Go/No-Go decision support
- Readiness scores: technical, data, user, process, overall (0-100)
- Recommendation: Go, GoWithRisks, Delay, NoGo
- Blocker/warning/info issue counts
- Success probability metrics (first-time success, zero-downtime, no user impact)
- Cutover and rollback timeline estimates
- Dependencies (critical, external, third-party integration count)

### Key Model Classes

See `MetricsModels.cs` for complete definitions including:
- `ValidationMetrics`: Container for all metric categories
- `SchemaMetrics`, `DataMetrics`, `DiscrepancyMetrics`, etc.
- `ModuleMetrics`: Per-module breakdown
- `MetricSnapshot`: Time-series data point
- `ComparativeMetrics`: Legacy vs. Blazor comparison
- `MetricsCalculationConfig`: Configuration options
- `ValidationHealth`: Component health status
- `MetricTrend`: Trend analysis results

### Interface: IMetricsService

#### Initialization
```csharp
Task<ValidationMetrics> InitializeMetricsAsync(string sessionId);
Task<ValidationMetrics> GetMetricsAsync(string sessionId);
```

#### Schema Metrics
```csharp
Task<SchemaMetrics> CalculateSchemaMetricsAsync(string sessionId, SchemaAnalysisResult analysis);
Task UpdateSchemaMetricsAsync(string sessionId, SchemaMetrics metrics);
```

#### Data Metrics
```csharp
Task<DataMetrics> CalculateDataMetricsAsync(string sessionId, TableComparisonResult comparison);
Task<DataMetrics> AggregateDataMetricsAsync(string sessionId, List<TableComparisonResult> comparisons);
```

#### Discrepancy Metrics
```csharp
Task<DiscrepancyMetrics> CalculateDiscrepancyMetricsAsync(string sessionId, DiscrepancyAnalysisResult analysis);
Task<DiscrepancyMetrics> GetDiscrepancyBreakdownAsync(string sessionId);
```

#### Performance Metrics
```csharp
Task RecordPhasePerformanceAsync(string sessionId, string phaseName, long durationMs, int itemsProcessed);
Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(string sessionId);
```

#### Quality Metrics
```csharp
Task<QualityMetrics> CalculateQualityMetricsAsync(string sessionId);
Task<List<ModuleMetrics>> GetModuleQualityAsync(string sessionId);
```

#### Risk Metrics
```csharp
Task<RiskMetrics> CalculateRiskMetricsAsync(string sessionId, MigrationRiskAssessment assessment);
Task UpdateRiskMetricsAsync(string sessionId, RiskMetrics metrics);
```

#### Migration Readiness
```csharp
Task<MigrationReadinessMetrics> CalculateReadinessAsync(string sessionId);
Task<MigrationReadinessMetrics> GetReadinessAsync(string sessionId);
```

#### Comparative Analysis
```csharp
Task<List<ComparativeMetrics>> CalculateComparativeMetricsAsync(string sessionId, List<string> metricNames);
Task<ComparativeMetrics> CompareMetricAsync(string sessionId, string metricName, double legacyValue, double blazorValue);
```

#### Snapshots and Trending
```csharp
Task<MetricSnapshot> RecordSnapshotAsync(string sessionId, string phaseName);
Task<List<MetricSnapshot>> GetSnapshotsAsync(string sessionId, int? maxCount = null);
Task<MetricTrend> AnalyzeTrendAsync(string sessionId, string metricName);
```

#### Health Assessment
```csharp
Task<ValidationHealth> CalculateHealthAsync(string sessionId);
Task<ValidationHealth> GetHealthAsync(string sessionId);
Task<List<HealthIssue>> GetHealthIssuesAsync(string sessionId);
```

#### Module Metrics
```csharp
Task<ModuleMetrics> CalculateModuleMetricsAsync(string sessionId, string moduleName);
Task<List<ModuleMetrics>> GetAllModuleMetricsAsync(string sessionId);
Task<List<ModuleMetrics>> RankModulesByRiskAsync(string sessionId);
```

#### Aggregation
```csharp
Task<ValidationMetrics> AggregateAllMetricsAsync(string sessionId);
Task<ValidationMetrics> RecalculateAllMetricsAsync(string sessionId);
Task<PhaseMetrics> GetPhaseMetricsAsync(string sessionId, string phaseName);
```

#### Analytics
```csharp
Task<ScenarioAnalysis> AnalyzeScenarioAsync(string sessionId, string scenario, Dictionary<string, object> parameters);
Task<List<MetricRecommendation>> GetRecommendationsAsync(string sessionId);
Task<List<ImprovementArea>> IdentifyImprovementAreasAsync(string sessionId);
```

#### Export and Comparison
```csharp
Task<string> ExportMetricsAsync(string sessionId, string format); // json, csv, markdown
Task<MetricsComparison> CompareSessionsAsync(string sessionId1, string sessionId2);
Task ArchiveMetricsAsync(string sessionId, int olderThanDays);
```

#### Configuration and Real-time Updates
```csharp
void Configure(MetricsCalculationConfig config);
MetricsCalculationConfig GetConfiguration();
void SubscribeToMetricsUpdates(string sessionId, Func<ValidationMetrics, Task> callback);
void UnsubscribeFromMetricsUpdates(string sessionId);
Task BroadcastMetricsUpdateAsync(ValidationMetrics metrics);
IAsyncEnumerable<ValidationMetrics> GetMetricsStreamAsync(string sessionId, CancellationToken ct);
```

### Implementation Details

**State Storage:**
- In-memory Dictionary<string, ValidationMetrics> for active sessions
- Dictionary<string, List<MetricSnapshot>> for historical snapshots
- Dictionary<string, Dictionary<string, object>> for phase performance data
- Dictionary<string, List<Func<ValidationMetrics, Task>>> for subscribers
- MetricsCalculationConfig for configuration

**Calculation Algorithms:**

Schema Metrics:
```
TableMatchPercentage = (MatchedTableCount / TotalTableCount) × 100
ColumnMatchPercentage = (MatchedColumnCount / TotalColumnCount) × 100
SchemaCompleteness = (TableMatchPercentage + ColumnMatchPercentage) / 2
```

Data Metrics:
```
DataMatchPercentage = (MatchedRowCount / TotalRowsLegacy) × 100
DataLossPercentage = ((TotalRowsLegacy - TotalRowsBlazor) / TotalRowsLegacy) × 100
ValueMismatchPercentage = (MismatchedValueCount / TotalValueCount) × 100
DataIntegrityScore = 100 - ValueMismatchPercentage
```

Quality Score:
```
OverallAccuracy = (SchemaAccuracy + DataAccuracy + LogicAccuracy) / 3
OverallAccuracy calculated from SchemaMetrics.TableMatchPercentage + DataMetrics.DataMatchPercentage / 2
```

Risk Decision:
```
Risk < 25: Go (suitable for production)
25 ≤ Risk < 50: GoWithRisks (acceptable with mitigation)
50 ≤ Risk < 75: Delay (address issues before migration)
Risk ≥ 75: NoGo (not ready for migration)
```

## Usage Examples

### Basic Initialization and Calculation
```csharp
var service = new MetricsService();

// Initialize metrics for session
var metrics = await service.InitializeMetricsAsync("session-xyz");
Console.WriteLine($"Initialized metrics for {metrics.SessionId}");

// Get current metrics
var current = await service.GetMetricsAsync("session-xyz");
Console.WriteLine($"Overall Quality: {current.QualityMetrics.OverallAccuracy:F1}%");
```

### Schema Analysis
```csharp
// Calculate schema metrics from analysis results
var schemaAnalysis = new SchemaAnalysisResult { /* ... */ };
var schemaMetrics = await service.CalculateSchemaMetricsAsync("session-xyz", schemaAnalysis);

Console.WriteLine($"Tables: {schemaMetrics.MatchedTableCount}/{schemaMetrics.TotalTableCount} matched");
Console.WriteLine($"Columns: {schemaMetrics.MatchedColumnCount}/{schemaMetrics.TotalColumnCount} matched");
Console.WriteLine($"Overall Completeness: {schemaMetrics.SchemaCompleteness:F1}%");
```

### Data Comparison Analysis
```csharp
// Aggregate metrics from multiple table comparisons
var comparisons = new List<TableComparisonResult> { /* ... */ };
var dataMetrics = await service.AggregateDataMetricsAsync("session-xyz", comparisons);

Console.WriteLine($"Rows: {dataMetrics.MatchedRowCount}/{dataMetrics.TotalRowsLegacy} matched");
Console.WriteLine($"Data Loss: {dataMetrics.DataLossPercentage:F2}%");
Console.WriteLine($"Data Integrity: {dataMetrics.DataIntegrityScore:F1}%");
```

### Discrepancy Categorization
```csharp
// Analyze discrepancies with severity and category breakdown
var discrepancyAnalysis = new DiscrepancyAnalysisResult { /* ... */ };
var discMetrics = await service.CalculateDiscrepancyMetricsAsync("session-xyz", discrepancyAnalysis);

Console.WriteLine($"Total Issues: {discMetrics.TotalDiscrepancies}");
Console.WriteLine($"Critical: {discMetrics.CriticalCount}, High: {discMetrics.HighCount}");
Console.WriteLine($"Data Loss Issues: {discMetrics.DataLossIssues}");
Console.WriteLine($"Affected Tables: {discMetrics.TotalAffectedTables}");
```

### Quality Assessment
```csharp
// Calculate comprehensive quality metrics
var qualityMetrics = await service.CalculateQualityMetricsAsync("session-xyz");

Console.WriteLine($"Schema Accuracy: {qualityMetrics.SchemaAccuracy:F1}%");
Console.WriteLine($"Data Accuracy: {qualityMetrics.DataAccuracy:F1}%");
Console.WriteLine($"Overall Quality: {qualityMetrics.OverallAccuracy:F1}%");
Console.WriteLine($"Test Pass Rate: {qualityMetrics.TestPassRate:F1}%");

// Get module-level quality breakdown
var modules = await service.GetModuleQualityAsync("session-xyz");
foreach (var module in modules.OrderByDescending(m => m.OverallQualityScore))
{
    Console.WriteLine($"{module.ModuleName}: {module.OverallQualityScore:F1}% quality");
}
```

### Risk Assessment
```csharp
// Assess migration risk
var assessment = new MigrationRiskAssessment { /* ... */ };
var riskMetrics = await service.CalculateRiskMetricsAsync("session-xyz", assessment);

Console.WriteLine($"Risk Score: {riskMetrics.OverallRiskScore:F1}");
Console.WriteLine($"Critical: {riskMetrics.CriticalRisks}, High: {riskMetrics.HighRisks}");
Console.WriteLine($"Mitigation Coverage: {riskMetrics.MitigationCoveragePercent:F1}%");
Console.WriteLine($"Confidence: {riskMetrics.ConfidenceRating}");
```

### Migration Readiness Decision
```csharp
// Calculate readiness and get recommendation
var readiness = await service.CalculateReadinessAsync("session-xyz");

Console.WriteLine($"Recommendation: {readiness.RecommendedDecision}");
Console.WriteLine($"Rationale: {readiness.DecisionRationale}");
Console.WriteLine($"Technical Readiness: {readiness.TechnicalReadiness:F1}%");
Console.WriteLine($"Data Readiness: {readiness.DataReadiness:F1}%");

if (readiness.BlockingIssues > 0)
{
    Console.WriteLine($"WARNING: {readiness.BlockingIssues} blocking issues identified");
}
```

### Performance Tracking
```csharp
// Record phase execution metrics
await service.RecordPhasePerformanceAsync("session-xyz", "Discovery", 45000, 150);
await service.RecordPhasePerformanceAsync("session-xyz", "Comparison", 120000, 5000000);

var perfMetrics = await service.CalculatePerformanceMetricsAsync("session-xyz");
Console.WriteLine($"Total Duration: {TimeSpan.FromMilliseconds(perfMetrics.TotalDurationMs):hh\\:mm\\:ss}");
Console.WriteLine($"Throughput: {perfMetrics.TablesAnalyzedPerSecond:F1} tables/sec");
```

### Trend Analysis
```csharp
// Record snapshots at regular intervals
await service.RecordSnapshotAsync("session-xyz", "Discovery");
// ... perform work ...
await service.RecordSnapshotAsync("session-xyz", "TestGen");
// ... perform work ...
await service.RecordSnapshotAsync("session-xyz", "Execution");

// Analyze trends
var qualityTrend = await service.AnalyzeTrendAsync("session-xyz", "Quality");
Console.WriteLine($"Quality Trend: {qualityTrend.Direction}");
Console.WriteLine($"Change: {qualityTrend.ChangePercentage:+0.0%;-0.0%;0.0%}");

var riskTrend = await service.AnalyzeTrendAsync("session-xyz", "Risk");
Console.WriteLine($"Risk Trend: {riskTrend.Direction}");
```

### Health Assessment
```csharp
// Get comprehensive health status
var health = await service.CalculateHealthAsync("session-xyz");

Console.WriteLine($"Overall Health: {health.HealthStatus} ({health.HealthScore:F1})");
Console.WriteLine($"Schema Health: {health.SchemaHealth.Status}");
Console.WriteLine($"Data Health: {health.DataHealth.Status}");

var issues = await service.GetHealthIssuesAsync("session-xyz");
foreach (var issue in issues.Where(i => i.Severity == "Critical"))
{
    Console.WriteLine($"CRITICAL: {issue.Component} - {issue.Issue}");
}
```

### Module Risk Ranking
```csharp
// Identify highest-risk modules
var rankedModules = await service.RankModulesByRiskAsync("session-xyz");

foreach (var module in rankedModules.Take(5))
{
    Console.WriteLine($"{module.ModuleName}: Quality {module.OverallQualityScore:F1}%, Status: {module.MigrationStatus}");
    foreach (var issue in module.PendingIssues)
    {
        Console.WriteLine($"  - Pending: {issue}");
    }
}
```

### Recommendations and Improvements
```csharp
// Get actionable recommendations
var recommendations = await service.GetRecommendationsAsync("session-xyz");
foreach (var rec in recommendations.Where(r => r.Priority == "Critical"))
{
    Console.WriteLine($"[{rec.Priority}] {rec.Area}: {rec.Recommendation}");
    if (rec.EstimatedEffortDays.HasValue)
    {
        Console.WriteLine($"  Estimated Effort: {rec.EstimatedEffortDays} days");
    }
}

// Identify improvement opportunities
var improvements = await service.IdentifyImprovementAreasAsync("session-xyz");
foreach (var improvement in improvements)
{
    Console.WriteLine($"{improvement.Area}: {improvement.CurrentScore} → {improvement.TargetScore}");
    foreach (var action in improvement.Actions)
    {
        Console.WriteLine($"  • {action}");
    }
}
```

### Scenario Analysis
```csharp
// Model impact of potential scenarios
var scenario1 = await service.AnalyzeScenarioAsync(
    "session-xyz",
    "extended-preparation",
    new Dictionary<string, object> { { "days", 14 } });

Console.WriteLine($"Extended Preparation Scenario:");
Console.WriteLine($"  Success Probability: {scenario1.SuccessProbability:F1}%");
Console.WriteLine($"  Timeline Impact: {scenario1.TimelineImpactPercent:+0.0%;-0.0%;0.0%}");

var scenario2 = await service.AnalyzeScenarioAsync(
    "session-xyz",
    "phased-rollout",
    new Dictionary<string, object> { { "phases", 3 } });

Console.WriteLine($"Phased Rollout Scenario:");
Console.WriteLine($"  Success Probability: {scenario2.SuccessProbability:F1}%");
Console.WriteLine($"  Cost Impact: {scenario2.CostImpactPercent:+0.0%;-0.0%;0.0%}");
```

### Export and Reporting
```csharp
// Export metrics in different formats
var jsonExport = await service.ExportMetricsAsync("session-xyz", "json");
System.IO.File.WriteAllText("metrics.json", jsonExport);

var csvExport = await service.ExportMetricsAsync("session-xyz", "csv");
System.IO.File.WriteAllText("metrics.csv", csvExport);

var mdExport = await service.ExportMetricsAsync("session-xyz", "markdown");
System.IO.File.WriteAllText("metrics.md", mdExport);

// Compare metrics across sessions
var comparison = await service.CompareSessionsAsync("session-1", "session-2");
foreach (var result in comparison.Results.Values)
{
    Console.WriteLine($"{result.MetricName}: {result.Value1:F1} → {result.Value2:F1} ({result.Status})");
}
```

### Real-time Monitoring
```csharp
// Subscribe to metrics updates
service.SubscribeToMetricsUpdates("session-xyz", async metrics =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Metrics Updated");
    Console.WriteLine($"  Quality: {metrics.QualityMetrics.OverallAccuracy:F1}%");
    Console.WriteLine($"  Risk: {metrics.RiskMetrics.OverallRiskScore:F1}");
});

// Or use async streaming
await foreach (var metrics in service.GetMetricsStreamAsync("session-xyz"))
{
    Console.WriteLine($"Progress: {metrics.ReadinessMetrics.OverallReadiness:F1}% ready");
}
```

## Configuration

Default configuration suitable for most scenarios:
```csharp
var config = new MetricsCalculationConfig
{
    EnableAutoCalculation = true,
    RecalculationIntervalMs = 5000,
    SnapshotRetentionCount = 100,
    EnableModuleMetrics = true,
    EnableSnapshots = true,
    ExcellentThreshold = 95,
    GoodThreshold = 80,
    AcceptableThreshold = 65,
    PoorThreshold = 50
};

service.Configure(config);
```

Quality Score Interpretation:
- **95-100 (Excellent):** Production-ready with high confidence
- **80-94 (Good):** Acceptable for migration with minimal risk
- **65-79 (Acceptable):** Requires attention but viable
- **50-64 (Poor):** Significant issues need resolution
- **< 50 (Failing):** Not ready for migration

## Integration Patterns

### With Risk Calculator
```csharp
// Provide metrics as input to risk assessment
var metrics = await metricsService.GetMetricsAsync(sessionId);
var assessment = await riskCalculator.CalculateRiskAsync(
    sessionId,
    metrics.DataMetrics,
    metrics.DiscrepancyMetrics);
```

### With Report Generator
```csharp
// Export metrics for report generation
var metrics = await metricsService.AggregateAllMetricsAsync(sessionId);
var report = await reportGenerator.GenerateComparisonReportAsync(
    sessionId,
    metrics);
```

### With Dashboard Service
```csharp
// Push metrics to dashboard updates
service.SubscribeToMetricsUpdates(sessionId, async metrics =>
{
    await dashboardService.UpdateMetricsAsync(sessionId, metrics);
});
```

## Performance Characteristics

**Time Complexity:**
- `CalculateSchemaMetricsAsync`: O(n) - iterates schema mapping
- `AggregateDataMetricsAsync`: O(m) - sums across m tables
- `CalculateDiscrepancyMetricsAsync`: O(d) - processes d discrepancies
- `CalculateReadinessAsync`: O(1) - simple aggregation
- `AnalyzeTrendAsync`: O(t) - analyzes t snapshots
- `RecordSnapshotAsync`: O(1) - append operation

**Space Complexity:**
- Per-session storage: ~10 KB for ValidationMetrics
- Snapshots: ~100 bytes per snapshot × retention count
- Performance data: ~200 bytes per phase

**Scalability:**
- 10,000 concurrent sessions: ~100 MB memory
- Real-time subscribers: 100+ per session practical limit
- Snapshot retention: 1000+ snapshots per session manageable

## Testing Strategy

The test suite (`MetricsServiceTests.cs`) includes 50+ unit tests covering:
- Initialization and state management
- Schema metrics calculation
- Data metrics aggregation
- Discrepancy categorization
- Performance tracking
- Quality scoring
- Risk assessment
- Readiness determination
- Module ranking
- Scenario analysis
- Export functionality
- Session comparison
- Configuration management
- Subscriber patterns
- End-to-end scenarios

## Related Forms

- **Form 7: Risk Calculator** - Consumes metrics for risk assessment
- **Form 8: Report Generator** - Uses metrics for reporting
- **Form 9: Dashboard Service** - Displays metrics in UI
- **Form 10: Progress Service** - Parallel tracking metric
- **Form 12: Status Cache** - Distributes metric snapshots
- **Form 16: Ruflow Orchestrator** - Coordinates metric calculation

## See Also

- `IMetricsService`: Interface definition with all 30+ methods
- `MetricsService`: Implementation with calculations and aggregation
- `MetricsModels.cs`: All data model definitions
- `MetricsServiceTests.cs`: 50+ comprehensive unit tests
