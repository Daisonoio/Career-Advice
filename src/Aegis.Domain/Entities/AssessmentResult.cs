namespace Aegis.Domain.Entities;

public class AssessmentResult : Entity
{
    public int AssessmentId { get; private set; }
    public List<ValidatedSkill> ValidatedSkills { get; private set; } = [];
    public List<string> WeakSignals { get; private set; } = [];
    public List<string> SuspectedInflations { get; private set; } = [];
    public double SeniorityScore { get; private set; }
    public double OverallConfidence { get; private set; }
    public string? LlmSummary { get; private set; }   // sintetizzato da Claude su dati già calcolati

    private AssessmentResult() { }

    public static AssessmentResult Create(
        int assessmentId,
        List<ValidatedSkill> validatedSkills,
        List<string> weakSignals,
        List<string> suspectedInflations,
        double seniorityScore,
        double confidence,
        string? llmSummary)
        => new()
        {
            AssessmentId = assessmentId,
            ValidatedSkills = validatedSkills,
            WeakSignals = weakSignals,
            SuspectedInflations = suspectedInflations,
            SeniorityScore = seniorityScore,
            OverallConfidence = confidence,
            LlmSummary = llmSummary
        };
}

public record ValidatedSkill(string SkillName, int ValidatedLevel, string Evidence);
