namespace Aegis.Domain.ValueObjects;

// A single skill extracted from a job posting's raw text, matched against the
// canonical skill catalogue. MatchedToProfile is computed once at submission
// time against the user's profile skills at that moment.
public record JobRequiredSkill(
    string SkillName,
    string SkillCanonical,
    bool MatchedToProfile
);
