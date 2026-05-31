using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class GradeEntity
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Nom du grade")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Niveau hiérarchique")]
    [Range(1, 10)]
    public int Level { get; set; } = 1;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
}
