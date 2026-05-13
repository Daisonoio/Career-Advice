namespace Aegis.Application.Market.DTOs;

public record MarketKpiResponse(
    int? SkillId,
    string? SkillName,
    string? RoleCanonical,
    string? GeoCountry,
    double? DemandScore,
    int? JobCount30d,
    double? JobCountYoyGrowth,
    decimal? SalaryMedian,
    decimal? SalaryP25,
    decimal? SalaryP75,
    string? SalaryCurrency,
    double? RemotePremiumPct,
    double? SaturationIndex,
    double? AIRiskScore,
    double? AutomationProbability,
    double? GrowthMomentum,
    double? CareerStability,
    double? DataConfidence,
    DateTime ComputedAt);

public record MarketTrendsResponse(
    List<SkillTrendDto> EmergingSkills,
    List<SkillTrendDto> DecliningSkills);

public record SkillTrendDto(
    int SkillId,
    string SkillName,
    double GrowthMomentum,
    double DemandScore,
    double? SalaryMedian);
