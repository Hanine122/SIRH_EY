using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Fenêtre temporelle d'une campagne de revue talent (ex. "Revue S2 2026").
/// Encadre les TalentEvaluation rattachées : hors cycle ouvert, pas de nouvelle soumission.
/// </summary>
public class ReviewCycle
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nom { get; set; } = string.Empty;

    public DateTime DateDebut { get; set; } = DateTime.Now;
    public DateTime DateFin { get; set; } = DateTime.Now.AddMonths(1);

    public ReviewCycleStatus Statut { get; set; } = ReviewCycleStatus.Open;

    /// <summary>Département/BU ciblé par le cycle, null = périmètre global.</summary>
    public string? Perimetre { get; set; }

    public ICollection<TalentEvaluation>? TalentEvaluations { get; set; }
}

public enum ReviewCycleStatus
{
    Open = 0,
    Closed = 1
}
