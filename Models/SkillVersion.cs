using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Historique versionné de la définition d'un Skill (nom, description) — même principe de
/// gouvernance que DecisionRule : une nouvelle version ajoute une ligne plutôt que d'écraser
/// l'ancienne, pour pouvoir répondre à "comment ce skill était-il défini au moment T".
/// </summary>
public class SkillVersion
{
    public int Id { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    public int Version { get; set; } = 1;

    [Required]
    [StringLength(150)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime DateEffet { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;

    public string? ModifieParId { get; set; }
    public ApplicationUser? ModifiePar { get; set; }
}
