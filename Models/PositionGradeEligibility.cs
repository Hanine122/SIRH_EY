using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Defines which grades are eligible for a given position.
/// A position can have a minimum required grade and optional maximum grade.
/// </summary>
public class PositionGradeEligibility
{
    public int Id { get; set; }

    [Required]
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    [Required]
    [Display(Name = "Grade")]
    public int GradeEntityId { get; set; }
    public GradeEntity GradeEntity { get; set; } = null!;

    [Display(Name = "Grade minimum requis")]
    public bool IsMinimum { get; set; } = false;
}
