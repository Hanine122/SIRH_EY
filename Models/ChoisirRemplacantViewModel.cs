namespace SIRH.EY.Models
{
    public class ChoisirRemplacantViewModel
    {
        public Collaborateur Partant { get; set; } = null!;
        public List<string> CompetencesRequises { get; set; } = new();
        public List<CandidatDetail> Candidats { get; set; } = new();
    }

    public class CandidatDetail
    {
        public int Id { get; set; }
        public string Prenom { get; set; } = "";
        public string Nom { get; set; } = "";
        public string Email { get; set; } = "";
        public string Poste { get; set; } = "";
        public string Departement { get; set; } = "";
        public string Grade { get; set; } = "";
        /// <summary>Compétences du poste (partant) absentes chez le candidat.</summary>
        public List<string> CompetencesManquantes { get; set; } = new();
        public List<string> FormationsRecommande { get; set; } = new();
        /// <summary>Nombre de compétences du partant également détenues par le candidat (même nom).</summary>
        public int NbCompetencesCommunes { get; set; }
        /// <summary>Autre département mais recouvrement de compétences (profil transversal).</summary>
        public bool ProfilTransversal { get; set; }
    }
}