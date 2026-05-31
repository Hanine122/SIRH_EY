using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class BusinessUnitEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nom")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
}
