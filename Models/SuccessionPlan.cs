using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Plan de succession persistant pour un poste critique. Remplace le calcul à la volée
/// aujourd'hui fait par CollaborateursController.ChoisirRemplacant / SuccessionEngine :
/// ici le résultat est figé, daté et soumis à approbation manager puis RH.
/// </summary>
public class SuccessionPlan
{
    public int Id { get; set; }

    /// <summary>Titulaire actuel du poste, nullable si le poste est déjà vacant.</summary>
    public int? CollaborateurTitulaireId { get; set; }
    public Collaborateur? CollaborateurTitulaire { get; set; }

    [Required]
    [StringLength(150)]
    public string Poste { get; set; } = string.Empty;

    public string? Departement { get; set; }

    /// <summary>Cycle de revue qui a déclenché/encadre ce plan (optionnel).</summary>
    public int? ReviewCycleId { get; set; }
    public ReviewCycle? ReviewCycle { get; set; }

    public SuccessionPlanStatus Statut { get; set; } = SuccessionPlanStatus.Draft;

    public DateTime DateCreation { get; set; } = DateTime.Now;

    // ── Étape 1 : validation manager ──
    public string? ProposeParId { get; set; }
    public ApplicationUser? ProposePar { get; set; }
    public DateTime? DateValidationManager { get; set; }

    // ── Étape 2 : approbation RH ──
    public string? ApprouveParId { get; set; }
    public ApplicationUser? ApprouvePar { get; set; }
    public DateTime? DateApprobationRH { get; set; }

    /// <summary>Renseigné uniquement si Statut = Rejected.</summary>
    public string? CommentaireRefus { get; set; }

    public ICollection<SuccessorRankingSnapshot>? Snapshots { get; set; }
}

public enum SuccessionPlanStatus
{
    Draft = 0,
    ManagerValidated = 1,
    HRApproved = 2,
    Rejected = 3,
    Archived = 4
}
