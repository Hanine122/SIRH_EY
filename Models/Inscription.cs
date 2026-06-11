using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Inscription
{
    public int Id { get; set; }
    public DateTime DateInscription { get; set; } = DateTime.Now;
    public bool Terminee { get; set; } = false;
    public virtual ICollection<EvaluationCompetence> EvaluationsFormation { get; set; }

    [Display(Name = "Date d'examen")]
    public DateTime? DateExamen { get; set; }

    [Display(Name = "Progression (%)")]
    public int Progression { get; set; } = 0;

    // ── Certification enrichment ─────────────────────────────────────────────

    [Display(Name = "Date de complétion")]
    public DateTime? DateCompletion { get; set; }

    [Display(Name = "Date d'expiration du certificat")]
    public DateTime? DateExpiration { get; set; }

    [Display(Name = "Source de certification")]
    [MaxLength(100)]
    public string? SourceCertification { get; set; }    // "Udemy", "EY Learning", "Microsoft Learn", etc.

    // ── Foreign keys ─────────────────────────────────────────────────────────
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    public int FormationId { get; set; }
    public Formation? Formation { get; set; }
}