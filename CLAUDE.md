# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Xpedeon Sentinel is an AI-powered validation orchestrator that guards Xpedeon's migration integrity from a legacy WinForms ERP to a Blazor ERP — validating business logic, data accuracy, and schema compatibility across both systems via SQL Server.

## Commands

All commands run from `ValidationOrchestrator/`:

```bash
# Build the main project
dotnet build

# Build the test project
dotnet build Tests/ValidationOrchestrator.Tests.csproj

# Run all tests
dotnet test Tests/ValidationOrchestrator.Tests.csproj

# Run a single test class
dotnet test Tests/ValidationOrchestrator.Tests.csproj --filter "FullyQualifiedName~ValidationMemoryServiceTests"

# Run a single test method
dotnet test Tests/ValidationOrchestrator.Tests.csproj --filter "FullyQualifiedName~ValidationMemoryServiceTests.CreateSessionAsync_ShouldInitializePhases"
```

## Project Structure

There is one C# project with a separate test project inside it:

```
ValidationOrchestrator/
├── ValidationOrchestrator.csproj       # Main library (excludes Tests/ via <Compile Remove>)
├── Tests/
│   ├── ValidationOrchestrator.Tests.csproj  # Separate test project; refs main via ProjectReference
│   └── *Tests.cs                            # xUnit + Moq + FluentAssertions
├── Comparison/          # All services and interfaces
├── Database/            # DB connectors for legacy and Blazor SQL Server
└── Models/              # One model file per domain (schema, discrepancy, risk, etc.)
```

**Key constraint:** `Tests/` is excluded from `ValidationOrchestrator.csproj` via `<Compile Remove="Tests\**" />`. Test files must only be built through `ValidationOrchestrator.Tests.csproj`.

## Architecture

### 6-Phase Validation Pipeline

`RuflowOrchestrator` drives the pipeline sequentially: **Discovery → TestGeneration → Execution → Comparison → Review → Reporting**. Each phase is a named step executed via `ExecutePhaseAsync`. `WorkflowConfig.StopOnError` controls whether the pipeline halts on failure.

### Service Layer (`Comparison/`)

Every service has a paired interface (`IFoo` / `Foo`). Key services:

| Service | Role |
|---|---|
| `RuflowOrchestrator` | Master workflow driver; manages `WorkflowContext` per session |
| `ValidationMemoryService` | Cross-phase session state store; compresses artifacts via `CavemanCompressionService` |
| `SchemaAnalyzer` | Maps legacy↔Blazor schema; produces `SchemaAnalysisResult` with `SchemaCompatibility` enum |
| `TableComparer` | Row-by-row SQL comparison; produces `TableComparisonResult` with `long` record counts |
| `DiscrepancyDetector` | Converts comparison results into `DetailedDiscrepancy` list; `Category` is a `string` field |
| `RiskCalculator` | Scores migration risk; outputs `MigrationRiskAssessment` with `CriticalItems` list |
| `MetricsService` | Aggregates all phase metrics into `ValidationMetrics` |
| `ReportGenerator` | Produces `ValidationReport` with markdown sections per phase |
| `DashboardService` | Real-time session dashboard via `IAsyncEnumerable<DashboardUpdate>` streams |
| `StatusCacheService` | Generic `SetAsync<T>` / `GetAsync<T>` cache partitioned by session |
| `ProgressService` | Tracks phase progress and milestone events |
| `CavemanCompressionService` | Semantic text compression used to store large phase artifacts in memory |
| `NotificationService` | Dispatches alerts and notifications |
| `HealthCheckService` | System health monitoring |
| `DashboardConfigService` | Dashboard layout/widget configuration |

### Model Organization (`Models/`)

Each model file owns one domain:

- `ValidationSessionModels.cs` — `ValidationSession`, `SessionPhaseStatus`, `DiscoveryFindings`, `ExecutionResults`, `Discrepancy`, `RiskAssessment`
- `SchemaModels.cs` — `SchemaAnalysisResult`, `SchemaCompatibility` enum, `DifferenceSeverity` enum (Critical/High/Medium/Low)
- `TableComparisonModels.cs` — `TableComparisonResult` (record counts are `long`), `ColumnComparison`, `ColumnValueDifference`
- `DiscrepancyModels.cs` — `DiscrepancyAnalysisResult`, `DetailedDiscrepancy` (`Category` is `string`, not enum), `DiscrepancyCategory` enum, `DiscrepancySeverity` enum
- `RiskCalculatorModels.cs` — `MigrationRiskAssessment` (`CriticalItems: List<CriticalRiskItem>`, `GoBlockers: List<string>`)
- `MetricsModels.cs` — `ValidationMetrics`, `ValidationRiskMetrics`, `CacheStatistics` (hit/miss/eviction counts are `long`)
- `StatusCacheModels.cs` — `CacheEntry<T>`, `CacheStatistics`, `CacheWarmUpPlan` (`WarmupFunction`, not `Revalidator`)
- `ReportGeneratorModels.cs` — `ExecutiveSummary` (includes `GoBlockers: List<string>`)

### Database Layer (`Database/`)

`LegacyDatabaseConnector` and `BlazonDatabaseConnector` both implement `IDatabaseConnector`. The base `DatabaseConnector` uses `Microsoft.Data.SqlClient` for connections to the two SQL Server databases (legacy WinForms DB and Blazor DB).

### Cross-Cutting Patterns

- **Session-scoped state**: all services key their in-memory state by `sessionId` (string).
- **Compression**: `ValidationMemoryService` compresses phase artifacts using `CavemanCompressionService` before storing; decompressed results are cached in `_decompressionCache`.
- **Streaming**: `DashboardService`, `MetricsService`, `ProgressService`, and `StatusCacheService` expose `IAsyncEnumerable<T>` streams for real-time updates. These methods require `[EnumeratorCancellation]` on the `CancellationToken` parameter to properly propagate cancellation.
- **Claude AI**: The `Anthropic` SDK (v1.0.0) is referenced for AI-assisted analysis within the pipeline.
- **Logging**: Serilog is used throughout; all services accept `ILogger? logger = null` and fall back to a default logger.

## Key Type Gotchas

- `TableComparisonResult` record counts (`LegacyRecordCount`, `BlazonRecordCount`, `MatchedRecords`, `MissingRecords`, `ExtraRecords`, `ModifiedRecords`) are all `long`. Cast explicitly when assigning to `int` fields.
- `DetailedDiscrepancy.Category` is a `string` (not `DiscrepancyCategory` enum). Category strings used in code: `"DataType"`, `"DataValue"`, `"Completeness"`, `"Consistency"`, `"DataIntegrity"`, `"BusinessLogic"`, `"Performance"`, `"SchemaStructure"`.
- `SchemaAnalysisResult.Compatibility` is `SchemaCompatibility` enum — not a nested type. Use `SchemaCompatibility.FullyCompatible`, not `SchemaAnalysisResult.CompatibilityLevel.FullyCompatible`.
- `CacheWarmUpPlan.WarmupFunction` is `Func<string, Task<object>>?` — not `Revalidator`.
- `MigrationRiskAssessment` exposes `CriticalItems` (`List<CriticalRiskItem>`) and `ModuleRisks` (`List<ModuleRiskAssessment>`), not `CriticalRisks` or `AffectedBusinessProcesses`.
- Switch expressions using non-constant `double` thresholds (e.g. from config) must be written as if-else chains — C# relational patterns in switch expressions require compile-time constants.
