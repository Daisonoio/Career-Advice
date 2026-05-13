namespace Aegis.Domain.Entities;

public class UserSkill : Entity
{
    public int UserId { get; private set; }
    public int SkillId { get; private set; }
    public int SelfRatedLevel { get; private set; }   // 1–5
    public double? YearsExperience { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime? LastUsed { get; private set; }

    public Skill Skill { get; private set; } = default!;

    private UserSkill() { }

    public static UserSkill Create(int userId, int skillId, int level, double? yearsExp, bool isPrimary)
    {
        if (level is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(level), "Level must be 1–5");
        return new UserSkill
        {
            UserId = userId,
            SkillId = skillId,
            SelfRatedLevel = level,
            YearsExperience = yearsExp,
            IsPrimary = isPrimary,
            LastUsed = DateTime.UtcNow
        };
    }

    public void Update(int level, double? yearsExp, bool isPrimary)
    {
        if (level is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(level));
        SelfRatedLevel = level;
        YearsExperience = yearsExp;
        IsPrimary = isPrimary;
        Touch();
    }
}
