namespace Aegis.Application.Assessment.DTOs;

public record AssessmentStartResponse(int AssessmentId, AssessmentQuestionDto FirstQuestion);

public record AssessmentQuestionDto(int QuestionId, string QuestionText, int Layer, int Difficulty);

public record AssessmentQuestionResponse(
    bool IsCompleted,
    AssessmentQuestionDto? NextQuestion,
    AssessmentResultSummary? Result);

public record AssessmentResultSummary(
    string EstimatedSeniority,
    double Confidence,
    int ValidatedSkillsCount);

public record AssessmentResultResponse(
    int AssessmentId,
    string EstimatedSeniority,
    double Confidence,
    List<ValidatedSkillDto> ValidatedSkills,
    List<string> WeakSignals,
    List<string> SuspectedInflations,
    string? LlmSummary,
    DateTime CompletedAt);

public record ValidatedSkillDto(string SkillName, int ValidatedLevel, string Evidence);

public record AssessmentSummaryResponse(
    int AssessmentId,
    string? EstimatedSeniority,
    double? Confidence,
    DateTime StartedAt,
    DateTime? CompletedAt,
    bool IsComplete);
