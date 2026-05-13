namespace Aegis.Domain.Entities;

/// <summary>
/// All KPI values are computed deterministically by KpiComputationService.
/// No LLM generates these numbers.
/// </summary>
public class MarketKpi : Entity
{
    public int? SkillId { get; private set; }
    public string? RoleCanonical { get; private set; }
    public string? GeoCountry { get; private set; }
    public string? GeoCity { get; private set; }

    // KPI-01: Market Demand (0–10)
    public double? DemandScore { get; private set; }
    public int? JobCount30d { get; private set; }
    public double? JobCountYoyGrowth { get; private set; }

    // KPI-02: Salary Strength
    public decimal? SalaryMedian { get; private set; }
    public decimal? SalaryP25 { get; private set; }
    public decimal? SalaryP75 { get; private set; }
    public string? SalaryCurrency { get; private set; }
    public double? RemotePremiumPct { get; private set; }

    // KPI-03: Saturation Index (0–10, 10=very saturated)
    public double? SaturationIndex { get; private set; }
    public double? EstimatedApplicantsPerJob { get; private set; }

    // KPI-04: AI Risk Score (0–10, 10=high risk)
    public double? AIRiskScore { get; private set; }
    public double? AutomationProbability { get; private set; }

    // KPI-05: Growth Momentum (0–10)
    public double? GrowthMomentum { get; private set; }
    public double? SkillMentionTrend90d { get; private set; }

    // KPI-06: Career Stability (0–10)
    public double? CareerStability { get; private set; }
    public double? LayoffSensitivity { get; private set; }

    // Meta
    public double? DataConfidence { get; private set; }

    private MarketKpi() { }

    public static MarketKpi Create(int? skillId, string? roleCanonical, string? geoCountry)
        => new() { SkillId = skillId, RoleCanonical = roleCanonical, GeoCountry = geoCountry };

    public void SetDemand(double score, int jobCount, double yoyGrowth)
    {
        DemandScore = score;
        JobCount30d = jobCount;
        JobCountYoyGrowth = yoyGrowth;
        Touch();
    }

    public void SetSalary(decimal median, decimal p25, decimal p75, string currency, double remotePremium)
    {
        SalaryMedian = median;
        SalaryP25 = p25;
        SalaryP75 = p75;
        SalaryCurrency = currency;
        RemotePremiumPct = remotePremium;
        Touch();
    }

    public void SetAIRisk(double riskScore, double automationProbability)
    {
        AIRiskScore = riskScore;
        AutomationProbability = automationProbability;
        Touch();
    }

    public void SetConfidence(double confidence)
    {
        DataConfidence = confidence;
        Touch();
    }
}
