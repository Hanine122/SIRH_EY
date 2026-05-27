namespace SIRH.EY.Models;

/// <summary>
/// Définition statique d'un secteur EY avec ses compétences de référence
/// </summary>
public class SecteurEYDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-briefcase";
    public IEnumerable<SecteurCompetenceDefinition> Competences { get; set; } = Enumerable.Empty<SecteurCompetenceDefinition>();
}

/// <summary>
/// Définition d'une compétence dans un secteur EY
/// </summary>
public class SecteurCompetenceDefinition
{
    public string Nom { get; set; } = string.Empty;
    public string? Categorie { get; set; }
    public int NiveauRequis { get; set; }
    public string? PosteCible { get; set; }
}