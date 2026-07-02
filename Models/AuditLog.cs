using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Journal d'audit transverse — point d'écriture unique pour toute entité talent sensible
/// (TalentEvaluation, OKR, SuccessionPlan, DecisionRule…). Association polymorphe via
/// EntityType + EntityId (pas de FK typée : une seule table pour toutes les entités auditées).
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }

    public string? UtilisateurId { get; set; }
    public ApplicationUser? Utilisateur { get; set; }

    public DateTime DateAction { get; set; } = DateTime.Now;
}

public enum AuditAction
{
    Created = 0,
    Updated = 1,
    StatusChanged = 2,
    Approved = 3,
    Locked = 4,
    Deleted = 5
}
