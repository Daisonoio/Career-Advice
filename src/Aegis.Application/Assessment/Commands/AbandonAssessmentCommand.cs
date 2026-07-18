using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Assessment.Commands;

public record AbandonAssessmentCommand(int UserId, int AssessmentId) : IRequest;

public class AbandonAssessmentCommandHandler : IRequestHandler<AbandonAssessmentCommand>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;

    public AbandonAssessmentCommandHandler(
        IAssessmentRepository assessmentRepository,
        IJobApplicationRepository jobApplicationRepository)
    {
        _assessmentRepository = assessmentRepository;
        _jobApplicationRepository = jobApplicationRepository;
    }

    public async Task Handle(AbandonAssessmentCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        if (assessment.UserId != request.UserId)
            throw new UnauthorizedException("This assessment does not belong to the current user.");

        assessment.Abandon();
        await _assessmentRepository.UpdateAsync(assessment, cancellationToken);

        // Un-stick the linked job application so the user can start a fresh test for it.
        if (assessment.JobApplicationId.HasValue)
        {
            var jobApplication = await _jobApplicationRepository.GetByIdAsync(assessment.JobApplicationId.Value, cancellationToken);
            if (jobApplication is not null)
            {
                jobApplication.RevertTestInProgress();
                await _jobApplicationRepository.UpdateAsync(jobApplication, cancellationToken);
            }
        }
    }
}
