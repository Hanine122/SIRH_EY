using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

public class Formation
{
    public int Id { get; set; }

    [Required]
    public string Titre { get; set; } = string.Empty;

    public string? Formateur { get; set; }
    public int DureeHeures { get; set; }
    public int CapaciteMax { get; set; } = 20;
    public int PlacesPrises { get; set; } = 0;
    public string? Categorie { get; set; }
    public DateTime DateDebut { get; set; }
    
    // Nouvelle propriété pour l'organisme
    public string? Organisme { get; set; } // "Interne" ou "Externe"
public string? CompetenceVisee { get; set; } // ex: "Leadership", "Gestion de Projet"
    public ICollection<Inscription>? Inscriptions { get; set; }
    
}