using Aegis.Domain.ValueObjects;

namespace Aegis.Domain.Entities;

public class UserProfile : Entity
{
    public int UserId { get; private set; }
    public string? CurrentRole { get; private set; }
    public int? YearsExperience { get; private set; }
    public string? LocationCountry { get; private set; }
    public string? LocationCity { get; private set; }
    public string? EnglishLevel { get; private set; }
    public string? RemotePreference { get; private set; }
    public SalaryRange? SalaryExpectation { get; private set; }
    public string? CareerGoals { get; private set; }
    public double ProfileCompleteness { get; private set; }

    public IReadOnlyList<UserSkill> Skills => _skills.AsReadOnly();
    private readonly List<UserSkill> _skills = [];

    public User User { get; private set; } = default!;

    private UserProfile() { }

    public static UserProfile Create(int userId) => new() { UserId = userId };

    public void Update(
        string? currentRole,
        int? yearsExperience,
        string? locationCountry,
        string? locationCity,
        string? englishLevel,
        string? remotePreference,
        SalaryRange? salaryExpectation,
        string? careerGoals)
    {
        CurrentRole = currentRole;
        YearsExperience = yearsExperience;
        LocationCountry = locationCountry;
        LocationCity = locationCity;
        EnglishLevel = englishLevel;
        RemotePreference = remotePreference;
        SalaryExpectation = salaryExpectation;
        CareerGoals = careerGoals;
        ProfileCompleteness = CalculateCompleteness();
        Touch();
    }

    public void AddOrUpdateSkill(int skillId, int selfRatedLevel, double? yearsExp, bool isPrimary)
    {
        var existing = _skills.FirstOrDefault(s => s.SkillId == skillId);
        if (existing is not null)
            existing.Update(selfRatedLevel, yearsExp, isPrimary);
        else
            _skills.Add(UserSkill.Create(UserId, skillId, selfRatedLevel, yearsExp, isPrimary));

        Touch();
    }

    public void RemoveSkill(int skillId)
    {
        var skill = _skills.FirstOrDefault(s => s.SkillId == skillId);
        if (skill is not null) _skills.Remove(skill);
        Touch();
    }

    private double CalculateCompleteness()
    {
        var fields = new bool[]
        {
            !string.IsNullOrWhiteSpace(CurrentRole),
            YearsExperience.HasValue,
            !string.IsNullOrWhiteSpace(LocationCountry),
            !string.IsNullOrWhiteSpace(EnglishLevel),
            !string.IsNullOrWhiteSpace(RemotePreference),
            SalaryExpectation is not null,
            !string.IsNullOrWhiteSpace(CareerGoals),
            _skills.Count >= 3
        };
        return fields.Count(f => f) / (double)fields.Length;
    }
}
