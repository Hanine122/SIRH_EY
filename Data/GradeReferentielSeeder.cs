using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public static class GradeReferentielSeeder
{
    private const string SeedVersion = "GRADE_REFERENTIEL_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        // Seuils issus de la matrice §3.6 du PDF AD Ports CRM + politique EY grades
        var grades = new List<GradeReferentiel>
        {
            new()
            {
                Grade                    = "Junior",
                Niveau                   = 1,
                NiveauMinCompetences     = 1.5,
                AncienneteMinAns         = 0,
                NombreImplementationsMin = 0,
                ExperienceDomainMinAns   = 0,
                GradeSuivant             = "Senior",
                Description              = "Première expérience professionnelle. Accompagnement senior requis sur toutes les missions.",
            },
            new()
            {
                Grade                    = "Senior",
                Niveau                   = 2,
                NiveauMinCompetences     = 3.0,
                AncienneteMinAns         = 2,
                NombreImplementationsMin = 1,
                ExperienceDomainMinAns   = 1,
                GradeSuivant             = "Manager",
                Description              = "Autonomie technique confirmée. Capacité à piloter un module ou un workstream sur un projet CRM.",
            },
            new()
            {
                Grade                    = "Manager",
                Niveau                   = 3,
                NiveauMinCompetences     = 3.8,
                AncienneteMinAns         = 4,
                NombreImplementationsMin = 3,
                ExperienceDomainMinAns   = 3,
                GradeSuivant             = "Senior Manager",
                Description              = "Responsabilité d'équipe et de livraison. Gestion des parties prenantes client et supervision technique.",
            },
            new()
            {
                Grade                    = "Senior Manager",
                Niveau                   = 4,
                NiveauMinCompetences     = 4.0,
                AncienneteMinAns         = 6,
                NombreImplementationsMin = 5,
                ExperienceDomainMinAns   = 5,
                GradeSuivant             = "Director",
                Description              = "Leadership de programme, contribution au développement commercial et expertise sectorielle reconnue.",
            },
            new()
            {
                Grade                    = "Director",
                Niveau                   = 5,
                NiveauMinCompetences     = 4.5,
                AncienneteMinAns         = 8,
                NombreImplementationsMin = 8,
                ExperienceDomainMinAns   = 7,
                GradeSuivant             = "Partner",
                Description              = "Responsabilité P&L, développement de nouvelles offres et management d'une practice ou d'un domaine.",
            },
            new()
            {
                Grade                    = "Partner",
                Niveau                   = 6,
                NiveauMinCompetences     = 4.8,
                AncienneteMinAns         = 10,
                NombreImplementationsMin = 12,
                ExperienceDomainMinAns   = 9,
                GradeSuivant             = null,
                Description              = "Direction stratégique, représentation EY, développement du portefeuille client et gouvernance.",
            },
        };

        ctx.GradeReferentiels.AddRange(grades);
        ctx.Parametres.Add(new Parametre { Code = SeedVersion, Valeur = "done" });
        await ctx.SaveChangesAsync();
    }
}
