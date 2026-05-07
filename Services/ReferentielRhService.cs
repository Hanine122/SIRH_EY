using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class ReferentielRhService : IReferentielRhService
{
    private readonly ApplicationDbContext _context;

    public ReferentielRhService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<string>> GetDepartementsAsync()
    {
        var departements = await _context.Collaborateurs
            .AsNoTracking()
            .Where(c => c.Departement != null && c.Departement != "")
            .Select(c => c.Departement!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        return departements.Count == 0 ? GetDepartementsFallback() : departements;
    }

    public async Task<IReadOnlyList<string>> GetPostesByDepartementAsync(string? departement)
    {
        if (string.IsNullOrWhiteSpace(departement))
            return Array.Empty<string>();

        var postes = await _context.Collaborateurs
            .AsNoTracking()
            .Where(c => c.Departement == departement && c.Poste != null && c.Poste != "")
            .Select(c => c.Poste!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        return postes.Count == 0 && PostesFallback.TryGetValue(departement, out var fallback)
            ? fallback
            : postes;
    }

    public async Task<IReadOnlyList<CompetenceRequiseParPoste>> GetCompetencesDisponiblesParGradeAsync(string? grade)
    {
        var niveauCible = CompetenceRules.GetNiveauCibleParGrade(grade ?? "");

        // Récupérer toutes les compétences requises filtrées par niveau
        var allCompetences = await _context.CompetencesRequisesParPoste
            .AsNoTracking()
            .Where(c => c.NiveauRequis <= niveauCible)
            .ToListAsync();

        // Grouper en mémoire pour éviter l'erreur EF Core
        var competences = allCompetences
            .GroupBy(c => c.Competence)
            .Select(g => g.OrderByDescending(c => c.NiveauRequis).First())
            .OrderBy(c => c.Competence)
            .ToList();

        if (competences.Count > 0)
            return competences;

        return CompetencesFallback
            .Where(c => c.NiveauRequis <= niveauCible)
            .OrderBy(c => c.Competence)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesCompetencesAsync()
    {
        var categories = await _context.CategoriesCompetences
            .AsNoTracking()
            .Select(c => c.Nom)
            .OrderBy(c => c)
            .ToListAsync();

        return categories.Count == 0
            ? new[] { "Audit", "Data", "Leadership", "Management", "Metier", "Outils", "Risk", "Soft skills" }
            : categories;
    }

    private static IReadOnlyList<string> GetDepartementsFallback() =>
        new[] { "Audit", "Tax", "Consulting", "Advisory", "Risk", "RH" };

    private static readonly Dictionary<string, IReadOnlyList<string>> PostesFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Audit"] = new[] { "Junior Auditor", "Senior Auditor", "Audit Manager" },
        ["Tax"] = new[] { "Consultant", "Data Analyst", "Tax Manager" },
        ["Consulting"] = new[] { "Consultant", "Senior Consultant", "Manager" },
        ["Advisory"] = new[] { "Consultant", "Senior Consultant", "Advisory Manager" },
        ["Risk"] = new[] { "Risk Analyst", "Risk Manager" },
        ["RH"] = new[] { "HR Officer", "HR Director" }
    };

    private static readonly IReadOnlyList<CompetenceRequiseParPoste> CompetencesFallback = new[]
    {
        new CompetenceRequiseParPoste { Competence = "Communication", Poste = "Referentiel", NiveauRequis = 3 },
        new CompetenceRequiseParPoste { Competence = "Excel avance", Poste = "Referentiel", NiveauRequis = 3 },
        new CompetenceRequiseParPoste { Competence = "Gestion de projet", Poste = "Referentiel", NiveauRequis = 4 },
        new CompetenceRequiseParPoste { Competence = "Leadership", Poste = "Referentiel", NiveauRequis = 5 },
        new CompetenceRequiseParPoste { Competence = "Power BI", Poste = "Referentiel", NiveauRequis = 4 },
        new CompetenceRequiseParPoste { Competence = "SQL", Poste = "Referentiel", NiveauRequis = 3 }
    };
}
