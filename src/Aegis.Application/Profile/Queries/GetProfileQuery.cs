using Aegis.Application.Common.Exceptions;
using Aegis.Application.Profile.DTOs;
using Aegis.Application.Profile.Services;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Profile.Queries;

public record GetProfileQuery(int UserId) : IRequest<ProfileResponse>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMarketKpiRepository _kpiRepository;
    private readonly ProfileEnrichmentService _enrichment;

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        IMarketKpiRepository kpiRepository,
        ProfileEnrichmentService enrichment)
    {
        _userRepository = userRepository;
        _kpiRepository  = kpiRepository;
        _enrichment     = enrichment;
    }

    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);

        // Fetch role KPI for enrichment — try country-specific first, then EU-wide
        var roleKpi = profile?.CurrentRole is not null
            ? await _kpiRepository.GetLatestAsync(null, profile.CurrentRole, profile.LocationCountry, cancellationToken)
              ?? await _kpiRepository.GetLatestAsync(null, profile.CurrentRole, "EU", cancellationToken)
            : null;

        double? salaryPercentile   = null;
        double? stagnationRisk     = null;
        var     quickWins          = new List<QuickWinResponse>();

        if (profile is not null)
        {
            salaryPercentile = _enrichment.ComputeSalaryPercentile(profile.SalaryExpectation?.Midpoint(), roleKpi);
            stagnationRisk   = _enrichment.ComputeStagnationRisk(profile, roleKpi);
            quickWins        = _enrichment.ComputeQuickWins(profile, roleKpi);
        }

        var skills = profile?.Skills.Select(s => new SkillResponse(
            SkillId: s.SkillId,
            SkillName: s.Skill?.Name ?? string.Empty,
            CanonicalName: s.Skill?.CanonicalName ?? string.Empty,
            Category: s.Skill?.Category?.Name,
            SelfRatedLevel: s.SelfRatedLevel,
            YearsExperience: s.YearsExperience,
            IsPrimary: s.IsPrimary)).ToList() ?? [];

        return new ProfileResponse(
            UserId: user.Id,
            Email: user.Email,
            CurrentRole: profile?.CurrentRole,
            YearsExperience: profile?.YearsExperience,
            LocationCountry: profile?.LocationCountry,
            LocationCity: profile?.LocationCity,
            EnglishLevel: profile?.EnglishLevel,
            RemotePreference: profile?.RemotePreference,
            SalaryMin: profile?.SalaryExpectation?.Min,
            SalaryMax: profile?.SalaryExpectation?.Max,
            SalaryCurrency: profile?.SalaryExpectation?.Currency,
            CareerGoals: profile?.CareerGoals,
            ProfileCompleteness: profile?.ProfileCompleteness ?? 0.0,
            SubscriptionTier: user.SubscriptionTier.ToString(),
            Skills: skills,
            SalaryPercentileForCurrentRole: salaryPercentile,
            StagnationRiskScore: stagnationRisk,
            QuickWins: quickWins);
    }
}
