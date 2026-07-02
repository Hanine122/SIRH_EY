namespace SIRH.EY.Services;

public enum CompetenceCompletionOutcome { NoCompetence, Created, Incremented, AlreadyAtTarget }

/// <summary>
/// Décision pure résultant de la complétion d'une formation. Ne contient aucune donnée de
/// persistance (pas d'Id, pas d'entité) — c'est au controller appelant de construire/muter
/// l'entité Competence et son propre message, cf. FormationCompletionEngine.
/// </summary>
public record CompetenceCompletionResult(
    CompetenceCompletionOutcome Outcome,
    string? CompetenceNom = null,
    int? NiveauCible = null,
    int? NouveauNiveau = null);

/// <summary>
/// Décision de mise à jour de compétence à la complétion d'une formation — partagée entre
/// FormationsController.TerminerFormation et InscriptionsController.Terminer. Les deux
/// appelants ont des effets de bord et des messages différents (vérifié avant extraction) :
/// ce moteur ne produit QUE la décision (créer / incrémenter / déjà au niveau cible / aucune
/// compétence visée), jamais le message ni les effets de bord sur l'Inscription elle-même.
/// </summary>
public static class FormationCompletionEngine
{
    public static CompetenceCompletionResult ResolveCompetenceUpdate(
        Models.Competence? existing,
        string? competenceVisee,
        string grade)
    {
        if (string.IsNullOrEmpty(competenceVisee))
            return new CompetenceCompletionResult(CompetenceCompletionOutcome.NoCompetence);

        if (existing == null)
        {
            int niveauCible = CompetenceRules.GetNiveauCibleParGrade(grade);
            return new CompetenceCompletionResult(CompetenceCompletionOutcome.Created, competenceVisee, niveauCible);
        }

        if (existing.NiveauActuel < existing.NiveauCible)
        {
            int nouveauNiveau = Math.Min(existing.NiveauActuel + 1, existing.NiveauCible);
            return new CompetenceCompletionResult(CompetenceCompletionOutcome.Incremented, NouveauNiveau: nouveauNiveau);
        }

        return new CompetenceCompletionResult(CompetenceCompletionOutcome.AlreadyAtTarget);
    }
}
