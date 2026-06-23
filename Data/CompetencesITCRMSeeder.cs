using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public static class CompetencesITCRMSeeder
{
    private const string SeedVersion = "COMPETENCES_IT_CRM_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var entries = new List<CompetenceRequiseParPoste>
        {
            // ── Technical Consultant CRM ──────────────────────────────────────────
            new() { Poste = "Technical Consultant CRM", Competence = "Microsoft Dynamics 365 CRM",       NiveauRequis = 4 },
            new() { Poste = "Technical Consultant CRM", Competence = "Power Platform",                    NiveauRequis = 3 },
            new() { Poste = "Technical Consultant CRM", Competence = "Intégration ERP/CRM",               NiveauRequis = 3 },
            new() { Poste = "Technical Consultant CRM", Competence = "Support fonctionnel",               NiveauRequis = 3 },

            // ── Functional Consultant CRM ─────────────────────────────────────────
            new() { Poste = "Functional Consultant CRM", Competence = "D365 Sales & Customer Service",   NiveauRequis = 4 },
            new() { Poste = "Functional Consultant CRM", Competence = "Microsoft Dynamics 365 CRM",      NiveauRequis = 4 },
            new() { Poste = "Functional Consultant CRM", Competence = "Analyse fonctionnelle",            NiveauRequis = 4 },
            new() { Poste = "Functional Consultant CRM", Competence = "Formation utilisateurs",           NiveauRequis = 3 },

            // ── Techno Functional CRM ─────────────────────────────────────────────
            new() { Poste = "Techno Functional CRM", Competence = "Microsoft Dynamics 365 CRM",          NiveauRequis = 4 },
            new() { Poste = "Techno Functional CRM", Competence = "Power Platform",                       NiveauRequis = 4 },
            new() { Poste = "Techno Functional CRM", Competence = "Analyse fonctionnelle",                NiveauRequis = 4 },
            new() { Poste = "Techno Functional CRM", Competence = "Intégration ERP/CRM",                 NiveauRequis = 4 },

            // ── Business Analyst CRM ─────────────────────────────────────────────
            new() { Poste = "Business Analyst CRM", Competence = "Rédaction de spécifications (BRD)",    NiveauRequis = 4 },
            new() { Poste = "Business Analyst CRM", Competence = "Analyse fonctionnelle",                 NiveauRequis = 4 },
            new() { Poste = "Business Analyst CRM", Competence = "Gestion de projet",                    NiveauRequis = 3 },
            new() { Poste = "Business Analyst CRM", Competence = "Communication",                         NiveauRequis = 4 },

            // ── Quality Analyst CRM ──────────────────────────────────────────────
            new() { Poste = "Quality Analyst CRM", Competence = "Test & validation (SIT/UAT)",            NiveauRequis = 4 },
            new() { Poste = "Quality Analyst CRM", Competence = "Rédaction de spécifications (BRD)",      NiveauRequis = 4 },
            new() { Poste = "Quality Analyst CRM", Competence = "Analyse fonctionnelle",                  NiveauRequis = 3 },
            new() { Poste = "Quality Analyst CRM", Competence = "Microsoft Dynamics 365 CRM",             NiveauRequis = 3 },

            // ── Project Manager CRM ──────────────────────────────────────────────
            new() { Poste = "Project Manager CRM", Competence = "Gestion de projet",                      NiveauRequis = 5 },
            new() { Poste = "Project Manager CRM", Competence = "Stakeholder management",                 NiveauRequis = 4 },
            new() { Poste = "Project Manager CRM", Competence = "Conduite du changement",                 NiveauRequis = 4 },
            new() { Poste = "Project Manager CRM", Competence = "Communication",                           NiveauRequis = 4 },
            new() { Poste = "Project Manager CRM", Competence = "Microsoft Dynamics 365 CRM",             NiveauRequis = 3 },

            // ── Architect CRM ────────────────────────────────────────────────────
            new() { Poste = "Architect CRM", Competence = "Microsoft Dynamics 365 CRM",                   NiveauRequis = 5 },
            new() { Poste = "Architect CRM", Competence = "Power Platform",                                NiveauRequis = 5 },
            new() { Poste = "Architect CRM", Competence = "Architecture solution",                         NiveauRequis = 5 },
            new() { Poste = "Architect CRM", Competence = "Intégration ERP/CRM",                          NiveauRequis = 5 },
            new() { Poste = "Architect CRM", Competence = "Analyse fonctionnelle",                         NiveauRequis = 4 },
        };

        ctx.CompetencesRequisesParPoste.AddRange(entries);
        ctx.Parametres.Add(new Parametre { Code = SeedVersion, Valeur = "done" });
        await ctx.SaveChangesAsync();
    }
}
