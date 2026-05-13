using Aegis.Application.Assessment.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Assessment.Commands;

public record StartAssessmentCommand(int UserId) : IRequest<AssessmentStartResponse>;

public class StartAssessmentCommandHandler : IRequestHandler<StartAssessmentCommand, AssessmentStartResponse>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAIOrchestrator _aiOrchestrator;

    public StartAssessmentCommandHandler(
        IAssessmentRepository assessmentRepository,
        IUserRepository userRepository,
        IAIOrchestrator aiOrchestrator)
    {
        _assessmentRepository = assessmentRepository;
        _userRepository = userRepository;
        _aiOrchestrator = aiOrchestrator;
    }

    public async Task<AssessmentStartResponse> Handle(StartAssessmentCommand request, CancellationToken cancellationToken)
    {
        // Check for active assessment
        var activeAssessment = await _assessmentRepository.GetActiveForUserAsync(request.UserId, cancellationToken);
        if (activeAssessment is not null)
            throw new ConflictException("User already has an active assessment in progress.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Rate limit for Free tier: max 1 assessment/month
        if (user.SubscriptionTier == SubscriptionTier.Free)
        {
            var history = await _assessmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var completedThisMonth = history.Count(a =>
                a.IsComplete &&
                a.CompletedAt.HasValue &&
                a.CompletedAt.Value.Year == DateTime.UtcNow.Year &&
                a.CompletedAt.Value.Month == DateTime.UtcNow.Month);

            if (completedThisMonth >= 1)
                throw new ConflictException("Free tier allows maximum 1 completed assessment per month.");
        }

        // Load profile to determine primary skills for question context
        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);
        var primarySkill = profile?.Skills
            .Where(s => s.IsPrimary)
            .Select(s => s.Skill?.Name ?? s.Skill?.CanonicalName)
            .FirstOrDefault(s => s != null)
            ?? profile?.Skills.FirstOrDefault()?.Skill?.Name
            ?? "Software Engineering";

        var assessment = Aegis.Domain.Entities.Assessment.Start(request.UserId);
        await _assessmentRepository.AddAsync(assessment, cancellationToken);

        // Generate first question (Layer 1, Difficulty 1)
        var ctx = new AssessmentQuestionContext(
            Skill: primarySkill!,
            Difficulty: 1,
            SeniorityTarget: "Mid",
            PreviousAnswers: []);

        var questionText = await _aiOrchestrator.GenerateAssessmentQuestionAsync(ctx, cancellationToken);
        assessment.AddQuestion(questionText, null, difficulty: 1, layer: 1);
        await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

        var firstQuestion = assessment.Questions.First();

        return new AssessmentStartResponse(
            AssessmentId: assessment.Id,
            FirstQuestion: new AssessmentQuestionDto(
                QuestionId: firstQuestion.Id,
                QuestionText: firstQuestion.QuestionText,
                Layer: firstQuestion.Layer,
                Difficulty: firstQuestion.DifficultyLevel));
    }
}
