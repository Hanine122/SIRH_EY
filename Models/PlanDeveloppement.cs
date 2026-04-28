using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models
{
    public class PlanDeveloppement
    {
        public int Id { get; set; }
        public int CollaborateurId { get; set; }
        public int FormationId { get; set; }
        public DateTime DateRecommandation { get; set; } = DateTime.Now;
        public string Statut { get; set; } = "À faire"; // À faire, En cours, Validé
        public string? Commentaire { get; set; }
        public virtual Collaborateur? Collaborateur { get; set; }
        public virtual Formation? Formation { get; set; }
    }
}