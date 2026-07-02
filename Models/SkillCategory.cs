using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Taxonomie hiérarchique des compétences (Domaine > Famille > ...), remplace la classification
/// à plat de CategorieCompetence pour le nouveau référentiel Skill. N'affecte pas
/// CategorieCompetence, qui reste utilisée par Competence (migration additive uniquement).
/// </summary>
public class SkillCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }
    public SkillCategory? ParentCategory { get; set; }
    public ICollection<SkillCategory>? SousCategories { get; set; }

    public ICollection<Skill>? Skills { get; set; }
}
