namespace Aegis.Domain.ValueObjects;

// Represents a single skill gap within a career path recommendation,
// ordered by SkillRoi so the consumer always gets an actionable priority queue.
public record PrioritizedSkillGap(
    string SkillName,
    string SkillCanonical,
    string Type,               // Foundation | Differentiating | Premium | NiceToHave | Declining
    double SkillRoi,           // composite score 0–10
    double? SalaryPremiumPct,  // estimated salary uplift for acquiring this skill
    int    EstimatedWeeks,     // time to reach working competency
    string LearningDifficulty  // Easy | Medium | Hard | VeryHard
);
