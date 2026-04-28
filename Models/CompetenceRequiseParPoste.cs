using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models
{
    public class CompetenceRequiseParPoste
    {
        public int Id { get; set; }
        [Required]
        public string Poste { get; set; } = string.Empty; // ex: "Auditeur", "Consultant"
        [Required]
        public string Competence { get; set; } = string.Empty;
        [Range(1,5)]
        public int NiveauRequis { get; set; } = 3;
    }
}