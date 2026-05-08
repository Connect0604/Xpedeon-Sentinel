namespace ValidationOrchestrator.Comparison;

using System.Text;
using Serilog;
using ValidationOrchestrator.Models;

/// <summary>
/// Generates synthetic test datasets for Phase 2 execution.
/// Data is production-realistic for the Xpedeon construction ERP domain.
/// </summary>
public class TestDataService : ITestDataService
{
    private readonly ILogger _logger;
    private TestGenerationConfig _config;

    // In-memory store keyed by "sessionId:clientId"
    private readonly Dictionary<string, List<TestDataSet>> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _testCaseLinks = new();

    // Xpedeon ERP domain reference data
    private static readonly string[] Sites = { "SITE-001", "SITE-002", "SITE-003", "SITE-004", "SITE-005" };
    private static readonly string[] Vendors = { "VEND-001", "VEND-002", "VEND-003", "VEND-004", "VEND-010" };
    private static readonly string[] Projects = { "PROJ-2024-001", "PROJ-2024-002", "PROJ-2025-001", "PROJ-2025-002" };
    private static readonly string[] Employees = { "EMP-001", "EMP-002", "EMP-003", "EMP-010", "EMP-020" };
    private static readonly string[] MaterialCodes = { "MAT-CEMENT", "MAT-STEEL", "MAT-SAND", "MAT-PIPE", "MAT-WIRE" };
    private static readonly string[] Equipment = { "PLANT-CRANE-01", "PLANT-MIXER-01", "PLANT-PUMP-01", "PLANT-GEN-01" };

    private static readonly decimal[] TypicalAmounts = { 1000m, 5000m, 25000m, 100000m, 500000m };
    private static readonly decimal[] TaxRates = { 0m, 0.05m, 0.12m, 0.18m, 0.28m };

    public TestDataService(ILogger? logger = null)
    {
        _logger = logger ?? new LoggerConfiguration().CreateLogger();
        _config = new TestGenerationConfig();
        _logger.Information("TestDataService initialized");
    }

    public async Task<TestDataSet> GenerateModuleDataAsync(
        string module,
        string sessionId,
        string clientId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Generating {Category} data for module {Module}", TestDataCategory.Normal, module);

        var dataSet = module.ToUpperInvariant() switch
        {
            "FINANCE" or "ACCOUNTING" => GenerateFinanceData(sessionId, config),
            "INVENTORY" => GenerateInventoryData(sessionId, config),
            "PROCUREMENT" => GenerateProcurementData(sessionId, config),
            "HR" or "PAYROLL" => GenerateHrPayrollData(sessionId, config),
            "PLANTMANAGEMENT" or "PLANT" => GeneratePlantData(sessionId, config),
            _ => GenerateGenericData(module, sessionId, config)
        };

        await StoreDataSetAsync(sessionId, clientId, dataSet);
        return await Task.FromResult(dataSet);
    }

    public async Task<TestDataSet> GenerateEdgeCaseDataAsync(
        string module,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var dataSet = new TestDataSet
        {
            Name = $"{module} — Edge Case Data",
            Module = module,
            Description = $"Boundary values and extreme conditions for {module}",
            Category = TestDataCategory.EdgeCase,
            GeneratedBy = "Template"
        };

        // Financial edge cases
        if (module is "Finance" or "Accounting" or "Payroll")
        {
            dataSet.TableData["Invoices"] = new()
            {
                MakeRow("INV-EDGE-001", 0m, 0m, "Zero-value invoice"),
                MakeRow("INV-EDGE-002", 0.01m, 0m, "Minimum invoice amount"),
                MakeRow("INV-EDGE-003", 9999999.99m, 1799999.99m, "Maximum invoice amount"),
                MakeRow("INV-EDGE-004", -100m, -18m, "Negative amount (credit note)"),
                MakeRow("INV-EDGE-005", 100.005m, 18.001m, "Rounding edge: 3 decimal places"),
                MakeRow("INV-EDGE-006", 100.004m, 18.0007m, "Rounding edge: round down"),
            };
        }

        // Date boundary edge cases
        dataSet.TableData["DateBoundaries"] = new()
        {
            new() { { "TestId", "DATE-FY-START" }, { "Date", "2025-04-01" }, { "Description", "Fiscal year start" } },
            new() { { "TestId", "DATE-FY-END" }, { "Date", "2026-03-31" }, { "Description", "Fiscal year end" } },
            new() { { "TestId", "DATE-LEAP" }, { "Date", "2024-02-29" }, { "Description", "Leap year date" } },
            new() { { "TestId", "DATE-QUARTER" }, { "Date", "2025-06-30" }, { "Description", "Quarter end" } },
        };

        // Workflow edge cases
        dataSet.TableData["WorkflowEdges"] = new()
        {
            new() { { "ScenarioId", "WF-RESUBMIT" }, { "Action", "Resubmit after rejection" } },
            new() { { "ScenarioId", "WF-CONCURRENT" }, { "Action", "Two approvers simultaneously" } },
            new() { { "ScenarioId", "WF-DELEGATED" }, { "Action", "Delegated approval authority" } },
        };

        dataSet.LegacySeedScripts = BuildLegacySeedScripts(dataSet);
        dataSet.BlazonSeedScripts = BuildBlazonSeedScripts(dataSet);
        dataSet.CleanupScripts = BuildCleanupScripts(dataSet);

        return await Task.FromResult(dataSet);
    }

    public async Task<TestDataSet> GenerateLargeVolumeDataAsync(
        string module,
        int rowCount,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Generating large-volume dataset: {Count} rows for {Module}", rowCount, module);

        var dataSet = new TestDataSet
        {
            Name = $"{module} — Large Volume ({rowCount:N0} rows)",
            Module = module,
            Description = $"Performance testing dataset with {rowCount:N0} rows",
            Category = TestDataCategory.LargeVolume,
            GeneratedBy = "Template"
        };

        var rows = new List<Dictionary<string, object?>>();
        var rng = new Random(42); // seeded for reproducibility

        for (int i = 0; i < rowCount; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var amount = TypicalAmounts[rng.Next(TypicalAmounts.Length)] * (decimal)(0.5 + rng.NextDouble());
            var taxRate = TaxRates[rng.Next(TaxRates.Length)];
            rows.Add(new()
            {
                { "RecordId", $"PERF-{i:D8}" },
                { "ProjectId", Projects[rng.Next(Projects.Length)] },
                { "SiteId", Sites[rng.Next(Sites.Length)] },
                { "Amount", Math.Round(amount, 2) },
                { "TaxRate", taxRate },
                { "TaxAmount", Math.Round(amount * taxRate, 2) },
                { "NetAmount", Math.Round(amount + amount * taxRate, 2) },
                { "Date", DateTime.Today.AddDays(-rng.Next(730)).ToString("yyyy-MM-dd") }
            });
        }

        dataSet.TableData[$"{module}PerformanceData"] = rows;
        dataSet.LegacySeedScripts = new() { $"-- Bulk insert {rowCount:N0} rows into {module} test table" };
        dataSet.BlazonSeedScripts = new() { $"-- Bulk insert {rowCount:N0} rows into Blazor {module} test table" };
        dataSet.CleanupScripts = BuildCleanupScripts(dataSet);

        return await Task.FromResult(dataSet);
    }

    public async Task<TestDataSet> GenerateMultiSiteDataAsync(
        int siteCount,
        string sessionId,
        TestGenerationConfig config,
        CancellationToken cancellationToken = default)
    {
        var dataSet = new TestDataSet
        {
            Name = $"Multi-Site Dataset ({siteCount} sites)",
            Module = "CrossSite",
            Description = $"Covers {siteCount} construction sites for cross-site validation",
            Category = TestDataCategory.MultiSite,
            GeneratedBy = "Template"
        };

        var sites = Sites.Take(siteCount).ToArray();

        dataSet.TableData["Sites"] = sites.Select((s, i) => new Dictionary<string, object?>
        {
            { "SiteId", s },
            { "SiteName", $"Construction Site {i + 1}" },
            { "City", new[] { "Mumbai", "Delhi", "Bangalore", "Chennai", "Hyderabad" }[i % 5] },
            { "IsActive", true }
        }).ToList();

        dataSet.TableData["CrossSiteTransfers"] = Enumerable.Range(0, siteCount * 3).Select(i => new Dictionary<string, object?>
        {
            { "TransferId", $"TRF-{i:D4}" },
            { "FromSite", sites[i % siteCount] },
            { "ToSite", sites[(i + 1) % siteCount] },
            { "MaterialCode", MaterialCodes[i % MaterialCodes.Length] },
            { "Quantity", (i + 1) * 10 },
            { "TransferDate", DateTime.Today.AddDays(-i * 7).ToString("yyyy-MM-dd") }
        }).ToList();

        dataSet.LegacySeedScripts = BuildLegacySeedScripts(dataSet);
        dataSet.BlazonSeedScripts = BuildBlazonSeedScripts(dataSet);
        dataSet.CleanupScripts = BuildCleanupScripts(dataSet);

        return await Task.FromResult(dataSet);
    }

    public async Task<List<TestDataSet>> GenerateAllDatasetsAsync(
        TestSuite suite,
        List<BusinessLogicItem> logicItems,
        TestGenerationConfig config,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dataSets = new List<TestDataSet>();
        var modules = suite.CoveredModules.Distinct().ToList();
        int done = 0;

        foreach (var module in modules)
        {
            if (cancellationToken.IsCancellationRequested) break;

            dataSets.Add(await GenerateModuleDataAsync(module, suite.SessionId, suite.ClientId, config, cancellationToken));
            dataSets.Add(await GenerateEdgeCaseDataAsync(module, suite.SessionId, config, cancellationToken));

            if (config.GeneratePerformanceCases)
                dataSets.Add(await GenerateLargeVolumeDataAsync(module, config.LargeVolumeRowCount, suite.SessionId, config, cancellationToken));

            done++;
            progress?.Report((int)((double)done / modules.Count * 100));
        }

        if (config.MultiSiteCount > 1)
            dataSets.Add(await GenerateMultiSiteDataAsync(config.MultiSiteCount, suite.SessionId, config, cancellationToken));

        _logger.Information("Generated {Count} datasets for {Modules} modules", dataSets.Count, modules.Count);
        return dataSets;
    }

    public TestDataSet GenerateFinanceData(string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = "Finance — Standard Test Data",
            Module = "Finance",
            Description = "Invoices, payments, tax records for financial validation",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData["Invoices"] = Enumerable.Range(1, 20).Select(i => new Dictionary<string, object?>
        {
            { "InvoiceId", $"INV-TEST-{i:D3}" },
            { "VendorId", Vendors[i % Vendors.Length] },
            { "ProjectId", Projects[i % Projects.Length] },
            { "Amount", TypicalAmounts[i % TypicalAmounts.Length] },
            { "TaxRate", TaxRates[i % TaxRates.Length] },
            { "TaxAmount", Math.Round(TypicalAmounts[i % TypicalAmounts.Length] * TaxRates[i % TaxRates.Length], 2) },
            { "InvoiceDate", DateTime.Today.AddDays(-i * 15).ToString("yyyy-MM-dd") },
            { "Status", i % 3 == 0 ? "Paid" : i % 3 == 1 ? "Pending" : "Approved" }
        }).ToList();

        ds.TableData["Payments"] = Enumerable.Range(1, 10).Select(i => new Dictionary<string, object?>
        {
            { "PaymentId", $"PAY-TEST-{i:D3}" },
            { "InvoiceId", $"INV-TEST-{i:D3}" },
            { "Amount", TypicalAmounts[i % TypicalAmounts.Length] },
            { "PaymentDate", DateTime.Today.AddDays(-i * 10).ToString("yyyy-MM-dd") },
            { "Method", i % 2 == 0 ? "BankTransfer" : "Cheque" }
        }).ToList();

        ds.LegacySeedScripts = BuildLegacySeedScripts(ds);
        ds.BlazonSeedScripts = BuildBlazonSeedScripts(ds);
        ds.CleanupScripts = BuildCleanupScripts(ds);
        return ds;
    }

    public TestDataSet GenerateInventoryData(string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = "Inventory — Standard Test Data",
            Module = "Inventory",
            Description = "Stock records, material movements for inventory validation",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData["StockLevels"] = MaterialCodes.Select((code, i) => new Dictionary<string, object?>
        {
            { "MaterialCode", code },
            { "SiteId", Sites[i % Sites.Length] },
            { "CurrentStock", (i + 1) * 100 },
            { "MinimumLevel", 50 },
            { "MaximumLevel", 1000 },
            { "UOM", i % 2 == 0 ? "KG" : "NOS" }
        }).ToList();

        ds.TableData["MaterialMovements"] = Enumerable.Range(1, 30).Select(i => new Dictionary<string, object?>
        {
            { "MovementId", $"MOV-{i:D4}" },
            { "MaterialCode", MaterialCodes[i % MaterialCodes.Length] },
            { "SiteId", Sites[i % Sites.Length] },
            { "MovementType", i % 3 == 0 ? "Receipt" : i % 3 == 1 ? "Issue" : "Transfer" },
            { "Quantity", (i % 5 + 1) * 10 },
            { "MovementDate", DateTime.Today.AddDays(-i * 3).ToString("yyyy-MM-dd") }
        }).ToList();

        ds.LegacySeedScripts = BuildLegacySeedScripts(ds);
        ds.BlazonSeedScripts = BuildBlazonSeedScripts(ds);
        ds.CleanupScripts = BuildCleanupScripts(ds);
        return ds;
    }

    public TestDataSet GenerateProcurementData(string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = "Procurement — Standard Test Data",
            Module = "Procurement",
            Description = "Purchase orders, vendor approvals for procurement validation",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData["PurchaseOrders"] = Enumerable.Range(1, 15).Select(i => new Dictionary<string, object?>
        {
            { "POId", $"PO-TEST-{i:D3}" },
            { "VendorId", Vendors[i % Vendors.Length] },
            { "ProjectId", Projects[i % Projects.Length] },
            { "TotalAmount", TypicalAmounts[i % TypicalAmounts.Length] },
            { "ApprovalStatus", i % 4 == 0 ? "Pending" : i % 4 == 1 ? "L1Approved" : i % 4 == 2 ? "L2Approved" : "Final" },
            { "PODate", DateTime.Today.AddDays(-i * 7).ToString("yyyy-MM-dd") }
        }).ToList();

        ds.LegacySeedScripts = BuildLegacySeedScripts(ds);
        ds.BlazonSeedScripts = BuildBlazonSeedScripts(ds);
        ds.CleanupScripts = BuildCleanupScripts(ds);
        return ds;
    }

    public TestDataSet GenerateHrPayrollData(string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = "HR/Payroll — Standard Test Data",
            Module = "HR",
            Description = "Employees, salary records for HR and payroll validation",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData["Employees"] = Employees.Select((emp, i) => new Dictionary<string, object?>
        {
            { "EmployeeId", emp },
            { "Name", $"Test Employee {i + 1}" },
            { "Department", new[] { "Finance", "Operations", "HR", "IT", "Site" }[i % 5] },
            { "GrossSalary", 30000m + i * 10000m },
            { "TaxBracket", i % 3 == 0 ? "5%" : i % 3 == 1 ? "20%" : "30%" },
            { "JoiningDate", DateTime.Today.AddYears(-(i + 1)).ToString("yyyy-MM-dd") }
        }).ToList();

        ds.TableData["PayrollRuns"] = Enumerable.Range(1, 6).Select(i => new Dictionary<string, object?>
        {
            { "PayrollRunId", $"PR-{DateTime.Today.AddMonths(-i):yyyy-MM}" },
            { "Month", DateTime.Today.AddMonths(-i).Month },
            { "Year", DateTime.Today.AddMonths(-i).Year },
            { "TotalGross", 150000m + i * 5000m },
            { "TotalDeductions", 45000m + i * 1500m },
            { "TotalNet", 105000m + i * 3500m },
            { "Status", i == 1 ? "Processing" : "Posted" }
        }).ToList();

        ds.LegacySeedScripts = BuildLegacySeedScripts(ds);
        ds.BlazonSeedScripts = BuildBlazonSeedScripts(ds);
        ds.CleanupScripts = BuildCleanupScripts(ds);
        return ds;
    }

    public TestDataSet GeneratePlantData(string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = "Plant Management — Standard Test Data",
            Module = "PlantManagement",
            Description = "Equipment hire, maintenance records for plant management validation",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData["Equipment"] = Equipment.Select((eq, i) => new Dictionary<string, object?>
        {
            { "EquipmentId", eq },
            { "EquipmentName", eq.Replace("PLANT-", "").Replace("-01", "") },
            { "SiteId", Sites[i % Sites.Length] },
            { "HireRatePerDay", 500m + i * 250m },
            { "Status", i % 3 == 0 ? "Available" : i % 3 == 1 ? "InUse" : "Maintenance" },
            { "LastServiceDate", DateTime.Today.AddDays(-i * 30).ToString("yyyy-MM-dd") }
        }).ToList();

        ds.TableData["HireRecords"] = Enumerable.Range(1, 10).Select(i => new Dictionary<string, object?>
        {
            { "HireId", $"HIRE-{i:D4}" },
            { "EquipmentId", Equipment[i % Equipment.Length] },
            { "ProjectId", Projects[i % Projects.Length] },
            { "StartDate", DateTime.Today.AddDays(-i * 14).ToString("yyyy-MM-dd") },
            { "EndDate", i % 2 == 0 ? DateTime.Today.AddDays(-i * 7).ToString("yyyy-MM-dd") : (object?)null },
            { "HireRatePerDay", 500m + (i % 4) * 250m },
            { "TotalAmount", (500m + (i % 4) * 250m) * 14 }
        }).ToList();

        ds.LegacySeedScripts = BuildLegacySeedScripts(ds);
        ds.BlazonSeedScripts = BuildBlazonSeedScripts(ds);
        ds.CleanupScripts = BuildCleanupScripts(ds);
        return ds;
    }

    public List<string> BuildLegacySeedScripts(TestDataSet dataSet)
    {
        var scripts = new List<string>();

        foreach (var (table, rows) in dataSet.TableData)
        {
            if (!rows.Any()) continue;
            var columns = rows.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"-- Legacy DB seed: {table} ({rows.Count} rows)");
            sb.AppendLine($"-- Dataset: {dataSet.Name}");

            foreach (var row in rows)
            {
                var values = columns.Select(c => FormatSqlValue(row.GetValueOrDefault(c)));
                sb.AppendLine($"INSERT INTO [dbo].[{table}] ({string.Join(", ", columns.Select(c => $"[{c}]"))}) VALUES ({string.Join(", ", values)});");
            }

            scripts.Add(sb.ToString());
        }

        return scripts;
    }

    public List<string> BuildBlazonSeedScripts(TestDataSet dataSet)
    {
        // Blazor uses EF Core conventions: PascalCase table names, no schema prefix
        var scripts = new List<string>();

        foreach (var (table, rows) in dataSet.TableData)
        {
            if (!rows.Any()) continue;
            var columns = rows.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"-- Blazor DB seed: {table} ({rows.Count} rows)");

            foreach (var row in rows)
            {
                var values = columns.Select(c => FormatSqlValue(row.GetValueOrDefault(c)));
                sb.AppendLine($"INSERT INTO [{table}] ({string.Join(", ", columns.Select(c => $"[{c}]"))}) VALUES ({string.Join(", ", values)});");
            }

            scripts.Add(sb.ToString());
        }

        return scripts;
    }

    public List<string> BuildCleanupScripts(TestDataSet dataSet)
    {
        return dataSet.TableData.Keys
            .Select(table => $"DELETE FROM [{table}] WHERE [{GetPrimaryKeyColumn(table)}] LIKE '%-TEST-%' OR [{GetPrimaryKeyColumn(table)}] LIKE 'PERF-%' OR [{GetPrimaryKeyColumn(table)}] LIKE '%-EDGE-%';")
            .ToList();
    }

    public Task StoreDataSetAsync(string sessionId, string clientId, TestDataSet dataSet)
    {
        var key = $"{sessionId}:{clientId}";
        if (!_store.ContainsKey(key)) _store[key] = new();
        _store[key].Add(dataSet);
        return Task.CompletedTask;
    }

    public Task<List<TestDataSet>> GetDataSetsAsync(string sessionId, string clientId)
    {
        return Task.FromResult(_store.GetValueOrDefault($"{sessionId}:{clientId}") ?? new());
    }

    public Task LinkDataSetToTestCaseAsync(string sessionId, string dataSetId, string testCaseId)
    {
        if (!_testCaseLinks.ContainsKey(sessionId)) _testCaseLinks[sessionId] = new();
        _testCaseLinks[sessionId][testCaseId] = dataSetId;
        return Task.CompletedTask;
    }

    public void Configure(TestGenerationConfig config) => _config = config;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TestDataSet GenerateGenericData(string module, string sessionId, TestGenerationConfig config)
    {
        var ds = new TestDataSet
        {
            Name = $"{module} — Standard Test Data",
            Module = module,
            Description = $"Generic test data for {module} module",
            Category = TestDataCategory.Normal,
            GeneratedBy = "Template"
        };

        ds.TableData[$"{module}Records"] = Enumerable.Range(1, 10).Select(i => new Dictionary<string, object?>
        {
            { "RecordId", $"{module.ToUpper()}-TEST-{i:D3}" },
            { "Description", $"Test record {i} for {module}" },
            { "Amount", 1000m * i },
            { "Date", DateTime.Today.AddDays(-i * 7).ToString("yyyy-MM-dd") }
        }).ToList();

        ds.LegacySeedScripts = new() { $"-- Generic seed for {module}" };
        ds.BlazonSeedScripts = new() { $"-- Generic Blazor seed for {module}" };
        ds.CleanupScripts = new() { $"DELETE FROM [{module}Records] WHERE RecordId LIKE '{module.ToUpper()}-TEST-%';" };
        return ds;
    }

    private static Dictionary<string, object?> MakeRow(string id, decimal amount, decimal tax, string desc) =>
        new()
        {
            { "Id", id },
            { "Amount", amount },
            { "TaxAmount", tax },
            { "Description", desc },
            { "Date", DateTime.Today.ToString("yyyy-MM-dd") }
        };

    private static string FormatSqlValue(object? value) => value switch
    {
        null => "NULL",
        bool b => b ? "1" : "0",
        string s => $"N'{s.Replace("'", "''")}'",
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
        decimal d => d.ToString("F4"),
        _ => value.ToString() ?? "NULL"
    };

    private static string GetPrimaryKeyColumn(string tableName)
    {
        return tableName switch
        {
            "Invoices" => "InvoiceId",
            "Payments" => "PaymentId",
            "PurchaseOrders" => "POId",
            "Employees" => "EmployeeId",
            "PayrollRuns" => "PayrollRunId",
            "Equipment" => "EquipmentId",
            "HireRecords" => "HireId",
            "StockLevels" => "MaterialCode",
            "MaterialMovements" => "MovementId",
            _ => "RecordId"
        };
    }
}
