using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class SubDepartment : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nom")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Required]
    [Display(Name = "Département")]
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
}
