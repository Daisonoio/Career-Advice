using Aegis.Application.Common.Exceptions;
using Aegis.Application.Profile.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Profile.Queries;

public record GetProfileQuery(int UserId) : IRequest<ProfileResponse>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);

        var skills = profile?.Skills.Select(s => new SkillResponse(
            SkillId: s.SkillId,
            SkillName: s.Skill?.Name ?? string.Empty,
            CanonicalName: s.Skill?.CanonicalName ?? string.Empty,
            Category: s.Skill?.Category?.Name,
            SelfRatedLevel: s.SelfRatedLevel,
            YearsExperience: s.YearsExperience,
            IsPrimary: s.IsPrimary)).ToList() ?? new List<SkillResponse>();

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
            Skills: skills);
    }
}
