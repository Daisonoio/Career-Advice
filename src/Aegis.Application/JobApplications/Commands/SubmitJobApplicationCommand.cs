using Aegis.Application.Interfaces;
using Aegis.Application.JobApplications.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Aegis.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.JobApplications.Commands;

public record SubmitJobApplicationCommand(
    int UserId,
    string JobTitle,
    string? CompanyName,
    string RawDescription) : IRequest<SubmitJobApplicationResponse>;

public class SubmitJobApplicationCommandValidator : AbstractValidator<SubmitJobApplicationCommand>
{
    public SubmitJobApplicationCommandValidator()
    {
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CompanyName).MaximumLength(300);
        RuleFor(x => x.RawDescription)
            .NotEmpty()
            .MinimumLength(50)
            .WithMessage("Job description must be at least 50 characters to extract meaningful skill requirements.")
            .MaximumLength(20000);
    }
}

public class SubmitJobApplicationCommandHandler : IRequestHandler<SubmitJobApplicationCommand, SubmitJobApplicationResponse>
{
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IAIOrchestrator _aiOrchestrator;

    public SubmitJobApplicationCommandHandler(
        IJobApplicationRepository jobApplicationRepository,
        IUserRepository userRepository,
        ISkillRepository skillRepository,
        IAIOrchestrator aiOrchestrator)
    {
        _jobApplicationRepository = jobApplicationRepository;
        _userRepository = userRepository;
        _skillRepository = skillRepository;
        _aiOrchestrator = aiOrchestrator;
    }

    public async Task<SubmitJobApplicationResponse> Handle(SubmitJobApplicationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);
        var userSkillCanonicals = profile?.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var jobApplication = JobApplication.Submit(request.UserId, request.JobTitle, request.CompanyName, request.RawDescription);
        await _jobApplicationRepository.AddAsync(jobApplication, cancellationToken);

        // LLM only labels the text — it never invents market numbers. Matching those
        // labels to canonical skills and scoring the overlap happens deterministically below.
        var extractedLabels = await _aiOrchestrator.ExtractRequiredSkillsAsync(
            request.JobTitle, request.RawDescription, cancellationToken);

        var requiredSkills = new List<JobRequiredSkill>();
        foreach (var label in extractedLabels.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            // SearchByNameAsync is a substring ILIKE match (also used for autocomplete),
            // not an exact resolver — with several candidates it can return the wrong
            // one (e.g. "SQL" matching "PostgreSQL" before "SQL Server"). Disambiguate
            // deterministically instead of blindly trusting the first alphabetical hit.
            var matches = await _skillRepository.SearchByNameAsync(label, limit: 10, ct: cancellationToken);
            var match = PickBestMatch(matches, label);
            var canonical = match?.CanonicalName ?? Slugify(label);
            var displayName = match?.Name ?? label;

            requiredSkills.Add(new JobRequiredSkill(
                SkillName: displayName,
                SkillCanonical: canonical,
                MatchedToProfile: userSkillCanonicals.Contains(canonical)));
        }

        var overlapCount = requiredSkills.Count(s => s.MatchedToProfile);
        var initialMatchScorePct = requiredSkills.Count > 0
            ? Math.Round((double)overlapCount / requiredSkills.Count * 100, 1)
            : 0.0;

        jobApplication.SetExtractedSkills(JsonSerializer.Serialize(requiredSkills), initialMatchScorePct);
        await _jobApplicationRepository.UpdateAsync(jobApplication, cancellationToken);

        return new SubmitJobApplicationResponse(
            JobApplicationId: jobApplication.Id,
            Status: jobApplication.Status.ToString(),
            MatchScorePct: initialMatchScorePct,
            RequiredSkills: requiredSkills
                .Select(s => new RequiredSkillDto(s.SkillName, s.SkillCanonical, s.MatchedToProfile))
                .ToList());
    }

    // Prefers an exact name match, then a name that starts with the label (shortest
    // first), falling back to the shortest overall match among the ILIKE hits.
    private static Domain.Entities.Skill? PickBestMatch(List<Domain.Entities.Skill> matches, string label)
    {
        if (matches.Count <= 1) return matches.FirstOrDefault();

        var exact = matches.FirstOrDefault(s =>
            string.Equals(s.Name, label, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.CanonicalName, label, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        var startsWith = matches
            .Where(s => s.Name.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Name.Length)
            .FirstOrDefault();
        if (startsWith is not null) return startsWith;

        return matches.OrderBy(s => s.Name.Length).First();
    }

    private static string Slugify(string label)
        => string.Join("-", label.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
