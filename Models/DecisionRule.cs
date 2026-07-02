using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Règle de décision RH versionnée (seuils/pondérations promotion, succession, éligibilité…)
/// — externalise les valeurs aujourd'hui codées en dur dans les contrôleurs vers une
/// configuration pilotable et auditable. Une nouvelle version désactive l'ancienne (Actif = false)
/// plutôt que de l'écraser, pour conserver l'historique des règles appliquées.
/// </summary>
public class DecisionRule
{
    public int Id { get; set; }

    /// <summary>Identifiant stable de la règle, ex. "PROMOTION_THRESHOLD", "SUCCESSION_WEIGHTS".</summary>
    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Paramètres de la règle (seuils, pondérations…) sérialisés en JSON.</summary>
    [Required]
    public string ParametresJson { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public DateTime DateEffet { get; set; } = DateTime.Now;

    public bool Actif { get; set; } = true;

    public string? ModifieParId { get; set; }
    public ApplicationUser? ModifiePar { get; set; }
}
