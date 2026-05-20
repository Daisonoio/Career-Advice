using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Application.Market.Services;

/// <summary>
/// Computes all KPI values deterministically from raw market data.
/// This service never calls any LLM. All output is based on aggregated data only.
/// </summary>
public class KpiComputationService
{
    private readonly IMarketKpiRepository _kpiRepo;
    private readonly ILogger<KpiComputationService> _logger;

    public KpiComputationService(IMarketKpiRepository kpiRepo, ILogger<KpiComputationService> logger)
    {
        _kpiRepo = kpiRepo;
        _logger = logger;
    }

    public async Task<MarketKpi> ComputeForRoleAsync(
        string roleCanonical,
        string? geoCountry,
        RawMarketData data,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Computing KPIs for role {Role} in {Geo}", roleCanonical, geoCountry);

        var kpi = MarketKpi.Create(null, roleCanonical, geoCountry);

        // KPI-01: Market Demand Score
        var demandScore = ComputeDemandScore(data.JobCount30d, data.JobCountBaseline, data.HiringVelocity);
        kpi.SetDemand(demandScore, data.JobCount30d, data.JobCountYoyGrowthPct);

        // KPI-02: Salary Strength
        if (data.SalaryDataPoints.Count >= 5)
        {
            var sorted = data.SalaryDataPoints.Order().ToList();
            var median = Percentile(sorted, 50);
            var p25 = Percentile(sorted, 25);
            var p75 = Percentile(sorted, 75);
            kpi.SetSalary(median, p25, p75, data.SalaryCurrency, data.RemotePremiumPct);
        }

        // KPI-04: AI Risk Score
        var aiRisk = ComputeAIRisk(data.AutomationProbability, data.CommoditizationIndex);
        kpi.SetAIRisk(aiRisk, data.AutomationProbability);

        // KPI-05: Growth Momentum derived from YoY job-count change
        var growthMomentum = ComputeGrowthMomentum(data.JobCountYoyGrowthPct);
        kpi.SetGrowthMomentum(growthMomentum);

        // Confidence based on data volume and recency
        var confidence = ComputeDataConfidence(data.JobCount30d, data.SalaryDataPoints.Count, data.DataAgeHours);
        kpi.SetConfidence(confidence);

        await _kpiRepo.UpsertAsync(kpi, ct);
        return kpi;
    }

    private static double ComputeDemandScore(int jobCount, int baseline, double hiringVelocity)
    {
        if (baseline <= 0) return 0;
        var relativeDemand = Math.Min(jobCount / (double)baseline, 3.0);
        var score = relativeDemand * 3.0 + hiringVelocity * 7.0;
        return Math.Clamp(score, 0, 10);
    }

    // Maps YoY job-count growth % to a 0–10 momentum score.
    // 0% growth → 5.0 (neutral); ±30% growth → ±5 points from neutral.
    private static double ComputeGrowthMomentum(double yoyGrowthPct)
    {
        var score = 5.0 + Math.Clamp(yoyGrowthPct / 6.0, -5.0, 5.0);
        return Math.Round(score, 2);
    }

    private static double ComputeAIRisk(double automationProb, double commoditizationIndex)
    {
        var score = automationProb * 7.0 + commoditizationIndex * 3.0;
        return Math.Clamp(score, 0, 10);
    }

    private static double ComputeDataConfidence(int jobCount, int salaryPoints, int dataAgeHours)
    {
        var volumeScore = Math.Min(jobCount / 100.0, 1.0) * 0.5;
        var salaryScore = Math.Min(salaryPoints / 20.0, 1.0) * 0.3;
        var freshnessScore = Math.Max(1.0 - dataAgeHours / 168.0, 0) * 0.2;
        return volumeScore + salaryScore + freshnessScore;
    }

    private static decimal Percentile(List<decimal> sorted, int percentile)
    {
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        return lower == upper ? sorted[lower] : (sorted[lower] + sorted[upper]) / 2;
    }
}

public record RawMarketData(
    int JobCount30d,
    int JobCountBaseline,
    double JobCountYoyGrowthPct,
    double HiringVelocity,
    List<decimal> SalaryDataPoints,
    string SalaryCurrency,
    double RemotePremiumPct,
    double AutomationProbability,
    double CommoditizationIndex,
    int DataAgeHours);
