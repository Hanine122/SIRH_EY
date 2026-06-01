using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>Junction: a Formation that is mandatory for a given Position.</summary>
public class PositionMandatoryFormation
{
    public int Id { get; set; }

    [Required]
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    [Required]
    public int FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    /// <summary>Must be completed within X months of taking the position. Null = no deadline.</summary>
    [Display(Name = "Délai (mois)")]
    [Range(1, 60)]
    public int? DeadlineMonths { get; set; }
}
