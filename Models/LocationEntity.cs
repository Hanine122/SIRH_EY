using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class LocationEntity : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Nom du site")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Ville")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "Pays")]
    public string? Country { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
}
