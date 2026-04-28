using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class AutoEvaluationCompetenceViewModel
{
    public int CompetenceId { get; set; }
    public int CollaborateurId { get; set; }

    public string CompetenceNom { get; set; } = string.Empty;
    public string CollaborateurNom { get; set; } = string.Empty;
    public string? Poste { get; set; }
    public string? Categorie { get; set; }

    [Range(0, 100)]
    [Display(Name = "Objectif RH")]
    public int SeuilRh { get; set; }

    [Range(0, 100)]
    [Display(Name = "Votre auto-évaluation")]
    public int AutoEvaluationCollaborateur { get; set; }

    public int? EvaluationManager { get; set; }
    public bool ValidationManager { get; set; }

    [StringLength(1000)]
    [Display(Name = "Commentaire collaborateur")]
    public string? CommentaireCollaborateur { get; set; }

    public string? CommentaireManager { get; set; }
}
