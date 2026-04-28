namespace SIRH.EY.Models
{
    public class DepartViewModel
    {
        public Collaborateur CollaborateurPartant { get; set; }
        public Collaborateur CollaborateurRemplacant { get; set; }
        public List<string> CompetencesManquantes { get; set; }
        public List<string> FormationsRecommande { get; set; }
    }
}