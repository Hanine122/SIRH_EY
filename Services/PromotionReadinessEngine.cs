using SIRH.EY.Models;
using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

/// <summary>
/// Calcul pur du scoring de readiness promotion — extrait de PromotionReadinessService,
/// qui ne fait plus que la récupération EF et le mapping. Aucune règle de classification
/// (Severity/PriorityLabel) ici : ces décisions vivent dans DecisionEngine.
/// </summary>
public static class PromotionReadinessEngine
{
    public static List<PromotionCompetencyGapViewModel> BuildCompetencyGaps(
        IEnumerable<(string Name, int RequiredLevel)> requiredCompetencies,
        Dictionary<string, int> currentSkills)
    {
        return requiredCompetencies
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
                    Severity = DecisionEngine.ClassifyGapSeverity(gap),
                    PriorityLabel = DecisionEngine.ClassifyGapPriorityLabel(gap)
                };
            })
            .OrderByDescending(g => g.Gap)
            .ThenBy(g => g.Competence)
            .ToList();
    }

    public static double ComputeCompatibilityScore(int coveredCount, int totalCount) =>
        Math.Round(100.0 * coveredCount / Math.Max(1, totalCount), 1);

    public static double ComputeReadiness(int totalGap, int maxGap) =>
        Math.Round(Math.Max(35, 100.0 - (totalGap * 100.0 / maxGap)), 1);

    /// <summary>25 % du score multi-critères. Fallback : NiveauPreparationSuccession ou neutre 50.</summary>
    public static double ComputeScorePerformance(TalentEvaluation? talentEval, int? niveauPreparationSuccession) =>
        talentEval != null
            ? Math.Min(100.0, talentEval.PerformanceScore * 20.0)
            : niveauPreparationSuccession ?? 50;

    /// <summary>20 % du score multi-critères. Fallback : PotentielCarriere (High/Medium/Low) ou neutre 40.</summary>
    public static double ComputeScorePotentiel(TalentEvaluation? talentEval, string? potentielCarriere) =>
        talentEval != null
            ? Math.Min(100.0, talentEval.PotentielScore * 20.0)
            : (potentielCarriere?.Trim().ToLowerInvariant()) switch
            {
                "high" => 100.0,
                "medium" => 60.0,
                "low" => 20.0,
                _ => 40.0
            };

    /// <summary>15 % du score multi-critères, ancienneté relative au seuil du grade cible.</summary>
    public static double ComputeScoreAnciennete(double ancienneteAns, int? ancienneteMinAnsGradeCible) =>
        ancienneteMinAnsGradeCible is > 0
            ? Math.Min(100.0, ancienneteAns * 100.0 / ancienneteMinAnsGradeCible.Value)
            : Math.Min(100.0, ancienneteAns * 25.0);

    /// <summary>Score global pondéré 40 % compétences / 25 % performance / 20 % potentiel / 15 % ancienneté.</summary>
    public static double ComputeMultiCriteriaScore(double scoreComp, double scorePerformance, double scorePotentiel, double scoreAnc) =>
        Math.Round(0.40 * scoreComp + 0.25 * scorePerformance + 0.20 * scorePotentiel + 0.15 * scoreAnc, 1);

    public static double ComputePromotionPotential(double multiCriteriaScore) =>
        Math.Round(Math.Min(98.0, multiCriteriaScore), 1);

    public static (int Min, int Max) EstimateMonths(int totalGap, int completedTrainings, int recommendationCount)
    {
        var baseMonths = Math.Max(2, totalGap * 2 + recommendationCount);
        var trainingReduction = Math.Min(4, completedTrainings);
        var min = Math.Max(1, baseMonths - trainingReduction);
        return (min, min + 2);
    }

    public static List<PromotionFormationRecommendationViewModel> BuildFormationRecommendations(
        List<PromotionCompetencyGapViewModel> gaps, List<Formation> formations)
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
                    ProgressionImpact = DecisionEngine.ClassifyGapProgressionImpact(gap.Gap),
                    EstimatedWeeks = Math.Max(2, gap.Gap * 3)
                };
            })
            .ToList();

        if (!recommendations.Any())
        {
            recommendations.Add(new PromotionFormationRecommendationViewModel
            {
                FormationTitre = "Mentoring manager avance",
                TargetCompetence = "Leadership",
                ReadinessGain = 8,
                ProgressionImpact = "Consolidation avant promotion",
                EstimatedWeeks = 4
            });
        }

        return recommendations;
    }
}
