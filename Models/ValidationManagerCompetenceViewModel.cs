using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class ValidationManagerCompetenceViewModel
{
    public int CompetenceId { get; set; }
    public int CollaborateurId { get; set; }

    public string CompetenceNom { get; set; } = string.Empty;
    public string CollaborateurNom { get; set; } = string.Empty;
    public string? Poste { get; set; }
    public string? Categorie { get; set; }

    public int SeuilRh { get; set; }
    public int AutoEvaluationCollaborateur { get; set; }

    [Range(0, 100)]
    [Display(Name = "Évaluation du manager")]
    public int EvaluationManager { get; set; }

    [Display(Name = "Validation manager")]
    public bool ValidationManager { get; set; }

    public string? CommentaireCollaborateur { get; set; }

    [StringLength(1000)]
    [Display(Name = "Commentaire manager")]
    public string? CommentaireManager { get; set; }
}
