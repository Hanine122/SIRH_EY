namespace SIRH.EY.Models;

public class ParcoursCarriereViewModel
{
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public string GradeActuel { get; set; } = string.Empty;
    public string? ProchainGrade { get; set; }

    /// <summary>0–100 % representing progression toward next grade based on competences.</summary>
    public int ProgressionGrade { get; set; }

    public List<CompetenceStatutItem> CompetencesAcquises  { get; set; } = new();
    public List<CompetenceStatutItem> CompetencesManquantes { get; set; } = new();

    public List<Formation> FormationsRecommandees { get; set; } = new();

    /// <summary>Inscriptions courantes du collaborateur (to detect already-enrolled).</summary>
    public List<int> FormationsInscritesIds { get; set; } = new();
}

public class CompetenceStatutItem
{
    public string Nom { get; set; } = string.Empty;
    public int NiveauActuel { get; set; }
    public int NiveauCible { get; set; }
    public Formation? FormationRecommandee { get; set; }

    public int PourcentageAtteint =>
        NiveauCible == 0 ? 100 : Math.Min(100, (int)((double)NiveauActuel / NiveauCible * 100));
}
