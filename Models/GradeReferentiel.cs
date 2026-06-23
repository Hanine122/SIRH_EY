using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class GradeReferentiel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Grade { get; set; } = string.Empty;

    // Score moyen minimum de compétences (sur 5) pour accéder à ce grade
    public double NiveauMinCompetences { get; set; }

    // Ancienneté minimum en années dans le grade actuel avant promotion
    public int AncienneteMinAns { get; set; }

    // Nombre minimum d'implémentations CRM/ERP (Phase 3 field)
    public int NombreImplementationsMin { get; set; }

    // Années d'expérience domaine minimum
    public int ExperienceDomainMinAns { get; set; }

    // Grade suivant dans la hiérarchie EY
    [MaxLength(50)]
    public string? GradeSuivant { get; set; }

    public string? Description { get; set; }

    // Niveau hiérarchique (1 = Junior … 6 = Partner)
    public int Niveau { get; set; }
}
