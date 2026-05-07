using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class CategorieCompetence
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;

    // Navigation
    public ICollection<Competence>? Competences { get; set; }
}