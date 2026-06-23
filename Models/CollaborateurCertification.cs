namespace SIRH.EY.Models;

public class CollaborateurCertification
{
    public int Id { get; set; }

    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public int CertificationId { get; set; }
    public Certification? Certification { get; set; }

    public DateTime? DateObtention { get; set; }
    public DateTime? DateExpiration { get; set; }

    // "Active" | "Expirée" | "En cours"
    public string? Statut { get; set; } = "Active";
}
