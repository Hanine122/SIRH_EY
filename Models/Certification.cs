using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Certification
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Organisme { get; set; }

    [MaxLength(100)]
    public string? Domaine { get; set; }

    [MaxLength(50)]
    public string? CodeExamen { get; set; }

    public bool EstReconnue { get; set; } = true;

    public ICollection<CollaborateurCertification>? CollaborateurCertifications { get; set; }
}
