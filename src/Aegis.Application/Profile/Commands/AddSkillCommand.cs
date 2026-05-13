using Aegis.Application.Common.Exceptions;
using Aegis.Application.Profile.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Profile.Commands;

public record AddSkillCommand(
    int UserId,
    int SkillId,
    int SelfRatedLevel,
    double? YearsExperience,
    bool IsPrimary) : IRequest<SkillResponse>;

public class AddSkillCommandValidator : AbstractValidator<AddSkillCommand>
{
    public AddSkillCommandValidator()
    {
        RuleFor(x => x.SkillId).GreaterThan(0).WithMessage("SkillId must be a positive integer.");
        RuleFor(x => x.SelfRatedLevel)
            .InclusiveBetween(1, 5)
            .WithMessage("Self-rated level must be between 1 and 5.");
        RuleFor(x => x.YearsExperience)
            .GreaterThanOrEqualTo(0)
            .When(x => x.YearsExperience.HasValue)
            .WithMessage("Years of experience must be non-negative.");
    }
}

public class AddSkillCommandHandler : IRequestHandler<AddSkillCommand, SkillResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ISkillRepository _skillRepository;

    public AddSkillCommandHandler(IUserRepository userRepository, ISkillRepository skillRepository)
    {
        _userRepository = userRepository;
        _skillRepository = skillRepository;
    }

    public async Task<SkillResponse> Handle(AddSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepository.GetByIdAsync(request.SkillId, cancellationToken)
            ?? throw new NotFoundException(nameof(Skill), request.SkillId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new NotFoundException(nameof(User), request.UserId);
            profile = UserProfile.Create(request.UserId);
        }

        profile.AddOrUpdateSkill(request.SkillId, request.SelfRatedLevel, request.YearsExperience, request.IsPrimary);
        await _userRepository.AddOrUpdateProfileAsync(profile, cancellationToken);

        return new SkillResponse(
            SkillId: skill.Id,
            SkillName: skill.Name,
            CanonicalName: skill.CanonicalName,
            Category: skill.Category?.Name,
            SelfRatedLevel: request.SelfRatedLevel,
            YearsExperience: request.YearsExperience,
            IsPrimary: request.IsPrimary);
    }
}
