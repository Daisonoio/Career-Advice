using Aegis.Domain.ValueObjects;

namespace Aegis.Application.Recommendations.Services;

// Deterministic service — zero LLM calls.
// Computes SkillROI and classifies every gap skill for a target role.
//
// SkillROI formula (FA-002):
//   (salary_premium × 0.35) + (demand × 0.25) + (growth × 0.20)
//   - (learning_difficulty × 0.10) - (ai_risk × 0.10)
//
// When per-skill MarketKpi data is not yet available (SkillId not populated),
// the service falls back to the static SkillMetadata table below.
// Once real per-skill KPIs are flowing, callers pass them in and the static
// values are no longer used.
public class SkillPrioritizationService
{
    // Static baseline metadata for all canonical skills in the catalogue.
    // Values are conservative EU-market estimates for the POC phase.
    // Columns: (SalaryPremiumPct, DemandScore, GrowthMomentum, LearningDifficulty 1-4, EstimatedWeeks, AIRisk 0-10)
    private static readonly Dictionary<string, SkillMeta> SkillMetadata =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Languages
        ["csharp"]              = new(12.0,  8.0, 6.0, 2, 12, 2.5),
        ["java"]                = new(10.0,  8.5, 5.0, 2, 12, 2.5),
        ["python"]              = new(14.0,  9.2, 8.5, 1,  8, 3.0),
        ["typescript"]          = new(11.0,  8.8, 7.5, 2, 10, 2.0),
        ["go"]                  = new(16.0,  7.5, 8.0, 3, 14, 2.0),
        ["rust"]                = new(18.0,  5.5, 9.0, 4, 24, 1.5),
        // Frameworks
        ["dotnet-aspnet-core"]  = new(11.0,  7.8, 6.5, 2, 10, 2.5),
        ["spring-boot"]         = new( 9.0,  7.5, 5.5, 2, 10, 2.5),
        ["fastapi"]             = new(10.0,  6.8, 7.5, 1,  6, 2.5),
        ["react"]               = new( 9.0,  8.5, 6.0, 2,  8, 2.0),
        ["nodejs"]              = new( 8.0,  7.8, 6.0, 2,  8, 2.5),
        // Cloud
        ["aws"]                 = new(18.0,  9.0, 8.0, 3, 20, 2.0),
        ["azure"]               = new(16.0,  8.5, 7.5, 3, 18, 2.0),
        ["gcp"]                 = new(17.0,  7.5, 8.5, 3, 18, 2.0),
        ["kubernetes"]          = new(19.0,  8.8, 8.5, 3, 16, 2.0),
        ["docker"]              = new(12.0,  9.0, 7.0, 2, 10, 2.0),
        // DevOps
        ["cicd"]                = new(10.0,  8.5, 7.0, 2, 10, 2.0),
        ["terraform"]           = new(17.0,  8.2, 8.5, 3, 14, 2.0),
        ["ansible"]             = new(11.0,  6.8, 5.5, 2, 12, 2.0),
        ["prometheus-grafana"]  = new(12.0,  7.5, 7.5, 2, 10, 2.0),
        ["elk-stack"]           = new(10.0,  6.5, 6.0, 2, 12, 2.0),
        ["opentelemetry"]       = new(13.0,  7.0, 9.0, 2,  8, 2.0),
        // Architecture
        ["microservices"]       = new(14.0,  8.0, 7.0, 3, 18, 2.5),
        ["event-driven"]        = new(15.0,  7.5, 7.5, 3, 16, 2.5),
        ["cqrs"]                = new(12.0,  6.5, 6.5, 3, 14, 2.5),
        ["ddd"]                 = new(11.0,  6.0, 6.0, 3, 16, 2.5),
        ["rest-apis"]           = new( 6.0,  9.0, 4.0, 1,  6, 3.0),
        ["grpc"]                = new(13.0,  6.5, 7.5, 2, 10, 2.0),
        // Data
        ["postgresql"]          = new(10.0,  8.5, 6.5, 2, 10, 2.5),
        ["sql-server"]          = new( 8.0,  7.0, 4.5, 2, 10, 3.0),
        ["mongodb"]             = new( 9.0,  7.0, 5.5, 2,  8, 2.5),
        ["redis"]               = new(10.0,  7.5, 6.5, 1,  6, 2.5),
        ["elasticsearch"]       = new(12.0,  6.5, 6.0, 2, 12, 2.5),
        ["apache-kafka"]        = new(15.0,  7.8, 8.0, 3, 14, 2.5),
        // AI/ML
        ["machine-learning"]    = new(22.0,  9.0, 9.5, 4, 28, 1.5),
        ["llm-integration"]     = new(24.0,  8.5, 9.8, 3, 14, 1.5),
        ["mlops"]               = new(20.0,  7.5, 9.0, 3, 18, 1.5),
        ["tensorflow-pytorch"]  = new(18.0,  7.0, 8.5, 4, 24, 1.5),
        // Leadership
        ["team-leadership"]     = new(20.0,  7.0, 6.0, 3, 24, 1.0),
        ["system-design"]       = new(18.0,  8.0, 7.5, 3, 20, 1.5),
        ["code-review"]         = new( 8.0,  7.5, 5.5, 1,  4, 1.5),
        ["mentoring"]           = new(10.0,  6.5, 6.0, 2, 12, 1.0),
    };

    // Role → ordered list of canonical skill names.
    // First skills in the list are Foundation; the rest are Differentiating or Premium.
    // This dictionary is the single source of truth for skill requirements across
    // GenerateRecommendationCommand and ProfileEnrichmentService.
    public static readonly Dictionary<string, List<string>> RoleRequiredSkills =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Backend Engineer"] =
        [
            "csharp", "dotnet-aspnet-core", "rest-apis", "postgresql", "docker",
            "cicd", "redis", "microservices", "grpc", "apache-kafka"
        ],
        ["Backend Developer"] =
        [
            "csharp", "dotnet-aspnet-core", "rest-apis", "postgresql", "docker",
            "cicd", "redis", "microservices", "grpc", "apache-kafka"
        ],
        ["Platform Engineer"] =
        [
            "kubernetes", "docker", "terraform", "aws", "cicd",
            "prometheus-grafana", "ansible", "opentelemetry", "azure", "elk-stack"
        ],
        ["Software Architect"] =
        [
            "microservices", "event-driven", "ddd", "cqrs", "system-design",
            "rest-apis", "grpc", "docker", "kubernetes", "apache-kafka"
        ],
        ["DevOps Engineer"] =
        [
            "kubernetes", "terraform", "cicd", "prometheus-grafana", "ansible",
            "docker", "aws", "elk-stack", "opentelemetry", "azure"
        ],
        ["AI/ML Engineer"] =
        [
            "machine-learning", "python", "llm-integration", "mlops", "tensorflow-pytorch",
            "docker", "kubernetes", "apache-kafka", "postgresql", "fastapi"
        ],
        ["Tech Lead"] =
        [
            "team-leadership", "system-design", "code-review", "mentoring", "microservices",
            "cqrs", "event-driven", "ddd", "cicd", "docker"
        ],
        ["Data Engineer"] =
        [
            "python", "apache-kafka", "postgresql", "elasticsearch", "docker",
            "kubernetes", "terraform", "aws", "mongodb", "redis"
        ],
        ["Full Stack Developer"] =
        [
            "csharp", "dotnet-aspnet-core", "typescript", "react", "rest-apis",
            "postgresql", "docker", "redis", "cicd", "nodejs"
        ],
        ["Frontend Engineer"] =
        [
            "typescript", "react", "nodejs", "rest-apis", "cicd",
            "docker", "elasticsearch", "grpc", "redis", "opentelemetry"
        ],
    };

    // Number of skills at the start of each role's list classified as Foundation.
    // The rest are Differentiating (middle) or Premium (last 2).
    private static readonly Dictionary<string, int> FoundationCount =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Backend Engineer"]    = 4,
        ["Backend Developer"]   = 4,
        ["Platform Engineer"]   = 4,
        ["Software Architect"]  = 4,
        ["DevOps Engineer"]     = 4,
        ["AI/ML Engineer"]      = 4,
        ["Tech Lead"]           = 3,
        ["Data Engineer"]       = 4,
        ["Full Stack Developer"]= 4,
        ["Frontend Engineer"]   = 4,
    };

    private const int PremiumTailCount = 2;

    public List<PrioritizedSkillGap> Prioritize(
        string targetRole,
        IEnumerable<string> userSkillCanonicals,
        double roleDemandScore    = 7.0,
        double roleGrowthMomentum = 7.0,
        double roleAiRisk         = 3.0)
    {
        if (!RoleRequiredSkills.TryGetValue(targetRole, out var required))
            return [];

        var userSet = userSkillCanonicals
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var foundationLimit = FoundationCount.GetValueOrDefault(targetRole, 4);
        var premiumStart    = required.Count - PremiumTailCount;
        var gaps            = new List<PrioritizedSkillGap>();

        for (var i = 0; i < required.Count; i++)
        {
            var canonical = required[i];
            if (userSet.Contains(canonical)) continue;

            var type = i < foundationLimit             ? "Foundation"
                     : i >= premiumStart               ? "Premium"
                                                       : "Differentiating";

            var roi = ComputeRoi(canonical, roleDemandScore, roleGrowthMomentum, roleAiRisk);
            var meta = GetMeta(canonical);

            gaps.Add(new PrioritizedSkillGap(
                SkillName:        ToDisplayName(canonical),
                SkillCanonical:   canonical,
                Type:             type,
                SkillRoi:         roi,
                SalaryPremiumPct: meta.SalaryPremiumPct,
                EstimatedWeeks:   meta.EstimatedWeeks,
                LearningDifficulty: DifficultyLabel(meta.LearningDifficulty)));
        }

        // Foundation first (non-negotiable), then by ROI desc within each tier
        return gaps
            .OrderBy(g  => g.Type == "Foundation" ? 0 : g.Type == "Differentiating" ? 1 : 2)
            .ThenByDescending(g => g.SkillRoi)
            .ToList();
    }

    private double ComputeRoi(
        string canonical,
        double roleDemandScore,
        double roleGrowth,
        double roleAiRisk)
    {
        var meta = GetMeta(canonical);

        // Use per-skill values where available; fall back to role-level signal
        var demand  = meta.DemandScore  > 0 ? meta.DemandScore  : roleDemandScore;
        var growth  = meta.GrowthMomentum > 0 ? meta.GrowthMomentum : roleGrowth;
        var aiRisk  = meta.AiRisk       > 0 ? meta.AiRisk       : roleAiRisk;

        var roi = (meta.SalaryPremiumPct / 10.0 * 0.35)  // normalise salary to 0-10
                + (demand / 10.0               * 0.25)
                + (growth / 10.0               * 0.20)
                - (meta.LearningDifficulty / 4.0 * 0.10)
                - (aiRisk / 10.0               * 0.10);

        return Math.Round(Math.Clamp(roi * 10.0, 0.0, 10.0), 2);
    }

    private static SkillMeta GetMeta(string canonical)
        => SkillMetadata.TryGetValue(canonical, out var m) ? m : SkillMeta.Default;

    private static string ToDisplayName(string canonical)
    {
        // Convert kebab-case canonical back to a readable display name for skills
        // not found in the metadata table (dynamically synced ESCO skills).
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"]             = "C#",
            ["dotnet-aspnet-core"] = ".NET / ASP.NET Core",
            ["rest-apis"]          = "REST APIs",
            ["apache-kafka"]       = "Apache Kafka",
            ["elk-stack"]          = "ELK Stack",
            ["cicd"]               = "CI/CD",
            ["ddd"]                = "Domain-Driven Design",
            ["cqrs"]               = "CQRS",
            ["grpc"]               = "gRPC",
            ["llm-integration"]    = "LLM Integration",
            ["tensorflow-pytorch"] = "TensorFlow / PyTorch",
            ["prometheus-grafana"] = "Prometheus / Grafana",
        };

        if (known.TryGetValue(canonical, out var name)) return name;

        // Capitalise each kebab segment
        return string.Join(" ", canonical.Split('-')
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    private static string DifficultyLabel(int level) => level switch
    {
        1 => "Easy",
        2 => "Medium",
        3 => "Hard",
        _ => "VeryHard"
    };

    private record SkillMeta(
        double SalaryPremiumPct,
        double DemandScore,
        double GrowthMomentum,
        int    LearningDifficulty,
        int    EstimatedWeeks,
        double AiRisk)
    {
        public static readonly SkillMeta Default =
            new(SalaryPremiumPct: 8.0, DemandScore: 6.0, GrowthMomentum: 6.0,
                LearningDifficulty: 2, EstimatedWeeks: 10, AiRisk: 3.0);
    }
}
