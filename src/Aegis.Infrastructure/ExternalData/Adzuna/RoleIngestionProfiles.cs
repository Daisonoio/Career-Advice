namespace Aegis.Infrastructure.ExternalData.Adzuna;

/// <summary>
/// Per-role ingestion parameters used by <see cref="KpiIngestionJob"/>.
///
/// AutomationProbability: probability (0–1) that the role will be automated.
///   Source: Oxford Future of Employment (Frey &amp; Osborne, updated 2023) +
///           McKinsey Global Institute "The Future of Work" 2024.
///
/// CommoditizationIndex: how easily the role can be commoditized or outsourced (0–1).
///   Higher = more substitutable; derived from skill specificity and barrier-to-entry research.
///
/// JobCountBaseline: expected monthly job postings in a mid-size EU market (Italy reference).
///   Used to normalise the DemandScore so the score is comparable across roles
///   rather than biased by absolute market size.
///
/// RemotePremiumPct: typical salary premium (%) for fully-remote positions.
///   Source: Stack Overflow Developer Survey 2024, Levels.fyi EU data.
/// </summary>
internal static class RoleIngestionProfiles
{
    internal static readonly Dictionary<string, RoleProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Backend Engineer"]     = new("backend developer",         800, 0.21, 0.35, 12.0),
        ["Backend Developer"]    = new("backend developer",         800, 0.21, 0.35, 12.0),
        ["Platform Engineer"]    = new("platform engineer",         350, 0.15, 0.25, 15.0),
        ["Software Architect"]   = new("software architect",        250, 0.10, 0.18, 15.0),
        ["DevOps Engineer"]      = new("devops engineer",           600, 0.18, 0.28, 14.0),
        ["AI/ML Engineer"]       = new("machine learning engineer", 450, 0.08, 0.15, 18.0),
        ["Tech Lead"]            = new("tech lead developer",       400, 0.09, 0.15, 12.0),
        ["Data Engineer"]        = new("data engineer",             500, 0.16, 0.30, 13.0),
        ["Full Stack Developer"] = new("full stack developer",      900, 0.26, 0.40, 11.0),
        ["Frontend Engineer"]    = new("frontend developer",        850, 0.28, 0.42, 10.0),
    };
}

internal record RoleProfile(
    string AdzunaQuery,
    int JobCountBaseline,
    double AutomationProbability,
    double CommoditizationIndex,
    double RemotePremiumPct);
