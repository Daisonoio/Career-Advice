using Aegis.Application.Assessment.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Assessment.Queries;

public record GetAssessmentHistoryQuery(int UserId) : IRequest<List<AssessmentSummaryResponse>>;

public class GetAssessmentHistoryQueryHandler : IRequestHandler<GetAssessmentHistoryQuery, List<AssessmentSummaryResponse>>
{
    private readonly IAssessmentRepository _assessmentRepository;

    public GetAssessmentHistoryQueryHandler(IAssessmentRepository assessmentRepository)
    {
        _assessmentRepository = assessmentRepository;
    }

    public async Task<List<AssessmentSummaryResponse>> Handle(GetAssessmentHistoryQuery request, CancellationToken cancellationToken)
    {
        var assessments = await _assessmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return assessments
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssessmentSummaryResponse(
                AssessmentId: a.Id,
                EstimatedSeniority: a.EstimatedSeniority?.ToString(),
                Confidence: a.Confidence,
                StartedAt: a.CreatedAt,
                CompletedAt: a.CompletedAt,
                IsComplete: a.IsComplete))
            .ToList();
    }
}
