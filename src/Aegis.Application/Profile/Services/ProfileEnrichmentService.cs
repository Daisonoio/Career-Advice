using Aegis.Application.Profile.DTOs;
using Aegis.Domain.Entities;

namespace Aegis.Application.Profile.Services;

public class ProfileEnrichmentService
{
    private static readonly Dictionary<string, List<string>> RoleRequiredSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Backend Engineer"]    = ["csharp", "dotnet-aspnet-core", "rest-apis", "postgresql", "docker"],
        ["Backend Developer"]   = ["csharp", "dotnet-aspnet-core", "rest-apis", "postgresql", "docker"],
        ["Platform Engineer"]   = ["kubernetes", "docker", "terraform", "aws", "cicd"],
        ["Software Architect"]  = ["microservices", "event-driven", "ddd", "cqrs", "system-design"],
        ["DevOps Engineer"]     = ["kubernetes", "terraform", "cicd", "prometheus-grafana", "ansible"],
        ["AI/ML Engineer"]      = ["machine-learning", "python", "llm-integration", "mlops", "tensorflow-pytorch"],
        ["Tech Lead"]           = ["team-leadership", "system-design", "code-review", "mentoring", "microservices"],
        ["Full Stack Developer"] = ["csharp", "dotnet-aspnet-core", "typescript", "rest-apis", "postgresql"],
    };

    public double? ComputeSalaryPercentile(decimal? salaryMidpoint, MarketKpi? roleKpi)
    {
        if (salaryMidpoint is null
            || roleKpi?.SalaryP25 is null
            || roleKpi.SalaryMedian is null
            || roleKpi.SalaryP75 is null)
            return null;

        var s      = (double)salaryMidpoint.Value;
        var p25    = (double)roleKpi.SalaryP25.Value;
        var median = (double)roleKpi.SalaryMedian.Value;
        var p75    = (double)roleKpi.SalaryP75.Value;

        if (p25 <= 0 || median <= p25 || p75 <= median) return null;

        double percentile;
        if (s <= p25)
            percentile = s / p25 * 25.0;
        else if (s <= median)
            percentile = 25.0 + (s - p25) / (median - p25) * 25.0;
        else if (s <= p75)
            percentile = 50.0 + (s - median) / (p75 - median) * 25.0;
        else
            percentile = 75.0 + Math.Min((s - p75) / p75 * 25.0, 25.0);

        return Math.Round(Math.Clamp(percentile, 0.0, 100.0), 1);
    }

    public double ComputeStagnationRisk(UserProfile profile, MarketKpi? roleKpi)
    {
        double risk = 0.0;

        // Signal 1: role growth momentum — ruolo in declino aumenta il rischio
        var growth = roleKpi?.GrowthMomentum ?? 5.0;
        risk += growth switch
        {
            < 3.0 => 3.0,
            < 5.0 => 1.5,
            _     => 0.0
        };

        // Signal 2: anni di esperienza senza segnali di progressione
        risk += profile.YearsExperience switch
        {
            >= 6 => 2.5,
            >= 4 => 1.5,
            >= 2 => 0.5,
            _    => 0.0
        };

        // Signal 3: numero skill primarie dichiarate
        var primaryCount = profile.Skills.Count(s => s.IsPrimary);
        risk += primaryCount switch
        {
            0 => 2.0,
            1 => 1.0,
            2 => 0.5,
            _ => 0.0
        };

        // Signal 4: livello medio sulle skill primarie
        var primarySkills = profile.Skills.Where(s => s.IsPrimary).ToList();
        if (primarySkills.Count > 0)
        {
            var avgLevel = primarySkills.Average(s => (double)s.SelfRatedLevel);
            risk += avgLevel switch
            {
                < 2.5 => 1.5,
                < 3.5 => 0.5,
                _     => 0.0
            };
        }
        else if (profile.Skills.Count == 0)
        {
            risk += 2.0;
        }

        return Math.Round(Math.Clamp(risk, 0.0, 10.0), 1);
    }

    public List<QuickWinResponse> ComputeQuickWins(UserProfile profile, MarketKpi? roleKpi)
    {
        var wins = new List<QuickWinResponse>();

        var userCanonicals = profile.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Quick Win 1: approfondisci la skill primaria con livello più basso
        var weakPrimary = profile.Skills
            .Where(s => s.IsPrimary && s.SelfRatedLevel <= 2 && s.Skill?.Name is not null)
            .MinBy(s => s.SelfRatedLevel);

        if (weakPrimary is not null)
        {
            wins.Add(new QuickWinResponse(
                Action: $"Approfondisci {weakPrimary.Skill!.Name} da livello {weakPrimary.SelfRatedLevel} a {Math.Min(weakPrimary.SelfRatedLevel + 2, 5)}",
                ImpactLevel: "High",
                EstimatedWeeks: 8,
                Rationale: $"{weakPrimary.Skill.Name} è una tua skill primaria con livello basso. Aumentarla migliora direttamente la seniority percepita nelle interviste."));
        }

        // Quick Win 2: prima skill richiesta per il ruolo che manca nel profilo
        if (profile.CurrentRole is not null
            && RoleRequiredSkills.TryGetValue(profile.CurrentRole, out var requiredSkills))
        {
            var missingRequired = requiredSkills.FirstOrDefault(s => !userCanonicals.Contains(s));
            if (missingRequired is not null)
            {
                wins.Add(new QuickWinResponse(
                    Action: $"Aggiungi '{missingRequired}' al tuo stack",
                    ImpactLevel: "Medium",
                    EstimatedWeeks: 6,
                    Rationale: $"Skill attesa nel profilo standard di un {profile.CurrentRole} ma non presente nel tuo profilo."));
            }
        }

        // Quick Win 3: gap salariale rispetto alla mediana, oppure dati mancanti
        if (profile.SalaryExpectation is null)
        {
            wins.Add(new QuickWinResponse(
                Action: "Inserisci la tua retribuzione attuale nel profilo",
                ImpactLevel: "Low",
                EstimatedWeeks: 0,
                Rationale: "Senza dati salariali non è possibile calcolare il tuo percentile rispetto al mercato."));
        }
        else if (roleKpi?.SalaryMedian is not null)
        {
            var gap = (double)(roleKpi.SalaryMedian.Value - profile.SalaryExpectation.Midpoint());
            if (gap > 5_000)
            {
                wins.Add(new QuickWinResponse(
                    Action: $"Considera una rinegoziazione: sei €{gap:N0}/anno sotto la mediana di mercato",
                    ImpactLevel: "High",
                    EstimatedWeeks: 0,
                    Rationale: $"La mediana EU per {profile.CurrentRole ?? "il tuo ruolo"} è €{roleKpi.SalaryMedian.Value:N0}. Un cambio o una rinegoziazione può colmare il gap senza cambiare skill."));
            }
        }

        return wins.Take(3).ToList();
    }
}
