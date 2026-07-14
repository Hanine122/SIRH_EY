using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Formation
{
    public int Id { get; set; }

    [Required]
    public string Titre { get; set; } = string.Empty;

    public string? Formateur { get; set; }
    public int DureeHeures { get; set; }
    public int CapaciteMax { get; set; } = 20;
    public int PlacesPrises { get; set; } = 0;
    public string? Categorie { get; set; }
    public DateTime DateDebut { get; set; }

    [Display(Name = "Organisme")]
    public string? Organisme { get; set; }

    [Display(Name = "Compétence développée")]
    public string? CompetenceVisee { get; set; }

    public string? DepartementCible { get; set; }
    public string? MetierCible { get; set; }
    public string? PosteCible { get; set; }
    public string? DomaineCompetence { get; set; }
    public string? NiveauDifficulte { get; set; }
    public bool EstCertifiante { get; set; } = false;

    // ── Learning enrichment fields ───────────────────────────────────────────

    [Display(Name = "Plateforme")]
    [MaxLength(100)]
    public string? Plateforme { get; set; }     // "Udemy", "Microsoft Learn", "Coursera", "AWS Skill Builder", "LinkedIn Learning", "EY Learning"

    [Display(Name = "URL externe")]
    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Compétences requises (prérequis)")]
    [MaxLength(500)]
    public string? CompetencesRequises { get; set; }    // comma-separated

    [Display(Name = "Certification obtenue")]
    [MaxLength(200)]
    public string? CertificationNom { get; set; }

    [Display(Name = "Support PDF (URL)")]
    [MaxLength(500)]
    public string? SupportPdfUrl { get; set; }

    [Display(Name = "Email du mentor")]
    [MaxLength(200)]
    public string? MentorEmail { get; set; }

    [Display(Name = "Formation stratégique")]
    public bool EstStrategique { get; set; } = false;

    [Display(Name = "Forte demande")]
    public bool EstForteDemande { get; set; } = false;

    // ── Skill bridge (real relationship, replaces text-matching on CompetenceVisee) ──
    public int? SkillId { get; set; }
    public Skill? Skill { get; set; }

    // ── Navigations ──────────────────────────────────────────────────────────
    public ICollection<Inscription>? Inscriptions { get; set; }
}
