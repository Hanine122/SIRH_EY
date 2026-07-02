namespace SIRH.EY.Models;

public enum CriticalityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Strategic = 3
}

/// <summary>
/// Criticité métier d'un Skill (remplace le calcul ad hoc "compétence rare" recalculé
/// différemment dans ReportingController et RhInsightsController par une évaluation persistante
/// et gouvernée). Historisée : DateEvaluation permet de conserver plusieurs évaluations dans le
/// temps plutôt que d'écraser la précédente.
/// </summary>
public class SkillCriticality
{
    public int Id { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    public CriticalityLevel Niveau { get; set; } = CriticalityLevel.Medium;

    public string? Justification { get; set; }

    /// <summary>Département/poste concerné, null = criticité globale.</summary>
    public string? Perimetre { get; set; }

    public DateTime DateEvaluation { get; set; } = DateTime.Now;
}
