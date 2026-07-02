using SIRH.EY.Models;

namespace SIRH.EY.Services;

/// <summary>
/// Calculs de correspondance/écart compétence candidat-remplaçant. Contrairement à
/// SuccessionEngine (formule pondérée unique), chaque méthode ici correspond à un site
/// d'appel qui avait déjà sa propre formule avant extraction — vérifié : ce ne sont PAS
/// des duplications les unes des autres, elles restent volontairement indépendantes.
/// </summary>
public static class RemplacantMatchingEngine
{
    /// <summary>
    /// Compétences du partant (déjà filtrées NiveauCible >= 4 par l'appelant) non couvertes
    /// par le remplaçant (NiveauActuel du remplaçant < NiveauCible du partant, sur le même nom).
    /// Source : CollaborateursController.Depart.
    /// </summary>
    public static List<string> BuildCompetencesManquantesSimple(
        IEnumerable<Competence> competencesPartant,
        IEnumerable<Competence> competencesRemplacant)
    {
        var remplacantList = competencesRemplacant.ToList();

        return competencesPartant
            .Where(cp => !remplacantList.Any(cr => cr.Nom == cp.Nom && cr.NiveauActuel >= cp.NiveauCible))
            .Select(cp => cp.Nom)
            .ToList();
    }

    private static List<string> NomsDistincts(IEnumerable<Competence>? competences, StringComparer comparer) =>
        (competences ?? Enumerable.Empty<Competence>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Nom))
            .Select(x => x.Nom.Trim())
            .Distinct(comparer)
            .ToList();

    /// <summary>
    /// Pourcentage de couverture (0-100) d'un candidat sur une liste de compétences requises.
    /// Source : CollaborateursController.ExportComparaisonRemplacantsPdf (local function CompatPour).
    /// </summary>
    public static int CompatibilitePourcent(List<string> competencesRequises, IEnumerable<Competence>? competencesCandidat)
    {
        if (competencesRequises.Count == 0) return 0;

        var comparer = StringComparer.OrdinalIgnoreCase;
        var noms = NomsDistincts(competencesCandidat, comparer);

        var manq = competencesRequises.Count(r => !noms.Any(a => comparer.Equals(a, r)));
        var poss = Math.Max(0, competencesRequises.Count - manq);
        return (int)Math.Round(100.0 * poss / competencesRequises.Count);
    }

    /// <summary>
    /// Compétences requises non couvertes par un candidat.
    /// Source : CollaborateursController.ExportComparaisonRemplacantsPdf (local function Manquantes).
    /// </summary>
    public static List<string> CompetencesManquantesPourCandidat(List<string> competencesRequises, IEnumerable<Competence>? competencesCandidat)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var noms = NomsDistincts(competencesCandidat, comparer);

        return competencesRequises.Where(r => !noms.Any(a => comparer.Equals(a, r))).ToList();
    }

    /// <summary>
    /// Différence ensembliste simple (comparaison de chaînes par défaut, PAS OrdinalIgnoreCase —
    /// comportement d'origine conservé tel quel). Source : CollaborateursController.GetRemplacants.
    /// </summary>
    public static List<string> CompetencesManquantesParNoms(List<string> competencesRequisesNoms, List<string> competencesCandidatNoms)
    {
        return competencesRequisesNoms.Except(competencesCandidatNoms).ToList();
    }

    /// <summary>
    /// Score de correspondance (0-100, un chiffre après la virgule), compétences communes et
    /// manquantes pour un candidat. Formule indépendante de SuccessionEngine.Score (pas de
    /// pondération ancienneté/potentiel/transversalité ici — matching compétences pur).
    /// Source : RhInsightsController.GetMatchingRemplacants.
    /// </summary>
    public static (int Communes, List<string> Manquantes, double Score, List<string> NomsCandidat) ScoreMatching(
        List<string> competencesRequises, IEnumerable<Competence>? competencesCandidat)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var nomsCandidat = NomsDistincts(competencesCandidat, comparer);

        var communes = competencesRequises.Count(r => nomsCandidat.Any(a => comparer.Equals(a, r)));
        var manquantes = competencesRequises.Where(r => !nomsCandidat.Any(a => comparer.Equals(a, r))).ToList();
        var score = competencesRequises.Count > 0 ? Math.Round(100.0 * communes / competencesRequises.Count, 1) : 0;

        return (communes, manquantes, score, nomsCandidat);
    }
}
