using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Complète le référentiel CompetencesRequisesParPoste pour les postes présents
/// dans la base de collaborateurs mais absents du référentiel après tous les seeders
/// existants (PostesReferentielSeeder + DemoDataSeeder + CompetencesITCRMSeeder).
///
/// Postes couverts après ce seeder :
///   Existants  : Partner, Director, Senior Manager, Consultant, Senior Auditor,
///                Data Analyst, Audit Manager, Risk Manager, HR Director,
///                Technical/Functional/TechnoFunctional/Business Analyst/Quality
///                Analyst/Project Manager/Architect CRM
///   Ajoutés ici: Manager, Senior Consultant, Auditeur Senior, Auditeur,
///                Senior Data Analyst, Cybersecurity Analyst, Tax Manager
///
/// Les niveaux requis sont calibrés pour que les candidats actuels (Senior
/// Consultants, Juniors) aient une couverture de 60–100 % et apparaissent dans
/// le pool de succession du moteur (NbCommunes > 0).
///
/// Noms de compétences : identiques aux noms utilisés dans EnterpriseDemoSeeder
/// (comparaison OrdinalIgnoreCase dans SuccessionEngine).
/// </summary>
public static class MissingReferentielSeeder
{
    private const string SeedVersion = "POSTES_REFERENTIEL_COMPLETION_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var entries = new List<CompetenceRequiseParPoste>
        {
            // ── Manager ──────────────────────────────────────────────────────────
            //
            // Pool cible : Aziz Belhadj (Senior, Consulting), Khalil Ben Romdhane
            // (Senior, Data), après l'ajout de Leadership(3) par HrDataCorrectionSeeder.
            // Nidhal Hammami et Houssem Ben Amor couvrent Gestion de projet + Communication.
            //
            // NiveauRequis=3 : le moteur exige NiveauActuel >= 3*0.6 = 1.8 pour compter
            // dans NbCommunes, et calcule le poids NiveauActuel/3 pour ScoreCouverture.

            new() { Poste = "Manager", Competence = "Leadership",                      NiveauRequis = 3 },
            new() { Poste = "Manager", Competence = "Gestion de projet",               NiveauRequis = 3 },
            new() { Poste = "Manager", Competence = "Communication",                   NiveauRequis = 3 },
            new() { Poste = "Manager", Competence = "Analyse & résolution de problèmes", NiveauRequis = 3 },

            // ── Senior Consultant ─────────────────────────────────────────────────
            //
            // Pool cible (pour succession d'un Senior Consultant) : Junior Consultants
            // les plus avancés (Tarek, Lina, Rami) + Mehdi Mabrouk (via Gestion projet).
            // Niveaux 3 : calibrés pour que Aziz (3/3/3/3) soit couverture 100 %.

            new() { Poste = "Senior Consultant", Competence = "Gestion de projet",               NiveauRequis = 3 },
            new() { Poste = "Senior Consultant", Competence = "Communication",                   NiveauRequis = 3 },
            new() { Poste = "Senior Consultant", Competence = "Analyse & résolution de problèmes", NiveauRequis = 3 },
            new() { Poste = "Senior Consultant", Competence = "Excel avancé",                    NiveauRequis = 3 },

            // ── Auditeur Senior ───────────────────────────────────────────────────
            //
            // Poste de Mehdi Mabrouk (EY-SRC-003, depuis 2020).
            // Différent de "Senior Auditor" (DemoDataSeeder) par le nom français.
            // Compétences alignées sur le profil réel de Mehdi :
            //   Audit(3), IFRS(3), Communication(3) du seeder de base ;
            //   Gestion de projet(2) ajouté par SuccessionEnrichmentSeeder.
            // Niveau Gestion de projet requis = 2 : Mehdi atteint exactement ce seuil.

            new() { Poste = "Auditeur Senior", Competence = "Audit & contrôle interne",    NiveauRequis = 3 },
            new() { Poste = "Auditeur Senior", Competence = "IFRS / normes comptables",    NiveauRequis = 3 },
            new() { Poste = "Auditeur Senior", Competence = "Communication",               NiveauRequis = 3 },
            new() { Poste = "Auditeur Senior", Competence = "Gestion de projet",           NiveauRequis = 2 },

            // ── Auditeur ─────────────────────────────────────────────────────────
            //
            // Poste de Hajer Zribi (EY-CNS-007, Junior, depuis 2024).
            // Niveaux requis très bas (1-2) : profil d'entrée de poste.
            // Hajer couvre : Audit(1/1)=100 %, Communication(2/2)=100 %, Excel(2/2)=100 %,
            // IFRS manquant → ScoreCouverture ≈ 75 %.

            new() { Poste = "Auditeur", Competence = "Audit & contrôle interne",    NiveauRequis = 1 },
            new() { Poste = "Auditeur", Competence = "Communication",               NiveauRequis = 2 },
            new() { Poste = "Auditeur", Competence = "Excel avancé",                NiveauRequis = 2 },
            new() { Poste = "Auditeur", Competence = "IFRS / normes comptables",    NiveauRequis = 1 },

            // ── Senior Data Analyst ───────────────────────────────────────────────
            //
            // Poste de Nadia Ben Hassen (EY-SRC-005, Senior, depuis 2021, "Haut potentiel").
            // Khalil Ben Romdhane couvre Power BI(4), SQL(3), Data storytelling(3) → 3/4.
            // Dorra Ben Mustapha et Sarra Ben Ali (Machine Learning, Power BI, SQL)
            // peuvent apparaître comme candidats transversaux (autre département).

            new() { Poste = "Senior Data Analyst", Competence = "Power BI",           NiveauRequis = 4 },
            new() { Poste = "Senior Data Analyst", Competence = "SQL",                NiveauRequis = 3 },
            new() { Poste = "Senior Data Analyst", Competence = "Data storytelling",  NiveauRequis = 3 },
            new() { Poste = "Senior Data Analyst", Competence = "Machine Learning",   NiveauRequis = 3 },

            // ── Cybersecurity Analyst ─────────────────────────────────────────────
            //
            // Poste de Fatma Chaabane (EY-CNS-002, Junior, depuis 2023, "Développement requis").
            // Niveau requis = 2 pour les trois compétences principales : Fatma les a toutes à 2
            // → ScoreCouverture = 75 % (ISO 27001 manquant, requis à 1).
            // Walid Khelifi (Director, Cybersecurity) ou Meriem Gharbi (Manager) peuvent
            // être des candidats forts (RGPD+ISO+Gestion des risques).

            new() { Poste = "Cybersecurity Analyst", Competence = "RGPD & conformité",   NiveauRequis = 2 },
            new() { Poste = "Cybersecurity Analyst", Competence = "Gestion des risques", NiveauRequis = 2 },
            new() { Poste = "Cybersecurity Analyst", Competence = "Communication",       NiveauRequis = 2 },
            new() { Poste = "Cybersecurity Analyst", Competence = "ISO 27001",           NiveauRequis = 1 },

            // ── Tax Manager ───────────────────────────────────────────────────────
            //
            // Poste de Rim Triki (EY-MGR-005, Manager, Tax, depuis 2016).
            // Amira Ben Slama (Senior, Tax) est la candidate naturelle pour le pool :
            //   Tax compliance(3)=req(3) ✓, Communication(3) ✓, Excel(3) ✓
            //   Fiscalité internationale : Amira ne l'a pas → ScoreCouverture 75 %.
            // Niveau Fiscalité internationale requis = 2 pour permettre aux Seniors
            // qui commencent à développer cette compétence d'apparaître dans le pool.

            new() { Poste = "Tax Manager", Competence = "Tax compliance",           NiveauRequis = 3 },
            new() { Poste = "Tax Manager", Competence = "Fiscalité internationale", NiveauRequis = 2 },
            new() { Poste = "Tax Manager", Competence = "Communication",            NiveauRequis = 3 },
            new() { Poste = "Tax Manager", Competence = "Excel avancé",             NiveauRequis = 3 },
        };

        // Filtre idempotent : n'insère que ce qui n'existe pas encore
        var existingPairs = await ctx.CompetencesRequisesParPoste
            .Select(x => new { x.Poste, x.Competence })
            .ToListAsync();

        var toAdd = entries.Where(e =>
            !existingPairs.Any(ex =>
                string.Equals(ex.Poste, e.Poste, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ex.Competence, e.Competence, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (toAdd.Any())
            ctx.CompetencesRequisesParPoste.AddRange(toAdd);

        ctx.Parametres.Add(new Parametre
        {
            Code                = SeedVersion,
            Valeur              = DateTime.UtcNow.ToString("O"),
            TypeValeur          = "string",
            Description         = "Audit RH 2026-06 — Référentiel compétences requis pour 7 postes manquants",
            EstModifiable       = false,
            DerniereModification = DateTime.Now
        });

        await ctx.SaveChangesAsync();
    }
}
