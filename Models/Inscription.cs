namespace SIRH.EY.Models;

public class Inscription
{
    public int Id { get; set; }
    public DateTime DateInscription { get; set; } = DateTime.Now;
    public bool Terminee { get; set; } = false;
    public virtual ICollection<EvaluationCompetence> EvaluationsFormation { get; set; }
    // Ajouté pour la planification d'examen
    public DateTime? DateExamen { get; set; }
    
    // Optionnel : suivi de la progression (0-100)
    public int Progression { get; set; } = 0;

    // Clés étrangères
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public int FormationId { get; set; }
    public Formation? Formation { get; set; }
}