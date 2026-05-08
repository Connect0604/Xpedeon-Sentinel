# Xpedeon Validation Orchestrator - Technical Architecture

**Date:** May 8, 2026  
**Tech Stack:** C# + Ruflow + Blazor + SQL Server  
**Status:** Architecture Design

---

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     VALIDATION ORCHESTRATOR                      │
│                      (Ruflow Workflow)                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  RUFLOW WORKFLOW STEPS                    │  │
│  │                                                            │  │
│  │  1. Initialize                                            │  │
│  │     ├─ Load configuration                                │  │
│  │     ├─ Connect to legacy DB                             │  │
│  │     └─ Connect to Blazor DB                             │  │
│  │                                                            │  │
│  │  2. Discovery Phase                                       │  │
│  │     ├─ Extract legacy code logic                         │  │
│  │     ├─ Build business logic inventory                    │  │
│  │     └─ Identify validation checkpoints                   │  │
│  │                                                            │  │
│  │  3. Test Generation Phase                                 │  │
│  │     ├─ Generate test scenarios                           │  │
│  │     ├─ Create test data sets                             │  │
│  │     └─ Prepare comparison queries                        │  │
│  │                                                            │  │
│  │  4. Parallel Execution Phase                              │  │
│  │     ├─ Execute tests on legacy system                    │  │
│  │     ├─ Execute tests on Blazor system                    │  │
│  │     └─ Collect results & metrics                         │  │
│  │                                                            │  │
│  │  5. Comparison Phase                                      │  │
│  │     ├─ Compare results (exact match)                     │  │
│  │     ├─ Analyze discrepancies                             │  │
│  │     ├─ Calculate risk scores                             │  │
│  │     └─ Generate findings                                 │  │
│  │                                                            │  │
│  │  6. Reporting Phase                                       │  │
│  │     ├─ Generate Markdown reports                         │  │
│  │     ├─ Update dashboard with findings                    │  │
│  │     └─ Create risk assessment                            │  │
│  │                                                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                             ↓                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │           COMPARISON & ANALYSIS ENGINE (C#)              │  │
│  │                                                            │  │
│  │  • TableComparer - Compare tables row-by-row            │  │
│  │  • SchemaAnalyzer - Map schema and relationships         │  │
│  │  • BusinessLogicExtractor - Extract patterns             │  │
│  │  • DiscrepancyDetector - Find differences                │  │
│  │  • RiskCalculator - Score risk by module                │  │
│  │                                                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                             ↓                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │        SQL SERVER COMPARISON & VALIDATION                │  │
│  │                                                            │  │
│  │  • Stored Procedures (read-only via dbo schema)         │  │
│  │    - sp_CompareTable - Compare specific tables          │  │
│  │    - sp_CalculateHash - Data integrity check            │  │
│  │    - sp_ExtractMetrics - Performance metrics            │  │
│  │    - sp_ValidateFK - Referential integrity              │  │
│  │    - sp_CalculateChecksum - Data completeness           │  │
│  │                                                            │  │
│  │  • Direct SQL Queries (via C# ADO.NET/EF Core)         │  │
│  │    - Extract schema information                          │  │
│  │    - Run business logic validations                      │  │
│  │    - Calculate financial/inventory metrics               │  │
│  │                                                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                             ↓                                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │           REPORTING ENGINE                               │  │
│  │                                                            │  │
│  │  • MarkdownReportGenerator - Create detailed reports    │  │
│  │  • DashboardDataProvider - Feed findings to Blazor      │  │
│  │  • SignoffGenerator - Generate sign-off documents       │  │
│  │                                                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
          ┌───────────────────────────────────┐
          │   OUTPUT: Markdown Reports        │
          │   + Blazor Dashboard              │
          │   + Risk Assessment               │
          │   + Sign-off Documents            │
          └───────────────────────────────────┘
```

---

## Ruflow Workflow Structure

### Workflow Definition

```
ValidationOrchestrator Workflow
├─ Steps
│  ├─ InitializeStep
│  │  ├─ LoadConfiguration
│  │  ├─ ConnectLegacyDB
│  │  └─ ConnectBlazonDB
│  │
│  ├─ DiscoveryStep
│  │  ├─ ExtractLegacyCodeLogic
│  │  ├─ AnalyzeWinFormsPatterns
│  │  └─ BuildLogicInventory
│  │
│  ├─ TestGenerationStep
│  │  ├─ GenerateTestScenarios
│  │  ├─ CreateTestDataSets
│  │  └─ PrepareValidationQueries
│  │
│  ├─ ParallelExecutionStep
│  │  ├─ ExecuteLegacyTests (parallel)
│  │  ├─ ExecuteBlazonTests (parallel)
│  │  └─ CollectResults
│  │
│  ├─ ComparisonStep
│  │  ├─ CompareResults
│  │  ├─ AnalyzeDiscrepancies
│  │  ├─ CalculateRiskScores
│  │  └─ GenerateFindings
│  │
│  └─ ReportingStep
│     ├─ GenerateMarkdownReports
│     ├─ UpdateDashboard
│     ├─ CreateRiskAssessment
│     └─ GenerateSignoff
│
├─ Conditions
│  ├─ ContinueOnError: false (stop if critical error)
│  ├─ RetryOnFailure: true (with exponential backoff)
│  └─ ParallelExecution: true (Steps 4 runs in parallel)
│
└─ Outputs
   ├─ MarkdownReports (list of files)
   ├─ DashboardData (JSON for Blazor)
   ├─ RiskAssessment (structured data)
   └─ ExecutionLog (detailed trace)
```

### Ruflow Step Implementations

**Each step is implemented as:**
- Ruflow Activity (C# class)
- Input/Output contracts (what it receives, what it returns)
- Error handling & logging
- Integration with comparison engine

---

## C# Components

### 1. Comparison Engine

```csharp
// Core comparison interfaces
public interface ITableComparer
{
    ComparisonResult Compare(string tableName, DataSet legacyData, DataSet blazorData);
}

public interface ISchemaAnalyzer
{
    SchemaMap AnalyzeSchema(SqlConnection legacy, SqlConnection blazor);
}

public interface IBusinessLogicExtractor
{
    BusinessLogicInventory ExtractFromWinForms(string csharpCodePath);
}

public interface IDiscrepancyDetector
{
    List<Discrepancy> DetectDifferences(ComparisonResult comparison);
}

public interface IRiskCalculator
{
    RiskAssessment CalculateRisk(List<Discrepancy> discrepancies);
}
```

### 2. Database Access Layer

```csharp
// SQL Server connection management
public class LegacyDatabaseConnector
{
    public DataSet ExecuteQuery(string query, string clientId);
    public DataSet ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters);
}

public class BlazonDatabaseConnector
{
    public DataSet ExecuteQuery(string query, string clientId);
    public DataSet ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters);
}

// Stored procedure wrappers
public class ComparisonProcedures
{
    public DataSet CompareTable(string tableName);
    public string CalculateDataHash(string tableName);
    public DataSet ExtractMetrics();
    public DataSet ValidateReferentialIntegrity();
    public DataSet CalculateChecksum();
}
```

### 3. Reporting Engine

```csharp
public interface IMarkdownReportGenerator
{
    string GenerateReport(ValidationResults results);
    void WriteToFile(string content, string outputPath);
}

public interface IDashboardDataProvider
{
    DashboardData GetDashboardData(ValidationResults results);
    void UpdateDashboard(DashboardData data);
}

public interface ISignoffDocumentGenerator
{
    string GenerateSignoffDocument(RiskAssessment assessment);
}
```

---

## Blazor Dashboard

### Components

```
Dashboard Layout
├─ Header
│  ├─ Project Name: Xpedeon Migration Validation
│  ├─ Client: [Single Client Name]
│  ├─ Status: [In Progress / Completed]
│  └─ Execution Time: [HH:MM:SS]
│
├─ Executive Summary
│  ├─ Overall Risk Score: [0-100%]
│  ├─ Modules Validated: [X/Y]
│  ├─ Critical Issues: [Count]
│  ├─ High Issues: [Count]
│  ├─ Medium Issues: [Count]
│  └─ Low Issues: [Count]
│
├─ Module Risk Breakdown (Pie Chart + Table)
│  ├─ Accounting: [Risk %]
│  ├─ Procurement: [Risk %]
│  ├─ Inventory: [Risk %]
│  ├─ Plant Management: [Risk %]
│  ├─ HR/Payroll: [Risk %]
│  └─ Other modules...
│
├─ Validation Phase Progress
│  ├─ Phase 1 - Discovery: [Progress bar]
│  ├─ Phase 2 - Test Generation: [Progress bar]
│  ├─ Phase 3 - Execution: [Progress bar]
│  ├─ Phase 4 - Comparison: [Progress bar]
│  ├─ Phase 5 - Expert Review: [Progress bar]
│  └─ Phase 6 - Final Report: [Progress bar]
│
├─ Critical Issues (Expandable List)
│  ├─ [Issue #1]
│  │  ├─ Module: Accounting
│  │  ├─ Table: Invoices
│  │  ├─ Issue: Financial calculation mismatch
│  │  ├─ Severity: CRITICAL
│  │  ├─ Impact: 100% of invoices
│  │  └─ Evidence: [Legacy: $1000.00, Blazor: $999.50]
│  └─ [More issues...]
│
├─ Data Integrity Checks
│  ├─ Total Records Legacy: [X]
│  ├─ Total Records Blazor: [X]
│  ├─ Match Rate: [%]
│  ├─ Missing Records: [Count]
│  └─ Extra Records: [Count]
│
├─ Performance Metrics
│  ├─ Query Performance
│  │  ├─ Avg Legacy Query: [Xms]
│  │  ├─ Avg Blazor Query: [Xms]
│  │  └─ Variance: [%]
│  └─ System Metrics
│     ├─ CPU Usage
│     ├─ Memory Usage
│     └─ Database Locks
│
├─ Detailed Findings Table
│  ├─ Search/Filter
│  ├─ Columns: Module | Table | Issue | Severity | Status | Owner
│  └─ Sort options: By Severity, By Module, By Status
│
└─ Actions
   ├─ Download Markdown Report
   ├─ Export to PDF
   ├─ Export to Excel
   ├─ View Logs
   ├─ Re-run Validation
   └─ Sign Off
```

### Blazor Components (C#)

```csharp
// Main dashboard
@page "/validation-dashboard"
public partial class ValidationDashboard
{
    private ValidationResults results;
    private RiskAssessment riskAssessment;
    // Component logic
}

// Subcomponents
- ModuleRiskChart.razor (pie chart)
- PhaseProgressBar.razor (progress tracking)
- IssuesList.razor (detailed findings)
- DataIntegrityMetrics.razor (comparison stats)
- PerformanceMetrics.razor (query performance)
- ActionButtons.razor (export, download, etc.)
```

---

## Database Schema - Comparison Strategy

### Tables to Validate (dbo schema)

Assuming standard construction ERP tables:

```sql
-- Core business tables
dbo.Projects
dbo.Invoices
dbo.PurchaseOrders
dbo.Inventory
dbo.Equipment
dbo.Employees
dbo.Subcontractors
dbo.Transactions
dbo.Approvals
dbo.Reports
[... and other tables]
```

### Comparison Approach

**1. Schema Mapping (by C#)**
```
Legacy Table → Blazor Table (via column name/type mapping)
Handle renames/restructures
```

**2. Data Comparison (via SQL stored procedures)**
```sql
-- Stored Procedure: sp_CompareTable
-- Input: @tableName (e.g., 'Invoices')
-- Output: Comparison results
--   - Matching rows
--   - Missing in Blazor
--   - Extra in Blazor
--   - Column value differences
```

**3. Integrity Checks (via SQL)**
```sql
-- Referential integrity
-- Constraint validation
-- Data type consistency
-- NULL handling
-- Numeric precision (especially money)
```

---

## Execution Flow

### CLI Execution

```bash
# Run the validation orchestrator
dotnet run --client "ClientName" --legacy-db "connection-string" --blazor-db "connection-string" --output "reports/"

# Or with configuration file
dotnet run --config "validation-config.json"
```

### Dashboard Execution

```
1. User navigates to Blazor dashboard
2. Clicks "Start Validation"
3. Ruflow workflow executes in background
4. Dashboard updates in real-time
5. User sees progress, issues, and results
6. User can download reports or sign off
```

---

## Data Integrity Validation

### Key Validations

```
1. Record Count
   - Total records in legacy vs Blazor
   - Missing/extra records flagged

2. Column-by-Column Comparison
   - Exact match for strings/dates
   - Fuzzy match for floating point (within 0.01%)
   - NULL handling consistency

3. Calculated Fields
   - Total = Sum(detail)
   - Balances match
   - Financial calculations correct

4. Referential Integrity
   - Foreign keys valid in both systems
   - No orphaned records
   - Cascade deletes work correctly

5. Business Rule Validation
   - Discount calculations
   - Tax calculations
   - Approval workflow states
   - Status transitions valid
```

---

## Error Handling & Logging

### Ruflow Workflow Error Handling

```csharp
// Each step has:
- Try/catch with logging
- Specific error types (DataException, ValidationException, etc.)
- Retry logic with exponential backoff
- Graceful degradation (skip failed validation, log, continue)
```

### Logging Strategy

```
- INFO: Workflow progress, phase changes
- WARN: Non-critical mismatches, data warnings
- ERROR: Critical failures, data loss, calculation errors
- DEBUG: Detailed comparison results, SQL queries

Logs stored in: /logs/validation_YYYYMMDD_HHMMSS.log
```

---

## File Structure

```
Xpedeon-Sentinel/
├─ ValidationOrchestrator/
│  ├─ Ruflow/
│  │  ├─ ValidationOrchestrator.cs (main workflow)
│  │  ├─ Steps/
│  │  │  ├─ InitializeStep.cs
│  │  │  ├─ DiscoveryStep.cs
│  │  │  ├─ TestGenerationStep.cs
│  │  │  ├─ ExecutionStep.cs
│  │  │  ├─ ComparisonStep.cs
│  │  │  └─ ReportingStep.cs
│  │  └─ Activities/ (Ruflow activities)
│  │
│  ├─ Comparison/
│  │  ├─ TableComparer.cs
│  │  ├─ SchemaAnalyzer.cs
│  │  ├─ BusinessLogicExtractor.cs
│  │  ├─ DiscrepancyDetector.cs
│  │  └─ RiskCalculator.cs
│  │
│  ├─ Database/
│  │  ├─ LegacyDatabaseConnector.cs
│  │  ├─ BlazonDatabaseConnector.cs
│  │  └─ ComparisonProcedures.cs
│  │
│  ├─ Reporting/
│  │  ├─ MarkdownReportGenerator.cs
│  │  ├─ DashboardDataProvider.cs
│  │  └─ SignoffDocumentGenerator.cs
│  │
│  └─ Models/
│     ├─ ValidationResults.cs
│     ├─ RiskAssessment.cs
│     ├─ Discrepancy.cs
│     └─ [other models]
│
├─ ValidationDashboard/ (Blazor Web App)
│  ├─ Components/
│  │  ├─ ValidationDashboard.razor
│  │  ├─ ModuleRiskChart.razor
│  │  ├─ PhaseProgressBar.razor
│  │  ├─ IssuesList.razor
│  │  ├─ DataIntegrityMetrics.razor
│  │  ├─ PerformanceMetrics.razor
│  │  └─ ActionButtons.razor
│  │
│  ├─ Services/
│  │  ├─ ValidationService.cs
│  │  └─ DashboardService.cs
│  │
│  └─ Pages/
│     └─ Index.razor
│
├─ Reports/
│  └─ [Generated markdown reports]
│
├─ Logs/
│  └─ [Execution logs]
│
└─ Tests/
   ├─ ComparisonEngineTests.cs
   ├─ DatabaseConnectorTests.cs
   └─ WorkflowTests.cs
```

---

## Tech Stack Summary

| Component | Technology |
|-----------|-----------|
| Orchestration | Ruflow (C#) |
| Comparison Engine | C# (ADO.NET / EF Core) |
| Database | SQL Server (stored procedures) |
| Dashboard | Blazor Web App (C#) |
| Reporting | Markdown + C# |
| CLI | dotnet console app |
| Testing | xUnit (no preference so we use standard) |
| Logging | Serilog (standard for .NET) |

---

## Next Steps - Build Order

1. ✅ **Architecture defined** (this document)
2. ⬜ **Create project structure** (C# projects + Blazor app)
3. ⬜ **Build database connectors** (legacy & Blazor DB access)
4. ⬜ **Build comparison engine** (table comparer, schema analyzer)
5. ⬜ **Create SQL stored procedures** (comparison queries)
6. ⬜ **Build Ruflow workflow** (orchestration steps)
7. ⬜ **Create Blazor dashboard** (real-time monitoring)
8. ⬜ **Build reporting engine** (markdown + signoff)
9. ⬜ **Integration & testing** (end-to-end)
10. ⬜ **Execute validation phases** (run the orchestrator)

---

**END OF ARCHITECTURE DOCUMENT**
