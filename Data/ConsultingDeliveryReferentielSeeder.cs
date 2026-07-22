using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Injection du référentiel métiers Consulting / Delivery (alignement AD Ports) —
/// ajoute les 6 postes clés d'un projet de delivery ERP/CRM (Business Analyst,
/// MSD CRM Architect, Technical Consultant, Project Manager, Quality Analyst,
/// Data Entry Operator) au référentiel Position, rattachés au sous-département
/// "ERP & Enterprise Apps" du département Technology (créé par
/// EnterpriseDemoSeeder — le meilleur point d'ancrage existant pour des rôles
/// de delivery ERP/CRM, plus pertinent que "Digital Consulting").
///
/// Chaque poste reçoit ses compétences requises dans CompetenceRequiseParPoste,
/// avec Poste = exactement le même nom que la Position créée (les deux tables
/// ne sont pas liées par FK dans ce modèle — cf. PROJECT_KNOWLEDGE.md §7 — donc
/// la coherence des deux se fait ici par convention de nommage).
///
/// Additif uniquement : chaque Position et chaque ligne CompetenceRequiseParPoste
/// est protégée par une verification d'existence individuelle
/// (if (!await context.Positions.AnyAsync(...))), en plus du garde-fou de
/// version global. Aucune donnee existante n'est modifiee ni supprimee.
/// </summary>
public static class ConsultingDeliveryReferentielSeeder
{
    private const string SeedVersion = "CONSULTING_DELIVERY_REFERENTIEL_V1_2026_07";

    private record PosteDefinition(string Name, string Code, string Description, IReadOnlyList<(string Competence, int Niveau)> Competences);

    private static readonly IReadOnlyList<PosteDefinition> Postes = new[]
    {
        new PosteDefinition(
            "Business Analyst", "BA-CRM",
            "Recueil et formalisation des besoins metier pour les projets de delivery ERP/CRM.",
            new[] {
                ("Requirements Gathering", 4),
                ("Business Requirements Document (BRD)", 4),
                ("RACI Matrix", 3),
                ("Process Reengineering", 3),
            }),

        new PosteDefinition(
            "MSD CRM Architect", "ARCH-CRM",
            "Conception de l'architecture solution Microsoft Dynamics 365 CRM et de ses integrations.",
            new[] {
                ("Microsoft Dynamics 365 CRM", 5),
                ("Architecture solution", 5),
                ("Power Platform", 4),
                ("Intégration ERP/CRM", 4),
            }),

        new PosteDefinition(
            "Technical Consultant", "TC-CRM",
            "Implementation technique et parametrage des solutions CRM/ERP en delivery projet.",
            new[] {
                ("Microsoft Dynamics 365 CRM", 4),
                ("Power Platform", 3),
                ("Intégration ERP/CRM", 3),
                ("Support technique", 3),
            }),

        new PosteDefinition(
            "Project Manager", "PM-CRM",
            "Pilotage de projets de delivery ERP/CRM (planning, budget, parties prenantes).",
            new[] {
                ("Gestion de projet", 5),
                ("Stakeholder management", 4),
                ("RACI Matrix", 3),
                ("Communication", 4),
            }),

        new PosteDefinition(
            "Quality Analyst", "QA-CRM",
            "Validation qualite (SIT/UAT) des livrables ERP/CRM avant mise en production.",
            new[] {
                ("Test & validation (SIT/UAT)", 4),
                ("Rédaction de cas de test", 3),
                ("Analyse fonctionnelle", 3),
                ("Rigueur & fiabilité", 3),
            }),

        new PosteDefinition(
            "Data Entry Operator", "DEO-CRM",
            "Saisie et fiabilisation des donnees dans le cadre des projets de migration/delivery.",
            new[] {
                ("Saisie de données", 2),
                ("Rigueur & fiabilité", 3),
                ("Excel avancé", 2),
                ("Respect des délais", 2),
            }),
    };

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        // Sous-departement d'ancrage : ERP & Enterprise Apps (Technology), cree
        // par EnterpriseDemoSeeder. Repli sur NULL si absent (ne bloque pas la
        // creation des postes, cf. exigence "additif uniquement").
        var subDeptErp = await ctx.SubDepartments.FirstOrDefaultAsync(sd => sd.Name == "ERP & Enterprise Apps");

        var competencesAjoutees = 0;
        var postesAjoutes = 0;

        foreach (var poste in Postes)
        {
            // 1. Position — ajout conditionnel individuel (exigence explicite)
            if (!await ctx.Positions.AnyAsync(p => p.Name == poste.Name))
            {
                ctx.Positions.Add(new Position
                {
                    Name = poste.Name,
                    Code = poste.Code,
                    Description = poste.Description,
                    SubDepartmentId = subDeptErp?.Id,
                    IsActive = true
                });
                postesAjoutes++;
            }

            // 2. Competences requises — ajout conditionnel individuel par paire (Poste, Competence)
            foreach (var (competence, niveau) in poste.Competences)
            {
                if (!await ctx.CompetencesRequisesParPoste.AnyAsync(c => c.Poste == poste.Name && c.Competence == competence))
                {
                    ctx.CompetencesRequisesParPoste.Add(new CompetenceRequiseParPoste
                    {
                        Poste = poste.Name,
                        Competence = competence,
                        NiveauRequis = niveau
                    });
                    competencesAjoutees++;
                }
            }
        }

        if (postesAjoutes > 0 || competencesAjoutees > 0)
            await ctx.SaveChangesAsync();

        ctx.Parametres.Add(new Parametre
        {
            Code = SeedVersion,
            Valeur = $"{postesAjoutes} postes, {competencesAjoutees} competences requises",
            TypeValeur = "string",
            Description = "Referentiel metiers Consulting/Delivery ERP-CRM (alignement AD Ports)",
            EstModifiable = false,
            DerniereModification = DateTime.Now
        });
        await ctx.SaveChangesAsync();
    }
}
