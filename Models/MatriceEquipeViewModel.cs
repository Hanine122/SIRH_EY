namespace SIRH.EY.Models
{
    public class MatriceEquipeViewModel
    {
        public List<Collaborateur> Collaborateurs { get; set; }
        public Dictionary<int, List<Competence>> CompetencesParCollaborateur { get; set; }
    }
}