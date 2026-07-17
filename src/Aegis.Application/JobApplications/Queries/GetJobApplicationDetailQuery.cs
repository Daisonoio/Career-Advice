using Aegis.Application.Common.Exceptions;
using Aegis.Application.JobApplications.DTOs;
using Aegis.Application.Recommendations.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.JobApplications.Queries;

public record GetJobApplicationDetailQuery(int UserId, int JobApplicationId) : IRequest<JobApplicationDetailResponse>;

public class GetJobApplicationDetailQueryHandler
    : IRequestHandler<GetJobApplicationDetailQuery, JobApplicationDetailResponse>
{
    private readonly IJobApplicationRepository _jobApplicationRepository;

    public GetJobApplicationDetailQueryHandler(IJobApplicationRepository jobApplicationRepository)
    {
        _jobApplicationRepository = jobApplicationRepository;
    }

    public async Task<JobApplicationDetailResponse> Handle(GetJobApplicationDetailQuery request, CancellationToken cancellationToken)
    {
        var application = await _jobApplicationRepository.GetByIdAsync(request.JobApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(JobApplication), request.JobApplicationId);

        if (application.UserId != request.UserId)
            throw new UnauthorizedException("This job application does not belong to the current user.");

        var requiredSkills = DeserializeOrEmpty<RequiredSkillDto>(application.RequiredSkillsJson);
        var gaps = application.GapSummaryJson is not null
            ? DeserializeOrEmpty<PrioritizedSkillGapDto>(application.GapSummaryJson)
            : [];

        return new JobApplicationDetailResponse(
            Id: application.Id,
            JobTitle: application.JobTitle,
            CompanyName: application.CompanyName,
            Status: application.Status.ToString(),
            SubmittedAt: application.CreatedAt,
            AnalyzedAt: application.AnalyzedAt,
            ScoredAt: application.ScoredAt,
            AssessmentId: application.AssessmentId,
            MatchScorePct: application.MatchScorePct,
            RequiredSkills: requiredSkills,
            SkillGaps: gaps);
    }

    private static List<T> DeserializeOrEmpty<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
