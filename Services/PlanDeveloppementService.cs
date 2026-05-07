using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class PlanDeveloppementService : IPlanDeveloppementService
{
    private readonly ApplicationDbContext _context;

    public PlanDeveloppementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlanDeveloppementResult> GenererPourCollaborateurAsync(int collaborateurId)
    {
        var competencesEnEcart = await _context.Competences
            .Where(c => c.CollaborateurId == collaborateurId && c.NiveauActuel < c.NiveauCible)
            .ToListAsync();

        if (!competencesEnEcart.Any())
            return new PlanDeveloppementResult(0, "Aucun ecart de competence a traiter : le profil est aligne avec ses objectifs.");

        var formations = await _context.Formations.ToListAsync();
        var plansExistants = await _context.PlansDeveloppement
            .Where(p => p.CollaborateurId == collaborateurId)
            .Select(p => p.FormationId)
            .ToListAsync();

        var nouveauxPlans = new List<PlanDeveloppement>();
        foreach (var competence in competencesEnEcart)
        {
            var formation = TrouverFormationPourCompetence(formations, plansExistants, nouveauxPlans, competence.Nom);
            if (formation == null) continue;

            nouveauxPlans.Add(new PlanDeveloppement
            {
                CollaborateurId = collaborateurId,
                FormationId = formation.Id,
                Statut = "A faire",
                Commentaire = $"Recommande pour combler l'ecart sur {competence.Nom} ({competence.NiveauActuel}/{competence.NiveauCible})."
            });
        }

        if (!nouveauxPlans.Any())
            return new PlanDeveloppementResult(0, "Aucune nouvelle formation correspondante trouvee pour les ecarts actuels.");

        _context.PlansDeveloppement.AddRange(nouveauxPlans);
        await _context.SaveChangesAsync();

        return new PlanDeveloppementResult(nouveauxPlans.Count, $"{nouveauxPlans.Count} recommandation(s) ajoutee(s) au plan de developpement.");
    }

    private static Formation? TrouverFormationPourCompetence(
        IEnumerable<Formation> formations,
        IReadOnlyCollection<int> plansExistants,
        IReadOnlyCollection<PlanDeveloppement> nouveauxPlans,
        string competenceNom)
    {
        var formationsDisponibles = formations
            .Where(f => !plansExistants.Contains(f.Id) && !nouveauxPlans.Any(p => p.FormationId == f.Id))
            .ToList();

        return formationsDisponibles.FirstOrDefault(f =>
                   !string.IsNullOrWhiteSpace(f.CompetenceVisee) &&
                   f.CompetenceVisee.Trim().Equals(competenceNom.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? formationsDisponibles.FirstOrDefault(f =>
                   (f.Titre ?? "").Contains(competenceNom, StringComparison.OrdinalIgnoreCase));
    }
}
