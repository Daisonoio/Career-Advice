using Aegis.Domain.Enums;

namespace Aegis.Domain.Entities;

// A job posting the user submits for analysis: required skills are extracted from
// the raw text, compared against the user's profile, and — once the user completes
// a targeted assessment — scored for real against demonstrated competency.
public class JobApplication : Entity
{
    public int UserId { get; private set; }
    public string JobTitle { get; private set; } = default!;
    public string? CompanyName { get; private set; }
    public string RawDescription { get; private set; } = default!;
    public JobApplicationStatus Status { get; private set; }
    public string RequiredSkillsJson { get; private set; } = "[]";
    public double? MatchScorePct { get; private set; }
    public string? GapSummaryJson { get; private set; }
    public int? AssessmentId { get; private set; }
    public DateTime? AnalyzedAt { get; private set; }
    public DateTime? ScoredAt { get; private set; }

    private JobApplication() { }

    public static JobApplication Submit(int userId, string jobTitle, string? companyName, string rawDescription)
        => new()
        {
            UserId = userId,
            JobTitle = jobTitle,
            CompanyName = companyName,
            RawDescription = rawDescription,
            Status = JobApplicationStatus.Submitted
        };

    // Persists the skills extracted from the posting and the profile-only match
    // estimate, computed deterministically before any test is taken.
    public void SetExtractedSkills(string requiredSkillsJson, double initialMatchScorePct)
    {
        RequiredSkillsJson = requiredSkillsJson;
        MatchScorePct = initialMatchScorePct;
        Status = JobApplicationStatus.Analyzed;
        AnalyzedAt = DateTime.UtcNow;
        Touch();
    }

    public void LinkAssessment(int assessmentId)
    {
        if (Status != JobApplicationStatus.Analyzed)
            throw new InvalidOperationException("Job application must be analyzed before starting a targeted test.");

        AssessmentId = assessmentId;
        Status = JobApplicationStatus.TestInProgress;
        Touch();
    }

    // Final real score: blends the static profile overlap with the competency
    // actually demonstrated in the targeted assessment.
    public void CompleteScoring(double matchScorePct, string gapSummaryJson)
    {
        MatchScorePct = matchScorePct;
        GapSummaryJson = gapSummaryJson;
        Status = JobApplicationStatus.Completed;
        ScoredAt = DateTime.UtcNow;
        Touch();
    }

    // Called when the linked assessment is abandoned before completion, so the
    // job application isn't stuck in TestInProgress forever with no way to retry.
    public void RevertTestInProgress()
    {
        if (Status != JobApplicationStatus.TestInProgress) return;

        AssessmentId = null;
        Status = JobApplicationStatus.Analyzed;
        Touch();
    }
}
