using Aegis.Application.Assessment.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Application.JobApplications.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Interfaces;
using Aegis.Domain.ValueObjects;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.JobApplications.Commands;

public record StartJobApplicationAssessmentCommand(int UserId, int JobApplicationId) : IRequest<StartJobAssessmentResponse>;

public class StartJobApplicationAssessmentCommandHandler
    : IRequestHandler<StartJobApplicationAssessmentCommand, StartJobAssessmentResponse>
{
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAIOrchestrator _aiOrchestrator;

    public StartJobApplicationAssessmentCommandHandler(
        IJobApplicationRepository jobApplicationRepository,
        IAssessmentRepository assessmentRepository,
        IUserRepository userRepository,
        IAIOrchestrator aiOrchestrator)
    {
        _jobApplicationRepository = jobApplicationRepository;
        _assessmentRepository = assessmentRepository;
        _userRepository = userRepository;
        _aiOrchestrator = aiOrchestrator;
    }

    public async Task<StartJobAssessmentResponse> Handle(StartJobApplicationAssessmentCommand request, CancellationToken cancellationToken)
    {
        var jobApplication = await _jobApplicationRepository.GetByIdAsync(request.JobApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(JobApplication), request.JobApplicationId);

        if (jobApplication.UserId != request.UserId)
            throw new UnauthorizedException("This job application does not belong to the current user.");

        if (jobApplication.Status != JobApplicationStatus.Analyzed)
            throw new ConflictException("Job application must be analyzed, and not already have a test in progress, before starting the custom test.");

        var activeAssessment = await _assessmentRepository.GetActiveForUserAsync(request.UserId, cancellationToken);
        if (activeAssessment is not null)
            throw new ConflictException("User already has an active assessment in progress.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

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

        var requiredSkills = JsonSerializer.Deserialize<List<JobRequiredSkill>>(jobApplication.RequiredSkillsJson) ?? [];
        var topGapSkill = requiredSkills.FirstOrDefault(s => !s.MatchedToProfile)?.SkillName
            ?? requiredSkills.FirstOrDefault()?.SkillName
            ?? jobApplication.JobTitle;

        var assessment = Assessment.Start(request.UserId, jobApplication.Id);
        await _assessmentRepository.AddAsync(assessment, cancellationToken);

        var ctx = new AssessmentQuestionContext(
            Skill: topGapSkill,
            Difficulty: 1,
            SeniorityTarget: "Mid",
            PreviousAnswers: []);

        var questionText = await _aiOrchestrator.GenerateAssessmentQuestionAsync(ctx, cancellationToken);
        assessment.AddQuestion(questionText, null, difficulty: 1, layer: 1);
        await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

        jobApplication.LinkAssessment(assessment.Id);
        await _jobApplicationRepository.UpdateAsync(jobApplication, cancellationToken);

        var firstQuestion = assessment.Questions.First();

        return new StartJobAssessmentResponse(
            AssessmentId: assessment.Id,
            Status: jobApplication.Status.ToString(),
            FirstQuestion: new AssessmentQuestionDto(
                QuestionId: firstQuestion.Id,
                QuestionText: firstQuestion.QuestionText,
                Layer: firstQuestion.Layer,
                Difficulty: firstQuestion.DifficultyLevel));
    }
}
