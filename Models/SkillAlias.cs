using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Variante orthographique/nom alternatif d'un Skill (ex. "PowerBI", "MS Power BI" → Skill "Power BI").
/// Résout le problème de fragilité du matching par chaîne identifié dans l'audit du moteur de
/// succession/scoring : plusieurs formulations d'une même compétence convergent vers un seul Skill.
/// </summary>
public class SkillAlias
{
    public int Id { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    [Required]
    [StringLength(150)]
    public string Alias { get; set; } = string.Empty;
}
