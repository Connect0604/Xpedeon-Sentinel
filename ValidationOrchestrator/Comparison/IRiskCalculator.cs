namespace ValidationOrchestrator.Comparison;

using ValidationOrchestrator.Models;

/// <summary>
/// Interface for migration risk assessment and calculation
/// Converts discrepancies into actionable risk scores and Go/No-Go recommendations
/// </summary>
public interface IRiskCalculator
{
    // Risk Assessment
    /// <summary>
    /// Calculate overall migration risk from discrepancy analysis
    /// </summary>
    Task<MigrationRiskAssessment> AssessMigrationRiskAsync(
        DiscrepancyAnalysisResult discrepancies,
        string sessionId,
        string clientId);

    /// <summary>
    /// Assess risk for specific module
    /// </summary>
    Task<ModuleRiskAssessment> AssessModuleRiskAsync(
        string moduleName,
        List<DetailedDiscrepancy> moduleDiscrepancies,
        ImpactAssessment? moduleImpact = null);

    // Scoring
    /// <summary>
    /// Calculate overall risk score (0-100, higher = worse)
    /// </summary>
    double CalculateOverallRiskScore(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary);

    /// <summary>
    /// Calculate health score (0-100, higher = better)
    /// </summary>
    double CalculateHealthScore(
        List<DetailedDiscrepancy> discrepancies,
        DiscrepancySummary summary);

    /// <summary>
    /// Calculate module risk score
    /// </summary>
    double CalculateModuleRiskScore(
        List<DetailedDiscrepancy> moduleDiscrepancies);

    /// <summary>
    /// Calculate component-level risk scores
    /// </summary>
    List<RiskComponent> CalculateRiskComponents(
        List<DetailedDiscrepancy> discrepancies);

    // Decision Making
    /// <summary>
    /// Make Go/No-Go recommendation
    /// </summary>
    MigrationRecommendation MakeRecommendation(
        MigrationRiskAssessment assessment);

    /// <summary>
    /// Determine migration readiness
    /// </summary>
    MigrationReadiness DetermineMigrationReadiness(double riskScore);

    /// <summary>
    /// Identify blocking items for launch
    /// </summary>
    List<string> IdentifyGoBlockers(MigrationRiskAssessment assessment);

    /// <summary>
    /// Identify non-blocking warnings
    /// </summary>
    List<string> IdentifyWarnings(MigrationRiskAssessment assessment);

    // Critical Items
    /// <summary>
    /// Extract critical risk items from discrepancies
    /// </summary>
    List<CriticalRiskItem> ExtractCriticalItems(
        List<DetailedDiscrepancy> discrepancies);

    /// <summary>
    /// Prioritize critical items by risk contribution
    /// </summary>
    List<CriticalRiskItem> PrioritizeCriticalItems(
        List<CriticalRiskItem> items);

    /// <summary>
    /// Analyze dependencies between risk items
    /// </summary>
    void AnalyzeDependencies(List<CriticalRiskItem> items);

    // Mitigation
    /// <summary>
    /// Generate mitigation strategies for critical items
    /// </summary>
    List<RiskMitigationStrategy> GenerateMitigationStrategies(
        List<CriticalRiskItem> criticalItems,
        List<RemediationRecommendation> recommendations);

    /// <summary>
    /// Estimate time to readiness
    /// </summary>
    int EstimateDaysToReadiness(
        List<CriticalRiskItem> criticalItems);

    /// <summary>
    /// Create launch readiness checklist
    /// </summary>
    LaunchReadinessChecklist CreateReadinessChecklist(
        MigrationRiskAssessment assessment);

    // Trend Analysis
    /// <summary>
    /// Analyze risk trends if historical data available
    /// </summary>
    RiskTrendAnalysis AnalyzeTrends(
        List<MigrationRiskAssessment> historicalAssessments);

    /// <summary>
    /// Project readiness date based on trends
    /// </summary>
    DateTime? ProjectReadinessDate(RiskTrendAnalysis trends);

    // Metrics
    /// <summary>
    /// Generate comprehensive risk metrics
    /// </summary>
    RiskMetrics GenerateMetrics(MigrationRiskAssessment assessment);

    /// <summary>
    /// Calculate confidence level for recommendation
    /// </summary>
    double CalculateConfidenceLevel(MigrationRiskAssessment assessment);

    // Configuration
    /// <summary>
    /// Configure risk calculation parameters
    /// </summary>
    void Configure(RiskCalculationConfig config);

    /// <summary>
    /// Set custom scoring weights
    /// </summary>
    void SetScoringWeights(RiskScoringWeights weights);

    /// <summary>
    /// Get current configuration
    /// </summary>
    RiskCalculationConfig GetConfiguration();
}
