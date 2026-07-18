using Aegis.Application.JobApplications.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.JobApplications.Queries;

public record GetJobApplicationHistoryQuery(int UserId) : IRequest<List<JobApplicationSummaryDto>>;

public class GetJobApplicationHistoryQueryHandler
    : IRequestHandler<GetJobApplicationHistoryQuery, List<JobApplicationSummaryDto>>
{
    private readonly IJobApplicationRepository _jobApplicationRepository;

    public GetJobApplicationHistoryQueryHandler(IJobApplicationRepository jobApplicationRepository)
    {
        _jobApplicationRepository = jobApplicationRepository;
    }

    public async Task<List<JobApplicationSummaryDto>> Handle(GetJobApplicationHistoryQuery request, CancellationToken cancellationToken)
    {
        var applications = await _jobApplicationRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return applications
            .Select(a => new JobApplicationSummaryDto(
                a.Id, a.JobTitle, a.CompanyName, a.Status.ToString(), a.CreatedAt, a.MatchScorePct))
            .ToList();
    }
}
