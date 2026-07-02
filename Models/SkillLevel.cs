using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Définition d'un palier de maîtrise (1-5, échelle identique à Competence.NiveauActuel/NiveauCible
/// dans tout le reste de l'application) pour un Skill donné — décrit ce que signifie concrètement
/// chaque niveau plutôt que de rester un entier opaque.
/// </summary>
public class SkillLevel
{
    public int Id { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    [Range(1, 5)]
    public int Niveau { get; set; }

    [Required]
    [StringLength(200)]
    public string Libelle { get; set; } = string.Empty;

    public string? CriteresValidation { get; set; }
}
