using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Aegis.Domain.Tests;

public class JobApplicationTests
{
    private static JobApplication CreateSubmitted()
        => JobApplication.Submit(userId: 1, jobTitle: "Backend Engineer", companyName: "Acme", rawDescription: "C# and PostgreSQL required.");

    [Fact]
    public void Submit_SetsInitialStateToSubmitted()
    {
        var jobApplication = CreateSubmitted();

        jobApplication.Status.Should().Be(JobApplicationStatus.Submitted);
        jobApplication.UserId.Should().Be(1);
        jobApplication.RequiredSkillsJson.Should().Be("[]");
        jobApplication.MatchScorePct.Should().BeNull();
    }

    [Fact]
    public void SetExtractedSkills_TransitionsToAnalyzed()
    {
        var jobApplication = CreateSubmitted();

        jobApplication.SetExtractedSkills("[]", initialMatchScorePct: 50.0);

        jobApplication.Status.Should().Be(JobApplicationStatus.Analyzed);
        jobApplication.MatchScorePct.Should().Be(50.0);
        jobApplication.AnalyzedAt.Should().NotBeNull();
    }

    [Fact]
    public void LinkAssessment_Throws_WhenNotYetAnalyzed()
    {
        var jobApplication = CreateSubmitted();

        var act = () => jobApplication.LinkAssessment(assessmentId: 42);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LinkAssessment_TransitionsToTestInProgress_WhenAnalyzed()
    {
        var jobApplication = CreateSubmitted();
        jobApplication.SetExtractedSkills("[]", 0.0);

        jobApplication.LinkAssessment(assessmentId: 42);

        jobApplication.Status.Should().Be(JobApplicationStatus.TestInProgress);
        jobApplication.AssessmentId.Should().Be(42);
    }

    [Fact]
    public void RevertTestInProgress_RevertsToAnalyzed_AndClearsAssessmentId()
    {
        var jobApplication = CreateSubmitted();
        jobApplication.SetExtractedSkills("[]", 0.0);
        jobApplication.LinkAssessment(42);

        jobApplication.RevertTestInProgress();

        jobApplication.Status.Should().Be(JobApplicationStatus.Analyzed);
        jobApplication.AssessmentId.Should().BeNull();
    }

    [Fact]
    public void RevertTestInProgress_IsNoOp_WhenNotInProgress()
    {
        var jobApplication = CreateSubmitted();
        jobApplication.SetExtractedSkills("[]", 0.0);

        jobApplication.RevertTestInProgress();

        jobApplication.Status.Should().Be(JobApplicationStatus.Analyzed);
    }

    [Fact]
    public void CompleteScoring_TransitionsToCompleted()
    {
        var jobApplication = CreateSubmitted();
        jobApplication.SetExtractedSkills("[]", 0.0);
        jobApplication.LinkAssessment(42);

        jobApplication.CompleteScoring(75.5, "[]");

        jobApplication.Status.Should().Be(JobApplicationStatus.Completed);
        jobApplication.MatchScorePct.Should().Be(75.5);
        jobApplication.ScoredAt.Should().NotBeNull();
    }
}
