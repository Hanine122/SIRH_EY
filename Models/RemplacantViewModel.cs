using System.Collections.Generic;

namespace SIRH.EY.Models
{
    public class RemplacantViewModel
    {
        public Collaborateur Collaborateur { get; set; }
        public int ScoreCompatibilite { get; set; }
        public List<string> CompetencesManquantes { get; set; }
        public List<Formation> FormationsRecommandees { get; set; }
    }
}