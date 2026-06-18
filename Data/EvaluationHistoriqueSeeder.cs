using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Data
{
    public static class EvaluationHistoriqueSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.EvaluationsHistoriques.AnyAsync())
                return;

            // Tous les collaborateurs actifs avec au moins une compétence,
            // pas une liste de noms fixe — couvre l'intégralité du SIRH.
            var competences = await context.Competences
                .Include(c => c.Collaborateur)
                .Where(c => c.Collaborateur != null && c.Collaborateur.Actif)
                .ToListAsync();

            var historique = new List<EvaluationHistorique>();
            var dateBase = DateTime.Now;
            var random = new Random(42);

            foreach (var comp in competences)
            {
                var niveauActuel = comp.NiveauActuel;

                for (int i = 3; i >= 1; i--)
                {
                    var niveauPasse = Math.Max(1, niveauActuel - i);
                    historique.Add(new EvaluationHistorique
                    {
                        CompetenceId = comp.Id,
                        NiveauAncien = Math.Max(1, niveauPasse - 1),
                        NiveauNouveau = niveauPasse,
                        DateChangement = dateBase.AddMonths(-i * 3),
                        Raison = i == 3 ? "Évaluation initiale" : "Formation"
                    });
                }
            }

            context.EvaluationsHistoriques.AddRange(historique);
            await context.SaveChangesAsync();
        }
    }
}