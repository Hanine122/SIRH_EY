using SIRH.EY.Models;

namespace SIRH.EY.Services;

public interface IReferentielRhService
{
    Task<IReadOnlyList<string>> GetDepartementsAsync();
    Task<IReadOnlyList<string>> GetPostesByDepartementAsync(string? departement);
    Task<IReadOnlyList<CompetenceRequiseParPoste>> GetCompetencesDisponiblesParGradeAsync(string? grade);
    Task<IReadOnlyList<string>> GetCategoriesCompetencesAsync();
}
