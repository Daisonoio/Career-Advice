namespace Aegis.Domain.Entities;

public class AssessmentQuestion : Entity
{
    public int AssessmentId { get; private set; }
    public string QuestionText { get; private set; } = default!;
    public string? AnswerText { get; private set; }
    public int? SkillId { get; private set; }
    public int DifficultyLevel { get; private set; }  // 1–5
    public int Layer { get; private set; }            // 1=screening, 2=deep_dive, 3=tradeoff
    public double? EvaluatedScore { get; private set; }
    public DateTime? AnsweredAt { get; private set; }

    private AssessmentQuestion() { }

    public static AssessmentQuestion Create(int assessmentId, string questionText, int? skillId, int difficulty, int layer)
        => new()
        {
            AssessmentId = assessmentId,
            QuestionText = questionText,
            SkillId = skillId,
            DifficultyLevel = difficulty,
            Layer = layer
        };

    public void SetAnswer(string answerText, double score)
    {
        AnswerText = answerText;
        EvaluatedScore = score;
        AnsweredAt = DateTime.UtcNow;
        Touch();
    }
}
