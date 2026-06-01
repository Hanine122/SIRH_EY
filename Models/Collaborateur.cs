using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIRH.EY.Models;

public class Collaborateur
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;

    [Required]
    public string Prenom { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public DateTime? DateNaissance { get; set; }
    public string? Genre { get; set; }
    public string? Nationalite { get; set; }
    public string? EtatCivil { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? TelephonePersonnel { get; set; }
    public string? ContactUrgence { get; set; }

    public string? Matricule { get; set; }

    // ── Legacy string fields (kept for backward-compat with existing views) ──
    public string? Grade { get; set; }
    public string? Departement { get; set; }
    public string? Poste { get; set; }
    public string? BusinessUnit { get; set; }
    public string? Localisation { get; set; }

    // ── Self-referential manager ──
    public int? ManagerId { get; set; }

    // ── HR Master Data foreign keys ──
    [Display(Name = "Département")]
    public int? DepartmentId { get; set; }
    public Department? DepartmentRef { get; set; }

    [Display(Name = "Sous-département")]
    public int? SubDepartmentId { get; set; }
    public SubDepartment? SubDepartmentRef { get; set; }

    [Display(Name = "Poste (référentiel)")]
    public int? PositionId { get; set; }
    public Position? PositionRef { get; set; }

    [Display(Name = "Grade (référentiel)")]
    public int? GradeId { get; set; }
    public GradeEntity? GradeRef { get; set; }

    [Display(Name = "Business Unit")]
    public int? BusinessUnitId { get; set; }
    public BusinessUnitEntity? BusinessUnitRef { get; set; }

    [Display(Name = "Localisation")]
    public int? LocationId { get; set; }
    public LocationEntity? LocationRef { get; set; }

    [Display(Name = "Type de contrat")]
    public int? ContractTypeId { get; set; }
    public ContractType? ContractTypeRef { get; set; }

    // ── Other HR fields ──
    public string? RoleRH { get; set; }
    public string? TypeContrat { get; set; }
    public string? NiveauHierarchique { get; set; }
    public DateTime? DatePrisePoste { get; set; }
    public string? FormationsObligatoires { get; set; }
    public int? NiveauPreparationSuccession { get; set; }
    public string? PotentielCarriere { get; set; }

    public DateTime DateEmbauche { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;
    public StatutCollaborateur Statut { get; set; } = StatutCollaborateur.Actif;

    public int Anciennete => DateTime.Today < DateEmbauche.Date
        ? 0
        : (int)((DateTime.Today - DateEmbauche.Date).TotalDays / 365.25);

    public Collaborateur? Manager { get; set; }
    public ICollection<Collaborateur>? Equipe { get; set; }
    public ICollection<Competence>? Competences { get; set; }
    public ICollection<Inscription>? Inscriptions { get; set; }
}
