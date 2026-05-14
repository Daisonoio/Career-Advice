using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Assessment.Commands;

public record AbandonAssessmentCommand(int UserId, int AssessmentId) : IRequest;

public class AbandonAssessmentCommandHandler : IRequestHandler<AbandonAssessmentCommand>
{
    private readonly IAssessmentRepository _assessmentRepository;

    public AbandonAssessmentCommandHandler(IAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    public async Task Handle(AbandonAssessmentCommand request, CancellationToken cancellationToken)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        if (assessment.UserId != request.UserId)
            throw new UnauthorizedException("This assessment does not belong to the current user.");

        assessment.Abandon();
        await _assessmentRepository.UpdateAsync(assessment, cancellationToken);
    }
}
