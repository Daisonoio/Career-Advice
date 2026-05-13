using Aegis.Application.Assessment.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Assessment.Queries;

public record GetAssessmentResultQuery(int UserId, int AssessmentId) : IRequest<AssessmentResultResponse>;

public class GetAssessmentResultQueryHandler : IRequestHandler<GetAssessmentResultQuery, AssessmentResultResponse>
{
    private readonly IAssessmentRepository _assessmentRepository;

    public GetAssessmentResultQueryHandler(IAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    public async Task<AssessmentResultResponse> Handle(GetAssessmentResultQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(request.AssessmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        if (assessment.UserId != request.UserId)
            throw new UnauthorizedException("This assessment does not belong to the current user.");

        if (!assessment.IsComplete || assessment.Result is null)
            throw new ConflictException("Assessment is not yet completed.");

        var result = assessment.Result;

        return new AssessmentResultResponse(
            AssessmentId: assessment.Id,
            EstimatedSeniority: assessment.EstimatedSeniority?.ToString() ?? "Unknown",
            Confidence: assessment.Confidence ?? 0.0,
            ValidatedSkills: result.ValidatedSkills.Select(v => new ValidatedSkillDto(
                SkillName: v.SkillName,
                ValidatedLevel: v.ValidatedLevel,
                Evidence: v.Evidence)).ToList(),
            WeakSignals: result.WeakSignals,
            SuspectedInflations: result.SuspectedInflations,
            LlmSummary: result.LlmSummary,
            CompletedAt: assessment.CompletedAt!.Value);
    }
}
