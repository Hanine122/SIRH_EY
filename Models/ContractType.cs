using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class ContractType : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    [Display(Name = "Libellé")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Code")]
    public string? Code { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>Durée maximale en mois (null = illimitée, ex: CDI).</summary>
    [Display(Name = "Durée max (mois)")]
    [Range(1, 240)]
    public int? MaxDurationMonths { get; set; }

    [Display(Name = "Actif")]
    public bool IsActive { get; set; } = true;

    public ICollection<Collaborateur> Collaborateurs { get; set; } = new List<Collaborateur>();
}
