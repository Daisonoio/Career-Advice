using Aegis.Domain.Enums;

namespace Aegis.Domain.Entities;

public class Assessment : Entity
{
    public int UserId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public SeniorityLevel? EstimatedSeniority { get; private set; }
    public double? Confidence { get; private set; }
    public int CurrentLayer { get; private set; } = 1;

    public IReadOnlyList<AssessmentQuestion> Questions => _questions.AsReadOnly();
    private readonly List<AssessmentQuestion> _questions = [];
    public AssessmentResult? Result { get; private set; }

    private Assessment() { }

    public static Assessment Start(int userId) => new() { UserId = userId };

    public void AddQuestion(string questionText, int? skillId, int difficulty, int layer)
        => _questions.Add(AssessmentQuestion.Create(Id, questionText, skillId, difficulty, layer));

    public void AnswerCurrentQuestion(int questionId, string answerText, double score)
    {
        var q = _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new InvalidOperationException("Question not found in this assessment.");
        q.SetAnswer(answerText, score);
        Touch();
    }

    public void AdvanceToLayer(int layer)
    {
        if (layer <= CurrentLayer) throw new InvalidOperationException("Cannot go back to a previous layer.");
        CurrentLayer = layer;
    }

    public void Complete(SeniorityLevel seniority, double confidence, AssessmentResult result)
    {
        EstimatedSeniority = seniority;
        Confidence = confidence;
        Result = result;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    public bool IsComplete => CompletedAt.HasValue;
}
