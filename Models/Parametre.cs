namespace SIRH.EY.Models
{
    public class Parametre
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
        public string TypeValeur { get; set; } = "string";
        public string? Description { get; set; }
        public bool EstModifiable { get; set; } = true;
        public DateTime DerniereModification { get; set; } = DateTime.Now;
    }
}