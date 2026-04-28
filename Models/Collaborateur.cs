using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Collaborateur
{
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; } = string.Empty;

    [Required]
    public string Prenom { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;
public string? Grade { get; set; } // "Junior", "Senior", "Manager"
    public string? Departement { get; set; }
    public string? Poste { get; set; }
    public int? ManagerId { get; set; }
    public DateTime DateEmbauche { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;

    // Propriétés de navigation
    public Collaborateur? Manager { get; set; }
    public ICollection<Collaborateur>? Equipe { get; set; }
    public ICollection<Competence>? Competences { get; set; }
    public ICollection<Inscription>? Inscriptions { get; set; }
}