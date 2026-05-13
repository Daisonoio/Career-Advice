namespace Aegis.Application.Profile.DTOs;

public record ProfileResponse(
    int UserId,
    string Email,
    string? CurrentRole,
    int? YearsExperience,
    string? LocationCountry,
    string? LocationCity,
    string? EnglishLevel,
    string? RemotePreference,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? CareerGoals,
    double ProfileCompleteness,
    string SubscriptionTier,
    List<SkillResponse> Skills,
    double? SalaryPercentileForCurrentRole,
    double? StagnationRiskScore,
    List<QuickWinResponse> QuickWins);

public record SkillResponse(
    int SkillId,
    string SkillName,
    string CanonicalName,
    string? Category,
    int SelfRatedLevel,
    double? YearsExperience,
    bool IsPrimary);

public record QuickWinResponse(
    string Action,
    string ImpactLevel,
    int EstimatedWeeks,
    string Rationale);
