using Aegis.Domain.Entities;

namespace Aegis.Application.Assessment.Services;

/// <summary>
/// Determines the next step in an adaptive assessment.
/// Fully deterministic — no LLM involvement.
/// </summary>
public class AdaptiveInterviewService
{
    // Layer configuration
    private const int Layer1QuestionCount = 4;
    private const int Layer2QuestionCount = 5;
    private const int Layer3QuestionCount = 3;
    private const double Layer1PassThreshold = 0.4;
    private const double Layer2PassThreshold = 0.5;
    private const int MaxTotalQuestions = 12;

    /// <summary>
    /// Determines the next step based on the current state of the assessment.
    /// </summary>
    public NextStep DetermineNextStep(Aegis.Domain.Entities.Assessment assessment)
    {
        var questions = assessment.Questions.Where(q => q.EvaluatedScore.HasValue).ToList();
        var totalAnswered = questions.Count;

        if (totalAnswered >= MaxTotalQuestions)
            return new NextStep(ShouldComplete: true, NextLayer: assessment.CurrentLayer, NextDifficulty: 3, TargetSkill: null);

        var layer1Questions = questions.Where(q => q.Layer == 1).ToList();
        var layer2Questions = questions.Where(q => q.Layer == 2).ToList();
        var layer3Questions = questions.Where(q => q.Layer == 3).ToList();

        return assessment.CurrentLayer switch
        {
            1 when layer1Questions.Count < Layer1QuestionCount =>
                // Still in Layer 1, need more questions
                new NextStep(ShouldComplete: false, NextLayer: 1, NextDifficulty: DifficultyForLayer1(layer1Questions.Count), TargetSkill: null),

            1 when layer1Questions.Count >= Layer1QuestionCount =>
                // Layer 1 done — check pass threshold
                EvaluateLayer1Transition(layer1Questions),

            2 when layer2Questions.Count < Layer2QuestionCount =>
                // Still in Layer 2, need more questions
                new NextStep(ShouldComplete: false, NextLayer: 2, NextDifficulty: DifficultyForLayer2(layer2Questions.Count), TargetSkill: null),

            2 when layer2Questions.Count >= Layer2QuestionCount =>
                // Layer 2 done — check pass threshold
                EvaluateLayer2Transition(layer2Questions),

            3 when layer3Questions.Count < Layer3QuestionCount =>
                // Still in Layer 3, need more questions
                new NextStep(ShouldComplete: false, NextLayer: 3, NextDifficulty: 5, TargetSkill: null),

            3 when layer3Questions.Count >= Layer3QuestionCount =>
                // Layer 3 complete — always finish
                new NextStep(ShouldComplete: true, NextLayer: 3, NextDifficulty: 5, TargetSkill: null),

            _ => new NextStep(ShouldComplete: true, NextLayer: assessment.CurrentLayer, NextDifficulty: 3, TargetSkill: null)
        };
    }

    private static NextStep EvaluateLayer1Transition(List<AssessmentQuestion> layer1Questions)
    {
        var avgScore = layer1Questions.Average(q => q.EvaluatedScore!.Value);
        return avgScore >= Layer1PassThreshold
            ? new NextStep(ShouldComplete: false, NextLayer: 2, NextDifficulty: 2, TargetSkill: null)
            : new NextStep(ShouldComplete: true, NextLayer: 1, NextDifficulty: 1, TargetSkill: null);
    }

    private static NextStep EvaluateLayer2Transition(List<AssessmentQuestion> layer2Questions)
    {
        var avgScore = layer2Questions.Average(q => q.EvaluatedScore!.Value);
        return avgScore >= Layer2PassThreshold
            ? new NextStep(ShouldComplete: false, NextLayer: 3, NextDifficulty: 4, TargetSkill: null)
            : new NextStep(ShouldComplete: true, NextLayer: 2, NextDifficulty: 3, TargetSkill: null);
    }

    private static int DifficultyForLayer1(int questionIndex)
        => questionIndex switch
        {
            0 => 1,
            1 => 2,
            2 => 2,
            _ => 3
        };

    private static int DifficultyForLayer2(int questionIndex)
        => questionIndex switch
        {
            0 => 3,
            1 => 3,
            2 => 4,
            3 => 4,
            _ => 4
        };
}

public record NextStep(bool ShouldComplete, int NextLayer, int NextDifficulty, string? TargetSkill);
