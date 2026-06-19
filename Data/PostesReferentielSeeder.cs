using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public static class PostesReferentielSeeder
{
    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        const string seedVersion = "POSTES_REFERENTIEL_V1_2026_06";
        if (await ctx.Parametres.AnyAsync(p => p.Code == seedVersion))
            return;

        var entries = new List<CompetenceRequiseParPoste>
        {
            // Partner — maîtrise stratégique et business (niveau 5 requis)
            new() { Poste = "Partner", Competence = "Leadership stratégique",   NiveauRequis = 5 },
            new() { Poste = "Partner", Competence = "Business Development",     NiveauRequis = 5 },
            new() { Poste = "Partner", Competence = "Stakeholder management",   NiveauRequis = 5 },
            new() { Poste = "Partner", Competence = "Communication executive",  NiveauRequis = 5 },

            // Director — leadership opérationnel et pilotage stratégique (niveau 4-5)
            new() { Poste = "Director", Competence = "Leadership",                        NiveauRequis = 5 },
            new() { Poste = "Director", Competence = "Conseil stratégique",               NiveauRequis = 5 },
            new() { Poste = "Director", Competence = "Gestion de projet",                 NiveauRequis = 4 },
            new() { Poste = "Director", Competence = "Analyse & résolution de problèmes", NiveauRequis = 4 },

            // Senior Manager — encadrement d'équipe et expertise métier (niveau 4)
            new() { Poste = "Senior Manager", Competence = "Leadership",                        NiveauRequis = 4 },
            new() { Poste = "Senior Manager", Competence = "Gestion de projet",                 NiveauRequis = 4 },
            new() { Poste = "Senior Manager", Competence = "Stakeholder management",            NiveauRequis = 4 },
            new() { Poste = "Senior Manager", Competence = "Analyse & résolution de problèmes", NiveauRequis = 4 },
        };

        ctx.CompetencesRequisesParPoste.AddRange(entries);
        ctx.Parametres.Add(new Parametre { Code = seedVersion, Valeur = "done" });
        await ctx.SaveChangesAsync();
    }
}
