using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Base class that adds standard audit columns to every HR master data entity.
/// EF Core maps each derived entity to its own table (no TPH) because
/// every derived type has its own DbSet registered in ApplicationDbContext.
/// </summary>
public abstract class AuditableEntity
{
    [MaxLength(256)]
    [Display(Name = "Créé par")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Créé le")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    [Display(Name = "Modifié par")]
    public string? UpdatedBy { get; set; }

    [Display(Name = "Modifié le")]
    public DateTime? UpdatedAt { get; set; }
}
