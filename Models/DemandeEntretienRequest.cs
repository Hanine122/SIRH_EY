namespace SIRH.EY.Models
{
    public class DemandeEntretienRequest
    {
        public int PartantId { get; set; }
        public List<int> CandidatsIds { get; set; }
        public string Commentaire { get; set; }
    }
}