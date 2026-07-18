using Aegis.Application.Assessment.DTOs;
using Aegis.Application.Assessment.Services;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Application.Recommendations.Services;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Aegis.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.Assessment.Commands;

public record SubmitAnswerCommand(
    int UserId,
    int AssessmentId,
    int QuestionId,
    string Answer) : IRequest<AssessmentQuestionResponse>;

public class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.Answer).NotEmpty().WithMessage("Answer is required.");
        RuleFor(x => x.AssessmentId).GreaterThan(0).WithMessage("AssessmentId must be valid.");
        RuleFor(x => x.QuestionId).GreaterThan(0).WithMessage("QuestionId must be valid.");
    }
}

public class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand, AssessmentQuestionResponse>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IAIOrchestrator _aiOrchestrator;
    private readonly SeniorityScorer _seniorityScorer;
    private readonly AdaptiveInterviewService _adaptiveService;
    private readonly SkillPrioritizationService _skillPrioritization;

    public SubmitAnswerCommandHandler(
        IAssessmentRepository assessmentRepository,
        IUserRepository userRepository,
        IJobApplicationRepository jobApplicationRepository,
        IAIOrchestrator aiOrchestrator,
        SeniorityScorer seniorityScorer,
        AdaptiveInterviewService adaptiveService,
        SkillPrioritizationService skillPrioritization)
    {
        _assessmentRepository = assessmentRepository;
        _userRepository = userRepository;
        _jobApplicationRepository = jobApplicationRepository;
        _aiOrchestrator = aiOrchestrator;
        _seniorityScorer = seniorityScorer;
        _adaptiveService = adaptiveService;
        _skillPrioritization = skillPrioritization;
    }

    public async Task<AssessmentQuestionResponse> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        if (assessment.UserId != request.UserId)
            throw new UnauthorizedException("This assessment does not belong to the current user.");

        if (assessment.IsComplete)
            throw new ConflictException("Assessment is already completed.");

        var question = assessment.Questions.FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new NotFoundException(nameof(AssessmentQuestion), request.QuestionId);

        if (question.AnsweredAt.HasValue)
            throw new ConflictException("This question has already been answered.");

        // Evaluate answer via AI
        var skillName = question.SkillId.HasValue ? "Software Engineering" : "Software Engineering";
        var evaluation = await _aiOrchestrator.EvaluateAnswerAsync(
            question: question.QuestionText,
            answer: request.Answer,
            skill: skillName,
            difficulty: question.DifficultyLevel,
            ct: cancellationToken);

        assessment.AnswerCurrentQuestion(request.QuestionId, request.Answer, evaluation.Score);

        // Determine next step
        var nextStep = _adaptiveService.DetermineNextStep(assessment);

        if (nextStep.ShouldComplete)
        {
            await CompleteAssessmentAsync(assessment, cancellationToken);
            await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

            var result = assessment.Result!;
            return new AssessmentQuestionResponse(
                IsCompleted: true,
                NextQuestion: null,
                Result: new AssessmentResultSummary(
                    EstimatedSeniority: assessment.EstimatedSeniority?.ToString() ?? "Junior",
                    Confidence: assessment.Confidence ?? 0.0,
                    ValidatedSkillsCount: result.ValidatedSkills.Count));
        }

        // Advance layer if needed
        if (nextStep.NextLayer > assessment.CurrentLayer)
            assessment.AdvanceToLayer(nextStep.NextLayer);

        // Job-targeted assessments cycle through the job's missing skills instead of
        // the user's generic primary skill.
        var jobTargetSkill = assessment.JobApplicationId.HasValue
            ? await ResolveJobTargetSkillAsync(assessment, cancellationToken)
            : null;

        string primarySkill;
        if (jobTargetSkill is not null)
        {
            primarySkill = jobTargetSkill;
        }
        else
        {
            var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);
            primarySkill = nextStep.TargetSkill
                ?? profile?.Skills.FirstOrDefault(s => s.IsPrimary)?.Skill?.Name
                ?? "Software Engineering";
        }

        // Previous answers for context
        var previousAnswers = assessment.Questions
            .Where(q => q.AnsweredAt.HasValue)
            .TakeLast(3)
            .Select(q => (q.QuestionText, q.AnswerText ?? string.Empty))
            .ToList();

        var ctx = new AssessmentQuestionContext(
            Skill: primarySkill,
            Difficulty: nextStep.NextDifficulty,
            SeniorityTarget: "Mid",
            PreviousAnswers: previousAnswers);

        var nextQuestionText = await _aiOrchestrator.GenerateAssessmentQuestionAsync(ctx, cancellationToken);
        assessment.AddQuestion(nextQuestionText, null, nextStep.NextDifficulty, nextStep.NextLayer);

        await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

        var nextQuestion = assessment.Questions.Last();

        return new AssessmentQuestionResponse(
            IsCompleted: false,
            NextQuestion: new AssessmentQuestionDto(
                QuestionId: nextQuestion.Id,
                QuestionText: nextQuestion.QuestionText,
                Layer: nextQuestion.Layer,
                Difficulty: nextQuestion.DifficultyLevel),
            Result: null);
    }

    private async Task CompleteAssessmentAsync(Aegis.Domain.Entities.Assessment assessment, CancellationToken cancellationToken)
    {
        var seniorityResult = _seniorityScorer.Score(assessment.Questions);

        // Build validated skills from answered questions
        var validatedSkills = assessment.Questions
            .Where(q => q.EvaluatedScore.HasValue && q.EvaluatedScore.Value >= 0.5)
            .Select(q => new Domain.Entities.ValidatedSkill(
                SkillName: "Software Engineering",
                ValidatedLevel: (int)Math.Round(q.EvaluatedScore!.Value * 5),
                Evidence: $"Layer {q.Layer} question scored {q.EvaluatedScore:F2}"))
            .ToList();

        var weakSignals = assessment.Questions
            .Where(q => q.EvaluatedScore.HasValue && q.EvaluatedScore.Value < 0.3)
            .Select(q => $"Low score ({q.EvaluatedScore:F2}) on Layer {q.Layer} question")
            .ToList();

        var suspectedInflations = assessment.Questions
            .Where(q => q.EvaluatedScore.HasValue && q.EvaluatedScore.Value < 0.4 && q.Layer > 1)
            .Select(q => $"Possible skill inflation detected at Layer {q.Layer}")
            .Distinct()
            .ToList();

        // LLM summary is optional and expensive — generate only when needed
        string? llmSummary = null;
        try
        {
            var kpiData = new Dictionary<string, double>
            {
                ["seniority_score"] = seniorityResult.Score,
                ["confidence"] = seniorityResult.Confidence
            };
            var insightRequest = new Interfaces.CareerInsightRequest(
                CurrentRole: "Software Engineer",
                YearsExperience: 0,
                ValidatedSkills: validatedSkills.Select(v => v.SkillName).Distinct().ToList(),
                MarketKpis: kpiData,
                RecommendedPaths: []);
            llmSummary = await _aiOrchestrator.GenerateCareerInsightAsync(insightRequest, cancellationToken);
        }
        catch
        {
            // Non-critical: summary is optional
        }

        var result = AssessmentResult.Create(
            assessmentId: assessment.Id,
            validatedSkills: validatedSkills,
            weakSignals: weakSignals,
            suspectedInflations: suspectedInflations,
            seniorityScore: seniorityResult.Score,
            confidence: seniorityResult.Confidence,
            llmSummary: llmSummary);

        assessment.Complete(seniorityResult.Level, seniorityResult.Confidence, result);

        if (assessment.JobApplicationId.HasValue)
        {
            try
            {
                await FinalizeJobApplicationScoringAsync(assessment, cancellationToken);
            }
            catch
            {
                // Non-critical — the job application keeps its pre-test estimate if this fails.
            }
        }
    }

    private async Task<string?> ResolveJobTargetSkillAsync(Domain.Entities.Assessment assessment, CancellationToken ct)
    {
        try
        {
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(assessment.JobApplicationId!.Value, ct);
            if (jobApplication is null) return null;

            var requiredSkills = JsonSerializer.Deserialize<List<JobRequiredSkill>>(jobApplication.RequiredSkillsJson) ?? [];
            var gapSkills = requiredSkills.Where(s => !s.MatchedToProfile).ToList();
            if (gapSkills.Count == 0) gapSkills = requiredSkills;
            if (gapSkills.Count == 0) return null;

            var index = assessment.Questions.Count % gapSkills.Count;
            return gapSkills[index].SkillName;
        }
        catch
        {
            // Non-critical — falls back to the profile-based skill selection.
            return null;
        }
    }

    // Blends the static profile/job skill overlap with the competency actually
    // demonstrated across the targeted assessment — the "real scoring" for this job.
    private async Task FinalizeJobApplicationScoringAsync(Domain.Entities.Assessment assessment, CancellationToken ct)
    {
        var jobApplication = await _jobApplicationRepository.GetByIdAsync(assessment.JobApplicationId!.Value, ct);
        if (jobApplication is null) return;

        var requiredSkills = JsonSerializer.Deserialize<List<JobRequiredSkill>>(jobApplication.RequiredSkillsJson) ?? [];
        var requiredCanonicals = requiredSkills.Select(s => s.SkillCanonical).ToList();

        var profile = await _userRepository.GetProfileByUserIdAsync(assessment.UserId, ct);
        var userSkillCanonicals = profile?.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var overlapCount = requiredCanonicals.Count(userSkillCanonicals.Contains);
        var overlapPct = requiredCanonicals.Count > 0 ? (double)overlapCount / requiredCanonicals.Count : 0.5;

        var testScores = assessment.Questions
            .Where(q => q.EvaluatedScore.HasValue)
            .Select(q => q.EvaluatedScore!.Value)
            .ToList();
        var testAvgScore = testScores.Count > 0 ? testScores.Average() : 0.0;

        var matchScorePct = Math.Round((overlapPct * 0.5 + testAvgScore * 0.5) * 100, 1);
        var gaps = _skillPrioritization.PrioritizeSkillList(requiredCanonicals, userSkillCanonicals);

        jobApplication.CompleteScoring(matchScorePct, JsonSerializer.Serialize(gaps));
        await _jobApplicationRepository.UpdateAsync(jobApplication, ct);
    }
}
