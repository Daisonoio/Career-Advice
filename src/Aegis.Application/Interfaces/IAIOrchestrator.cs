namespace Aegis.Application.Interfaces;

public interface IAIOrchestrator
{
    /// <summary>Generates one technical question for the adaptive assessment. Never generates KPI data.</summary>
    Task<string> GenerateAssessmentQuestionAsync(AssessmentQuestionContext ctx, CancellationToken ct = default);

    /// <summary>Evaluates a technical answer. Returns a score and signals. Never invents market data.</summary>
    Task<AnswerEvaluation> EvaluateAnswerAsync(string question, string answer, string skill, int difficulty, CancellationToken ct = default);

    /// <summary>Generates a narrative career insight. All KPIs must be pre-computed and passed in.</summary>
    Task<string> GenerateCareerInsightAsync(CareerInsightRequest request, CancellationToken ct = default);

    /// <summary>Explains a specific recommendation using pre-computed KPIs passed as input.</summary>
    Task<string> ExplainRecommendationAsync(RecommendationExplainContext ctx, CancellationToken ct = default);

    /// <summary>Extracts the technical skill labels required by a job posting. Text labelling only — never returns market numbers.</summary>
    Task<List<string>> ExtractRequiredSkillsAsync(string jobTitle, string jobDescription, CancellationToken ct = default);
}

public record AssessmentQuestionContext(
    string Skill,
    int Difficulty,
    string SeniorityTarget,
    List<(string Question, string Answer)> PreviousAnswers);

public record AnswerEvaluation(
    double Score,
    string Depth,
    List<string> BluffSignals,
    List<string> StrongSignals,
    List<string> WeakSignals);

public record CareerInsightRequest(
    string CurrentRole,
    int YearsExperience,
    List<string> ValidatedSkills,
    Dictionary<string, double> MarketKpis,
    List<string> RecommendedPaths);

public record RecommendationExplainContext(
    string TargetRole,
    double SalaryUpliftPct,
    string TransitionDifficulty,
    Dictionary<string, double> MarketKpis,
    string CurrentRole);
