using Aegis.Domain.Entities;
using System.Text.RegularExpressions;

namespace Aegis.Infrastructure.ExternalData.Esco;

public class EscoSkillMapper
{
    // Maps ESCO broader-concept titles (partial match) to AEGIS category IDs.
    // Order matters: more specific patterns first.
    private static readonly (string Pattern, int CategoryId)[] CategoryRules =
    [
        ("programming language",      1),
        ("scripting language",        1),
        ("markup language",           1),
        ("query language",            1),
        ("software framework",        2),
        ("web framework",             2),
        ("application framework",     2),
        ("library",                   2),
        ("cloud",                     3),
        ("container",                 3),
        ("virtualisation",            3),
        ("infrastructure as code",    4),
        ("continuous integration",    4),
        ("devops",                    4),
        ("monitoring",                4),
        ("logging",                   4),
        ("observability",             4),
        ("network",                   4),
        ("microservice",              5),
        ("service-oriented",          5),
        ("software architecture",     5),
        ("design pattern",            5),
        ("api",                       5),
        ("database",                  6),
        ("data storage",              6),
        ("data management",           6),
        ("message queue",             6),
        ("stream processing",         6),
        ("machine learning",          7),
        ("artificial intelligence",   7),
        ("deep learning",             7),
        ("natural language",          7),
        ("data science",              7),
        ("mlops",                     7),
        ("project management",        8),
        ("team management",           8),
        ("leadership",                8),
        ("agile",                     8),
        ("scrum",                     8),
    ];

    // Broad fallbacks by ESCO skillType
    private static readonly Dictionary<string, int> SkillTypeFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        ["knowledge"] = 5,  // Architecture is a reasonable default for generic knowledge
        ["skill"]     = 5,
        ["competence"] = 8,
    };

    public Skill Map(EscoSkillData data)
    {
        var canonical   = Canonicalize(data.TitleEn);
        var categoryId  = ResolveCategory(data.BroaderConceptTitle, data.SkillType);
        var confidence  = data.IsEssential ? 0.90 : 0.70;

        var aliases = new List<string>();
        if (data.TitleIt is not null && !string.Equals(data.TitleIt, data.TitleEn, StringComparison.OrdinalIgnoreCase))
            aliases.Add(data.TitleIt);

        return Skill.CreateFromSync(
            name:             data.TitleEn,
            nameIt:           data.TitleIt,
            canonicalName:    canonical,
            categoryId:       categoryId,
            aliases:          aliases,
            sourceType:       "ESCO",
            sourceExternalId: data.Uri,
            confidenceScore:  confidence);
    }

    // Converts a free-text skill name to a URL-safe kebab-case canonical name.
    // e.g. "use C# programming language" → "use-c-programming-language"
    public static string Canonicalize(string name)
    {
        // Lowercase
        var s = name.ToLowerInvariant();

        // Replace common symbols before stripping
        s = s.Replace("c#", "csharp")
             .Replace("f#", "fsharp")
             .Replace(".net", "dotnet")
             .Replace("c++", "cpp")
             .Replace("node.js", "nodejs")
             .Replace("asp.net", "aspnet");

        // Remove characters that are not alphanumeric or spaces/hyphens
        s = Regex.Replace(s, @"[^a-z0-9\s\-]", "");

        // Collapse whitespace and replace with hyphens
        s = Regex.Replace(s, @"\s+", "-");

        // Collapse consecutive hyphens
        s = Regex.Replace(s, @"-{2,}", "-");

        // Trim leading/trailing hyphens
        s = s.Trim('-');

        // Hard cap at 100 chars to fit the DB column
        return s.Length > 100 ? s[..100].TrimEnd('-') : s;
    }

    private static int? ResolveCategory(string? broaderTitle, string skillType)
    {
        if (broaderTitle is not null)
        {
            var lower = broaderTitle.ToLowerInvariant();
            foreach (var (pattern, catId) in CategoryRules)
            {
                if (lower.Contains(pattern))
                    return catId;
            }
        }

        if (SkillTypeFallback.TryGetValue(skillType, out var fallback))
            return fallback;

        return null; // Uncategorised — will appear in search but without category filter
    }
}
