using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class EvaluationSuiviFormation
{
    public int Id { get; set; }

    [Required]
    public int InscriptionId { get; set; }
    public Inscription? Inscription { get; set; }

    [Range(1, 5)]
    public int NoteApplicationCompetences { get; set; }

    [Range(1, 5)]
    public int NoteImpactBusiness { get; set; }

    [MaxLength(1000)]
    public string? ExemplesConcrets { get; set; }

    [MaxLength(1000)]
    public string? Commentaire { get; set; }

    public DateTime DateEvaluation { get; set; } = DateTime.Now;

    // ── J+90 manager validation ──────────────────────────────────────────────
    // Mirrors EvaluationCompetence.ValidationManager (4-eyes pattern already used
    // for auto-évaluation). Only once ValidationManager is true does the manager's
    // NiveauValide get written back to Competence.NiveauActuel.
    [Range(1, 5)]
    public int? NiveauValide { get; set; }

    public bool ValidationManager { get; set; } = false;

    public DateTime? DateValidationManager { get; set; }
}
