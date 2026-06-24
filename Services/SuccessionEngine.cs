using SIRH.EY.Models;

namespace SIRH.EY.Services;

/// <summary>
/// Moteur de succession partagé entre CollaborateursController et ChatbotController.
/// Garantit des résultats identiques dans les deux contextes (vue Razor et API chatbot).
/// </summary>
public static class SuccessionEngine
{
    private static readonly StringComparer Ci = StringComparer.OrdinalIgnoreCase;

    private static readonly HashSet<string> ContratsIneligibles =
        new(StringComparer.OrdinalIgnoreCase) { "Stage", "Alternance", "Stagiaire", "Intern" };

    // ─── Construction des compétences requises ────────────────────────────────

    /// <summary>
    /// Priorité : référentiel <c>CompetencesRequisesParPoste</c> du poste du partant.
    /// Fallback  : compétences personnelles du partant avec NiveauActuel >= 3
    ///             (exclut les compétences accessoires / bas niveau).
    /// Doublons  : gardé le NiveauRequis le plus élevé.
    /// </summary>
    public static List<CompetenceExigee> BuildExigences(
        IEnumerable<CompetenceRequiseParPoste> referentiel,
        IEnumerable<Competence>? competencesPartant)
    {
        var fromRef = referentiel
            .Where(r => !string.IsNullOrWhiteSpace(r.Competence))
            .GroupBy(r => r.Competence.Trim(), Ci)
            .Select(g => new CompetenceExigee(g.Key, g.Max(r => r.NiveauRequis)))
            .ToList();

        if (fromRef.Any())
            return fromRef;

        // Fallback : compétences du partant significatives (niveau >= 3)
        return (competencesPartant ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom) && c.NiveauActuel >= 3)
            .GroupBy(c => c.Nom.Trim(), Ci)
            .Select(g => new CompetenceExigee(g.Key, g.Max(c => c.NiveauActuel)))
            .ToList();
    }

    // ─── Scoring d'un candidat ────────────────────────────────────────────────

    /// <summary>
    /// Calcule le score de succession pondéré (0–100) pour un candidat.
    ///
    /// Composition :
    ///   60 % — Couverture des compétences (avec comparaison NiveauActuel/NiveauRequis)
    ///   15 % — Ancienneté (plafonnée à 10 ans)
    ///   15 % — PotentielCarriere (High=100 / Medium=60 / Low=20 / null=40)
    ///   10 % — Profil transversal (autre département et au moins 1 compétence commune)
    ///
    /// Règles de couverture par compétence :
    ///   NiveauActuel >= NiveauRequis      → 100 % (couverture complète)
    ///   NiveauActuel == NiveauRequis - 1  → 60 %  (gap acceptable, formation courte)
    ///   NiveauActuel <= NiveauRequis - 2  → 0 %   (gap critique)
    ///   Absente                           → 0 %
    ///
    /// Éligibilité pool principal : Ancienneté >= 2 ans ET contrat non stagiaire.
    /// </summary>
    public static ResultatScore Score(
        Collaborateur candidat,
        List<CompetenceExigee> exigences,
        string deptPartant)
    {
        var compsCandidat = (candidat.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .ToList();

        // ── 1. Couverture compétences (60 %) ─────────────────────────────────
        int communes = 0;
        var manquantes = new List<string>();
        double cumulCouverture = 0;

        foreach (var ex in exigences)
        {
            var comp = compsCandidat.FirstOrDefault(c => Ci.Equals(c.Nom.Trim(), ex.Nom));

            if (comp == null)
            {
                manquantes.Add(ex.Nom);
            }
            else if (comp.NiveauActuel >= ex.NiveauRequis)
            {
                cumulCouverture += 1.0;
                communes++;
            }
            else if (comp.NiveauActuel == ex.NiveauRequis - 1)
            {
                // Gap acceptable : compétence présente mais à renforcer
                cumulCouverture += 0.6;
                communes++;
                manquantes.Add(ex.Nom); // → formation de renforcement recommandée
            }
            else
            {
                // Gap critique (≥ 2 niveaux d'écart)
                manquantes.Add(ex.Nom);
            }
        }

        var scoreCouverture = exigences.Any()
            ? (int)Math.Round(cumulCouverture / exigences.Count * 100)
            : 0;

        // ── 2. Ancienneté (15 %) ─────────────────────────────────────────────
        var anciennete = candidat.Anciennete;
        var scoreAnciennete = (int)Math.Round(Math.Min(anciennete / 10.0, 1.0) * 100);

        // ── 3. Potentiel carrière (15 %) ─────────────────────────────────────
        var scorePotentiel = (candidat.PotentielCarriere?.Trim().ToLowerInvariant()) switch
        {
            "high"   => 100,
            "medium" => 60,
            "low"    => 20,
            _        => 40   // non renseigné → neutre
        };

        // ── 4. Profil transversal (10 %) ─────────────────────────────────────
        var deptCandidat = (candidat.Departement ?? "").Trim();
        var transversal = deptPartant.Length > 0
            && deptCandidat.Length > 0
            && !Ci.Equals(deptPartant, deptCandidat)
            && communes > 0;
        var scoreTransversal = transversal ? 100 : 0;

        // ── Score global pondéré ──────────────────────────────────────────────
        var scoreSuccession = (int)Math.Round(
            scoreCouverture  * 0.60 +
            scoreAnciennete  * 0.15 +
            scorePotentiel   * 0.15 +
            scoreTransversal * 0.10);

        // ── Éligibilité pool principal ────────────────────────────────────────
        var contratIneligible = ContratsIneligibles.Contains(candidat.TypeContrat ?? "");
        var estEligible = anciennete >= 2 && !contratIneligible;

        return new ResultatScore
        {
            Candidat          = candidat,
            ScoreSuccession   = scoreSuccession,
            ScoreCouverture   = scoreCouverture,
            NbCommunes        = communes,
            Manquantes        = manquantes,
            ProfilTransversal = transversal,
            AnciennetéAns     = anciennete,
            EstEligible       = estEligible
        };
    }
}

/// <summary>Compétence exigée pour un poste, avec le niveau minimum requis (1–5).</summary>
public record CompetenceExigee(string Nom, int NiveauRequis);

/// <summary>Résultat du scoring pour un candidat.</summary>
public class ResultatScore
{
    public Collaborateur  Candidat          { get; init; } = null!;
    /// <summary>Score global pondéré 0–100 (critère de classement).</summary>
    public int            ScoreSuccession   { get; init; }
    /// <summary>Couverture des compétences requises 0–100 (60 % du score global).</summary>
    public int            ScoreCouverture   { get; init; }
    /// <summary>Compétences requises couvertes à NiveauActuel >= NiveauRequis ou NiveauRequis-1.</summary>
    public int            NbCommunes        { get; init; }
    /// <summary>Compétences absentes ou en gap critique/acceptable (nécessitent formation).</summary>
    public List<string>   Manquantes        { get; init; } = new();
    public bool           ProfilTransversal { get; init; }
    public int            AnciennetéAns     { get; init; }
    /// <summary>Éligible au pool principal (ancienneté >= 2 ans, contrat non-stage).</summary>
    public bool           EstEligible       { get; init; }
}
