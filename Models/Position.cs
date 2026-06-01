using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Position : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Titre du poste")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "Sous-département")]
    public int? SubDepartmentId { get; set; }
    public SubDepartment? SubDepartment { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    // ── Relationships ──────────────────────────────────────────────────────────
    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
    public ICollection<PositionRequiredCompetence> RequiredCompetences { get; set; } = new List<PositionRequiredCompetence>();
    public ICollection<PositionMandatoryFormation> MandatoryFormations { get; set; } = new List<PositionMandatoryFormation>();
    public ICollection<PositionGradeEligibility> GradeEligibilities { get; set; } = new List<PositionGradeEligibility>();
}
