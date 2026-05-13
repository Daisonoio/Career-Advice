using Aegis.Application.Common.Exceptions;
using Aegis.Application.Profile.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Aegis.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Profile.Commands;

public record UpdateProfileCommand(
    int UserId,
    string? CurrentRole,
    int? YearsExperience,
    string? LocationCountry,
    string? LocationCity,
    string? EnglishLevel,
    string? RemotePreference,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? CareerGoals) : IRequest<ProfileResponse>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.YearsExperience)
            .InclusiveBetween(0, 50)
            .When(x => x.YearsExperience.HasValue)
            .WithMessage("Years of experience must be between 0 and 50.");

        RuleFor(x => x)
            .Must(x => x.SalaryMin < x.SalaryMax)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue)
            .WithMessage("SalaryMin must be less than SalaryMax.");
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ProfileResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(request.UserId);
        }

        SalaryRange? salaryRange = null;
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && !string.IsNullOrWhiteSpace(request.SalaryCurrency))
        {
            salaryRange = new SalaryRange(request.SalaryMin.Value, request.SalaryMax.Value, request.SalaryCurrency);
        }

        profile.Update(
            request.CurrentRole,
            request.YearsExperience,
            request.LocationCountry,
            request.LocationCity,
            request.EnglishLevel,
            request.RemotePreference,
            salaryRange,
            request.CareerGoals);

        await _userRepository.AddOrUpdateProfileAsync(profile, cancellationToken);

        return MapToResponse(user, profile);
    }

    private static ProfileResponse MapToResponse(User user, UserProfile profile)
    {
        var skills = profile.Skills.Select(s => new SkillResponse(
            SkillId: s.SkillId,
            SkillName: s.Skill?.Name ?? string.Empty,
            CanonicalName: s.Skill?.CanonicalName ?? string.Empty,
            Category: s.Skill?.Category?.Name,
            SelfRatedLevel: s.SelfRatedLevel,
            YearsExperience: s.YearsExperience,
            IsPrimary: s.IsPrimary)).ToList();

        return new ProfileResponse(
            UserId: user.Id,
            Email: user.Email,
            CurrentRole: profile.CurrentRole,
            YearsExperience: profile.YearsExperience,
            LocationCountry: profile.LocationCountry,
            LocationCity: profile.LocationCity,
            EnglishLevel: profile.EnglishLevel,
            RemotePreference: profile.RemotePreference,
            SalaryMin: profile.SalaryExpectation?.Min,
            SalaryMax: profile.SalaryExpectation?.Max,
            SalaryCurrency: profile.SalaryExpectation?.Currency,
            CareerGoals: profile.CareerGoals,
            ProfileCompleteness: profile.ProfileCompleteness,
            SubscriptionTier: user.SubscriptionTier.ToString(),
            Skills: skills);
    }
}
