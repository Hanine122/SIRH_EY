namespace SIRH.EY.Models
{
    public class EvaluationHistorique
    {
        public int Id { get; set; }
        public int CompetenceId { get; set; }
        public int NiveauAncien { get; set; }
        public int NiveauNouveau { get; set; }
        public DateTime DateChangement { get; set; }
        public string Raison { get; set; } = string.Empty; // "Formation", "Manuel"
        public virtual Competence? Competence { get; set; }
    }
}