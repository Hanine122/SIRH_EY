namespace SIRH.EY.Models;

public enum SkillRelationType
{
    /// <summary>SourceSkill nécessite TargetSkill au préalable.</summary>
    Prerequisite = 0,
    /// <summary>Relation faible, sans dépendance directe.</summary>
    RelatedTo = 1,
    /// <summary>SourceSkill est une sous-compétence de TargetSkill.</summary>
    PartOf = 2,
    /// <summary>SourceSkill remplace/obsolète TargetSkill.</summary>
    Supersedes = 3
}

/// <summary>
/// Arête du graphe d'ontologie entre deux Skill (prérequis, parenté, obsolescence…).
/// </summary>
public class SkillRelation
{
    public int Id { get; set; }

    public int SourceSkillId { get; set; }
    public Skill? SourceSkill { get; set; }

    public int TargetSkillId { get; set; }
    public Skill? TargetSkill { get; set; }

    public SkillRelationType Type { get; set; }
}
