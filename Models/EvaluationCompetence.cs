using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class EvaluationCompetence
{
    public int Id { get; set; }

    [Required]
    public int CompetenceId { get; set; }
    public int? InscriptionId { get; set; } 
    public virtual Inscription? Inscription { get; set; }

    [Range(0, 100)]
    public int SeuilRh { get; set; }

    [Range(0, 100)]
    public int AutoEvaluationCollaborateur { get; set; }

    [Range(0, 100)]
    public int? EvaluationManager { get; set; }

    public bool ValidationManager { get; set; }

    public DateTime? DateAutoEvaluation { get; set; }

    public DateTime? DateValidationManager { get; set; }

    [StringLength(1000)]
    public string? CommentaireCollaborateur { get; set; }

    [StringLength(1000)]
    public string? CommentaireManager { get; set; }

    public Competence? Competence { get; set; }
}
