using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Competence
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;   

    public int? CategorieCompetenceId { get; set; }
    public CategorieCompetence? CategorieCompetence { get; set; }      
          

    [Range(1,5)]
    public int NiveauActuel { get; set; } = 1;

    [Range(1,5)]
    public int NiveauCible { get; set; } = 3;

    public DateTime DateEvaluation { get; set; } = DateTime.Now;


    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public EvaluationCompetence? EvaluationCompetence { get; set; }
}