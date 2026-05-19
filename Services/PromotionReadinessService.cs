using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

public class PromotionReadinessService : IPromotionReadinessService
{
    private readonly ApplicationDbContext _context;

    public PromotionReadinessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionReadinessSimulatorViewModel> BuildSimulatorAsync()
    {
        var collaborateurs = await _context.Collaborateurs
            .AsNoTracking()
            .Where(c => c.Actif)
            .OrderBy(c => c.Nom)
            .ThenBy(c => c.Prenom)
            .Select(c => new PromotionCollaborateurOptionViewModel
            {
                Id = c.Id,
                NomComplet = (c.Prenom + " " + c.Nom).Trim(),
                Poste = c.Poste ?? "",
                Grade = c.Grade ?? "",
                Departement = c.Departement ?? ""
            })
            .ToListAsync();

        var targets = await BuildTargetOptionsAsync();
        var defaultCollaborateur = collaborateurs.FirstOrDefault();
        var defaultTarget = targets.FirstOrDefault();

        return new PromotionReadinessSimulatorViewModel
        {
            Collaborateurs = collaborateurs,
            TargetPositions = targets,
            DefaultResult = defaultCollaborateur != null && defaultTarget != null
                ? await SimulateAsync(defaultCollaborateur.Id, defaultTarget.Key)
                : BuildFallbackResult()
        };
    }

    public async Task<PromotionReadinessResultViewModel?> SimulateAsync(int collaborateurId, string targetKey)
    {
        var collaborateur = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .FirstOrDefaultAsync(c => c.Id == collaborateurId && c.Actif);

        if (collaborateur == null)
            return null;

        var target = await ResolveTargetAsync(targetKey, collaborateur);
        var requiredCompetencies = await ResolveRequiredCompetenciesAsync(target.Poste, target.Grade);
        var formations = await _context.Formations.AsNoTracking().ToListAsync();

        var currentSkills = (collaborateur.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .GroupBy(c => c.Nom.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(c => c.NiveauActuel), StringComparer.OrdinalIgnoreCase);

        var gaps = requiredCompetencies
            .Select(req =>
            {
                currentSkills.TryGetValue(req.Name, out var currentLevel);
                var gap = Math.Max(0, req.RequiredLevel - currentLevel);
                return new PromotionCompetencyGapViewModel
                {
                    Competence = req.Name,
                    CurrentLevel = currentLevel,
                    RequiredLevel = req.RequiredLevel,
                    Gap = gap,
                    Severity = gap >= 3 ? "High" : gap == 2 ? "Medium" : gap == 1 ? "Low" : "Ready",
                    PriorityLabel = gap >= 3 ? "Critique" : gap == 2 ? "Prioritaire" : gap == 1 ? "A renforcer" : "Couvert"
                };
            })
            .OrderByDescending(g => g.Gap)
            .ThenBy(g => g.Competence)
            .ToList();

        var missing = gaps.Where(g => g.Gap > 0).ToList();
        var coveredCount = gaps.Count(g => g.Gap == 0);
        var totalGap = missing.Sum(g => g.Gap);
        var maxGap = Math.Max(1, gaps.Sum(g => g.RequiredLevel));
        var compatibilityScore = Math.Round(100.0 * coveredCount / Math.Max(1, gaps.Count), 1);
        var readiness = Math.Round(Math.Max(35, 100.0 - (totalGap * 100.0 / maxGap)), 1);

        var completedTrainings = (collaborateur.Inscriptions ?? Enumerable.Empty<Inscription>())
            .Count(i => i.Terminee || i.Progression >= 80);
        var leadershipIndicators = ResolveLeadershipIndicators(collaborateur, currentSkills, completedTrainings);
        var transversalSkills = ResolveTransversalSkills(currentSkills.Keys.ToList());
        var promotionPotential = Math.Round(Math.Min(98, readiness * 0.58 + compatibilityScore * 0.24 + leadershipIndicators.Count * 5 + completedTrainings * 2), 1);
        var recommendations = BuildFormationRecommendations(missing, formations);
        var months = EstimateMonths(totalGap, completedTrainings, recommendations.Count);

        return new PromotionReadinessResultViewModel
        {
            CollaborateurId = collaborateur.Id,
            CollaborateurNom = $"{collaborateur.Prenom} {collaborateur.Nom}".Trim(),
            CurrentRole = $"{collaborateur.Poste} - {collaborateur.Grade}".Trim(' ', '-'),
            TargetRole = $"{target.Poste} - {target.Grade}".Trim(' ', '-'),
            ReadinessPercentage = readiness,
            CompatibilityScore = compatibilityScore,
            PromotionPotential = promotionPotential,
            EstimatedMonthsMin = months.Min,
            EstimatedMonthsMax = months.Max,
            TransversalSkills = transversalSkills,
            LeadershipIndicators = leadershipIndicators,
            MissingCompetencies = gaps,
            RecommendedFormations = recommendations,
            ExecutiveSummary = BuildExecutiveSummary(collaborateur, target, missing, transversalSkills, leadershipIndicators, months)
        };
    }

    private async Task<List<PromotionTargetOptionViewModel>> BuildTargetOptionsAsync()
    {
        var targets = await _context.Collaborateurs
            .AsNoTracking()
            .Where(c => c.Actif && !string.IsNullOrWhiteSpace(c.Poste))
            .Select(c => new { c.Poste, c.Grade, c.Departement })
            .Distinct()
            .ToListAsync();

        var options = targets
            .Select(t => new PromotionTargetOptionViewModel
            {
                Poste = t.Poste ?? "",
                Grade = string.IsNullOrWhiteSpace(t.Grade) ? InferNextGrade(null) : t.Grade!,
                Departement = t.Departement ?? "",
            })
            .Select(t =>
            {
                t.Key = BuildTargetKey(t.Poste, t.Grade, t.Departement);
                t.Label = $"{t.Poste} - {t.Grade}" + (string.IsNullOrWhiteSpace(t.Departement) ? "" : $" ({t.Departement})");
                return t;
            })
            .OrderBy(t => t.Label)
            .ToList();

        if (!options.Any())
        {
            options.Add(new PromotionTargetOptionViewModel { Key = "Manager|Manager|RH", Label = "Manager - Manager (RH)", Poste = "Manager", Grade = "Manager", Departement = "RH" });
            options.Add(new PromotionTargetOptionViewModel { Key = "Senior Consultant|Senior|Consulting", Label = "Senior Consultant - Senior (Consulting)", Poste = "Senior Consultant", Grade = "Senior", Departement = "Consulting" });
        }

        return options;
    }

    private async Task<PromotionTargetOptionViewModel> ResolveTargetAsync(string targetKey, Collaborateur collaborateur)
    {
        var options = await BuildTargetOptionsAsync();
        var target = options.FirstOrDefault(t => t.Key == targetKey);
        if (target != null)
            return target;

        var nextGrade = InferNextGrade(collaborateur.Grade);
        var poste = string.IsNullOrWhiteSpace(collaborateur.Poste) ? "Manager" : collaborateur.Poste;
        var departement = collaborateur.Departement ?? "";
        return new PromotionTargetOptionViewModel
        {
            Key = BuildTargetKey(poste, nextGrade, departement),
            Label = $"{poste} - {nextGrade}",
            Poste = poste,
            Grade = nextGrade,
            Departement = departement
        };
    }

    private async Task<List<RequiredCompetency>> ResolveRequiredCompetenciesAsync(string poste, string grade)
    {
        var required = await _context.CompetencesRequisesParPoste
            .AsNoTracking()
            .Where(c => c.Poste == poste)
            .Select(c => c.Competence)
            .ToListAsync();

        var names = required
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        if (!names.Any())
            names = BuildFallbackCompetencies(poste, grade);

        var requiredLevel = grade.Contains("Manager", StringComparison.OrdinalIgnoreCase) ? 4 : grade.Contains("Senior", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
        return names.Select(n => new RequiredCompetency(n, requiredLevel)).ToList();
    }

    private static List<string> BuildFallbackCompetencies(string poste, string grade)
    {
        var competencies = new List<string> { "Communication", "Gestion de projet", "Analyse & resolution de problemes" };

        if (grade.Contains("Manager", StringComparison.OrdinalIgnoreCase) || poste.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            competencies.AddRange(new[] { "Leadership", "Stakeholder management", "Project governance" });
        else
            competencies.AddRange(new[] { "Expertise metier", "Collaboration transverse" });

        if (poste.Contains("Data", StringComparison.OrdinalIgnoreCase))
            competencies.Add("Data analytics");

        return competencies.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> ResolveLeadershipIndicators(Collaborateur collaborateur, Dictionary<string, int> currentSkills, int completedTrainings)
    {
        var indicators = new List<string>();
        if (currentSkills.Any(s => s.Key.Contains("Leadership", StringComparison.OrdinalIgnoreCase) && s.Value >= 3))
            indicators.Add("Leadership deja observe");
        if (currentSkills.Any(s => s.Key.Contains("Gestion", StringComparison.OrdinalIgnoreCase) && s.Value >= 3))
            indicators.Add("Pilotage et coordination");
        if (!string.IsNullOrWhiteSpace(collaborateur.Grade) && collaborateur.Grade.Contains("Senior", StringComparison.OrdinalIgnoreCase))
            indicators.Add("Seniorite operationnelle");
        if (completedTrainings > 0)
            indicators.Add("Apprentissage continu");

        if (!indicators.Any())
            indicators.Add("Potentiel a qualifier via evaluation manager");

        return indicators.Take(4).ToList();
    }

    private static List<string> ResolveTransversalSkills(List<string> skillNames)
    {
        var keywords = new[] { "Communication", "Analyse", "Projet", "Leadership", "Collaboration", "Excel", "Power BI", "Data" };
        var matches = skillNames
            .Where(skill => keywords.Any(k => skill.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (!matches.Any())
            matches.AddRange(new[] { "Communication transverse", "Adaptabilite", "Collaboration" });

        return matches;
    }

    private static List<PromotionFormationRecommendationViewModel> BuildFormationRecommendations(List<PromotionCompetencyGapViewModel> gaps, List<Formation> formations)
    {
        var recommendations = gaps
            .Take(5)
            .Select(gap =>
            {
                var formation = formations.FirstOrDefault(f =>
                    !string.IsNullOrWhiteSpace(f.CompetenceVisee) &&
                    f.CompetenceVisee.Contains(gap.Competence, StringComparison.OrdinalIgnoreCase))
                    ?? formations.FirstOrDefault(f => f.Titre.Contains(gap.Competence, StringComparison.OrdinalIgnoreCase));

                return new PromotionFormationRecommendationViewModel
                {
                    FormationTitre = formation?.Titre ?? $"Parcours cible - {gap.Competence}",
                    TargetCompetence = gap.Competence,
                    ReadinessGain = Math.Min(18, 6 + gap.Gap * 4),
                    ProgressionImpact = gap.Gap >= 3 ? "Impact critique sur la readiness" : gap.Gap == 2 ? "Impact eleve sur la compatibilite" : "Impact rapide de consolidation",
                    EstimatedWeeks = Math.Max(2, gap.Gap * 3)
                };
            })
            .ToList();

        if (!recommendations.Any())
        {
            recommendations.Add(new PromotionFormationRecommendationViewModel { FormationTitre = "Mentoring manager avance", TargetCompetence = "Leadership", ReadinessGain = 8, ProgressionImpact = "Consolidation avant promotion", EstimatedWeeks = 4 });
        }

        return recommendations;
    }

    private static (int Min, int Max) EstimateMonths(int totalGap, int completedTrainings, int recommendationCount)
    {
        var baseMonths = Math.Max(2, totalGap * 2 + recommendationCount);
        var trainingReduction = Math.Min(4, completedTrainings);
        var min = Math.Max(1, baseMonths - trainingReduction);
        return (min, min + 2);
    }

    private static string BuildExecutiveSummary(Collaborateur collaborateur, PromotionTargetOptionViewModel target, List<PromotionCompetencyGapViewModel> gaps, List<string> transversalSkills, List<string> leadershipIndicators, (int Min, int Max) months)
    {
        var name = $"{collaborateur.Prenom} {collaborateur.Nom}".Trim();
        var topGaps = gaps.Where(g => g.Gap > 0).Take(2).Select(g => g.Competence).ToList();
        var gapText = topGaps.Any() ? string.Join(", ", topGaps) : "aucun gap critique";
        var transversal = transversalSkills.FirstOrDefault() ?? "collaboration transverse";
        var leadership = leadershipIndicators.FirstOrDefault() ?? "potentiel leadership";

        return $"{name} demontre un signal fort sur {transversal} et {leadership}. Pour atteindre le role cible {target.Poste}, les principaux ecarts a traiter sont : {gapText}. Readiness estimee : {months.Min} a {months.Max} mois avec un upskilling cible.";
    }

    private static string InferNextGrade(string? currentGrade)
    {
        if (string.IsNullOrWhiteSpace(currentGrade))
            return "Senior";
        if (currentGrade.Contains("Junior", StringComparison.OrdinalIgnoreCase))
            return "Senior";
        if (currentGrade.Contains("Senior", StringComparison.OrdinalIgnoreCase))
            return "Manager";
        if (currentGrade.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            return "Senior Manager";
        return "Manager";
    }

    private static string BuildTargetKey(string poste, string grade, string departement)
    {
        return $"{poste}|{grade}|{departement}";
    }

    private static PromotionReadinessResultViewModel BuildFallbackResult()
    {
        return new PromotionReadinessResultViewModel
        {
            CollaborateurNom = "Collaborateur a selectionner",
            CurrentRole = "Role actuel",
            TargetRole = "Role cible",
            ReadinessPercentage = 72,
            CompatibilityScore = 68,
            PromotionPotential = 76,
            EstimatedMonthsMin = 6,
            EstimatedMonthsMax = 8,
            TransversalSkills = new List<string> { "Communication", "Collaboration", "Apprentissage" },
            LeadershipIndicators = new List<string> { "Potentiel a confirmer", "Progression formation" },
            MissingCompetencies = new List<PromotionCompetencyGapViewModel>
            {
                new() { Competence = "Stakeholder management", CurrentLevel = 2, RequiredLevel = 4, Gap = 2, Severity = "Medium", PriorityLabel = "Prioritaire" },
                new() { Competence = "Project governance", CurrentLevel = 2, RequiredLevel = 4, Gap = 2, Severity = "Medium", PriorityLabel = "Prioritaire" }
            },
            RecommendedFormations = new List<PromotionFormationRecommendationViewModel>
            {
                new() { FormationTitre = "Parcours Leadership & succession", TargetCompetence = "Leadership", ReadinessGain = 14, ProgressionImpact = "Impact eleve sur la readiness", EstimatedWeeks = 6 }
            },
            ExecutiveSummary = "Selectionnez un collaborateur et un role cible pour simuler la readiness promotionnelle avec les donnees RH disponibles."
        };
    }

    private record RequiredCompetency(string Name, int RequiredLevel);
}
