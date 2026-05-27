namespace SIRH.EY.Models;

/// <summary>
/// Modèle pour le catalogue de compétences par secteur EY
/// </summary>
public class CompetenceCatalogViewModel
{
    public int CollaborateurId { get; set; }
    public string CollaborateurNom { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public string? Poste { get; set; }
    public string? Departement { get; set; }

    // Compétences déjà acquises par le collaborateur
    public List<Competence> CompetencesExistantes { get; set; } = new();

    // Catégories EY (onglets)
    public List<SecteurEY> Secteurs { get; set; } = new();

    // Compétences sélectionnées (checkboxes)
    public List<int> CompetencesSelectionneesIds { get; set; } = new();
}

/// <summary>
/// Représente un secteur EY avec ses compétences associées
/// </summary>
public class SecteurEY
{
    public string Id { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-briefcase";
    public List<CompetenceCardViewModel> Competences { get; set; } = new();
}

/// <summary>
/// Modèle pour une carte de compétence dans le catalogue
/// </summary>
public class CompetenceCardViewModel
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Categorie { get; set; }
    public int NiveauRequis { get; set; }
    public string? PosteCible { get; set; }
    public bool EstDejaAcquise { get; set; }
    public int? NiveauActuel { get; set; }
    public string TypeCompetence { get; set; } = "Technique"; // Technique, Fonctionnel, Transverse
}