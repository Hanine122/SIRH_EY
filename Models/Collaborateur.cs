using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Collaborateur
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;

    [Required]
    public string Prenom { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Grade { get; set; }
    public string? Departement { get; set; }
    public string? Poste { get; set; }
    public int? ManagerId { get; set; }

    public DateTime DateEmbauche { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;

    public Collaborateur? Manager { get; set; }
    public ICollection<Collaborateur>? Equipe { get; set; }
    public ICollection<Competence>? Competences { get; set; }
    public ICollection<Inscription>? Inscriptions { get; set; }
}