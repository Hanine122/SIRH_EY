using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Defines which competences are required for a Position, with a required proficiency level.
/// Uses a name-based reference (not FK to Competence) because Competence is per-collaborateur.
/// </summary>
public class PositionRequiredCompetence
{
    public int Id { get; set; }

    [Required]
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    [Required, MaxLength(100)]
    [Display(Name = "Compétence")]
    public string CompetenceName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Catégorie")]
    public string? Category { get; set; }

    [Required, Range(1, 5)]
    [Display(Name = "Niveau requis")]
    public int RequiredLevel { get; set; } = 3;

    [Display(Name = "Obligatoire")]
    public bool IsMandatory { get; set; } = true;
}
