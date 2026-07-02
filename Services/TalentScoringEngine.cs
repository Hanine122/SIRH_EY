using SIRH.EY.Models;

namespace SIRH.EY.Services;

/// <summary>
/// Moteur de scoring talent (performance / potentiel / 9-box) partagé entre
/// TalentController (vues Razor) et ChatbotController (API).
/// Garantit des résultats identiques dans les deux contextes.
/// </summary>
public static class TalentScoringEngine
{
    public static int CalculatePerformanceScore(Collaborateur c)
    {
        int score = 3; // Base

        // Auto-évaluations (moyenne des compétences)
        if (c.Competences?.Any() == true)
        {
            var avgCompetence = c.Competences.Average(comp => comp.NiveauActuel);
            score += (int)Math.Round(avgCompetence / 5.0 * 2); // 0-2 points
        }

        // Formations complétées
        if (c.Inscriptions?.Any() == true)
        {
            var formationRate = c.Inscriptions.Count(i => i.Terminee) / (double)c.Inscriptions.Count();
            score += formationRate > 0.8 ? 1 : 0;
        }

        return Math.Min(5, score);
    }

    public static int CalculatePotentielScore(Collaborateur c)
    {
        int score = 3; // Base

        // Progression rapide (si ancienneté < 2 ans et déjà bon grade)
        var anciennete = (DateTime.Now - c.DateEmbauche).TotalDays / 365;
        if (anciennete < 2 && (c.Grade == "Senior" || c.Grade == "Manager"))
            score += 1;

        // Formations certifiantes (simulation)
        if (c.Inscriptions?.Any(i => i.Terminee) == true)
            score += 1;

        return Math.Min(5, score);
    }

    public static NineBoxCategory Calculate9BoxCategory(int performance, int potentiel)
    {
        // Matrice 9-box standard
        return (performance, potentiel) switch
        {
            ( >= 4, >= 4 ) => NineBoxCategory.Star,
            ( >= 4, 3 ) => NineBoxCategory.FutureLeader,
            ( >= 4, <= 2 ) => NineBoxCategory.HighProfessional,
            ( 3, >= 4 ) => NineBoxCategory.EmergingTalent,
            ( 3, 3 ) => NineBoxCategory.SolidProfessional,
            ( 3, <= 2 ) => NineBoxCategory.InPlace,
            ( <= 2, >= 4 ) => NineBoxCategory.RisingStar,
            ( <= 2, 3 ) => NineBoxCategory.NeedDevelopment,
            _ => NineBoxCategory.Underperformer
        };
    }
}
