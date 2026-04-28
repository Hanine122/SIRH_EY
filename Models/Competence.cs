using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Competence
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;   // ex: "Leadership"

    public string? Categorie { get; set; }            // "Métier", "Transversale"

    [Range(1,5)]
    public int NiveauActuel { get; set; } = 1;

    [Range(1,5)]
    public int NiveauCible { get; set; } = 3;

    public DateTime DateEvaluation { get; set; } = DateTime.Now;

    // Clé étrangère vers Collaborateur
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public EvaluationCompetence? EvaluationCompetence { get; set; }
}