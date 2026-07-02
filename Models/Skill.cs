using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Compétence canonique — référentiel central de la Skill Ontology. Contrairement à
/// Competence.Nom (chaîne libre par collaborateur) ou CompetenceRequiseParPoste.Competence
/// (chaîne libre par poste), Skill est l'entité de référence unique vers laquelle SkillAlias
/// fait converger les variantes orthographiques. Migration additive uniquement : Competence,
/// CompetenceRequiseParPoste et Formation ne sont pas modifiées dans cette passe — le pont
/// (ex. Competence.SkillId) est un chantier séparé, volontairement hors scope ici.
/// </summary>
public class Skill
{
    public int Id { get; set; }

    /// <summary>Nom de référence canonique (ex. "Power BI"), distinct des variantes en SkillAlias.</summary>
    [Required]
    [StringLength(150)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? SkillCategoryId { get; set; }
    public SkillCategory? SkillCategory { get; set; }

    public bool Actif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.Now;

    public ICollection<SkillAlias>? Aliases { get; set; }
    public ICollection<SkillLevel>? Levels { get; set; }
    public ICollection<SkillCriticality>? Criticalities { get; set; }
    public ICollection<SkillVersion>? Versions { get; set; }

    /// <summary>Relations où ce skill est la source (ex. prérequis pour un autre skill).</summary>
    public ICollection<SkillRelation>? RelationsSource { get; set; }
    /// <summary>Relations où ce skill est la cible.</summary>
    public ICollection<SkillRelation>? RelationsCible { get; set; }
}
