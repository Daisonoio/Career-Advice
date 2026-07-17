using Aegis.Application.Recommendations.Services;
using FluentAssertions;
using Xunit;

namespace Aegis.Application.Tests;

public class SkillPrioritizationServiceTests
{
    private readonly SkillPrioritizationService _sut = new();

    [Fact]
    public void PrioritizeSkillList_ReturnsGapsOnlyForMissingSkills()
    {
        var required = new List<string> { "csharp", "docker", "kubernetes" };
        var userSkills = new List<string> { "csharp" };

        var gaps = _sut.PrioritizeSkillList(required, userSkills);

        gaps.Should().HaveCount(2);
        gaps.Select(g => g.SkillCanonical).Should().BeEquivalentTo("docker", "kubernetes");
    }

    [Fact]
    public void PrioritizeSkillList_ReturnsEmpty_WhenUserHasAllRequiredSkills()
    {
        var required = new List<string> { "csharp", "docker" };
        var userSkills = new List<string> { "csharp", "docker" };

        var gaps = _sut.PrioritizeSkillList(required, userSkills);

        gaps.Should().BeEmpty();
    }

    [Fact]
    public void PrioritizeSkillList_MarksEarlySkillsAsFoundation()
    {
        var required = new List<string> { "csharp", "docker", "kubernetes", "terraform" };

        var gaps = _sut.PrioritizeSkillList(required, new List<string>(), foundationCount: 2);

        gaps.Where(g => g.SkillCanonical is "csharp" or "docker")
            .Should().OnlyContain(g => g.Type == "Foundation");
    }

    [Fact]
    public void Prioritize_ReturnsEmpty_ForUnknownRole()
    {
        var gaps = _sut.Prioritize("Nonexistent Role", new List<string>());

        gaps.Should().BeEmpty();
    }
}
