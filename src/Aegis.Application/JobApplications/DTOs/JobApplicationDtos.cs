using Aegis.Application.Assessment.DTOs;
using Aegis.Application.Recommendations.DTOs;

namespace Aegis.Application.JobApplications.DTOs;

// Property names deliberately mirror Domain.ValueObjects.JobRequiredSkill so the
// persisted JSON can be deserialized directly into this DTO.
public record RequiredSkillDto(string SkillName, string SkillCanonical, bool MatchedToProfile);

public record SubmitJobApplicationResponse(
    int JobApplicationId,
    string Status,
    double MatchScorePct,
    List<RequiredSkillDto> RequiredSkills);

public record JobApplicationSummaryDto(
    int Id,
    string JobTitle,
    string? CompanyName,
    string Status,
    DateTime SubmittedAt,
    double? MatchScorePct);

public record JobApplicationDetailResponse(
    int Id,
    string JobTitle,
    string? CompanyName,
    string Status,
    DateTime SubmittedAt,
    DateTime? AnalyzedAt,
    DateTime? ScoredAt,
    int? AssessmentId,
    double? MatchScorePct,
    List<RequiredSkillDto> RequiredSkills,
    List<PrioritizedSkillGapDto> SkillGaps);

public record StartJobAssessmentResponse(
    int AssessmentId,
    string Status,
    AssessmentQuestionDto FirstQuestion);
