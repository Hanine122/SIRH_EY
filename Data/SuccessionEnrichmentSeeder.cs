using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Enrichit les profils de compétences pour que chaque poste critique possède
/// au moins 3 candidats crédibles dans le moteur de succession (NbCommunes > 0).
///
/// Corrections métier :
///  · Partners (Sami, Hatem) : ajout Business Development + Stakeholder management
///  · Directors : ajout Conseil stratégique + Gestion de projet + Analyse manquants
///  · Senior Managers (Sarra, Omar, Aymen) : compétences transverses SM
///  · Managers de base (Ahmed, Yasmine, Meriem, Nidhal, Ibtissem) : compétences RH/Audit/Risk
///  · Fix cohérence : "Risk assessment" ajouté en complément de "Gestion des risques"
/// </summary>
public static class SuccessionEnrichmentSeeder
{
    private const string SeedVersion = "SUCCESSION_ENRICHMENT_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var now = DateTime.Today;

        // Index rapide : email → CollaborateurId
        var all = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);

        // Index catégories
        var cats = await ctx.CategoriesCompetences.ToDictionaryAsync(c => c.Nom, c => c.Id);
        int Cat(string nom) => cats.TryGetValue(nom, out var id) ? id : 0;

        var competences = new List<Competence>();

        // Ajoute la compétence uniquement si le collaborateur n'en a pas encore une
        // avec le même nom (protection idempotente).
        void Add(string email, string nom, string cat, int actuel, int cible, int daysAgo = 30)
        {
            if (!all.TryGetValue(email, out var colId)) return;
            if (ctx.Competences.Any(x => x.CollaborateurId == colId && x.Nom == nom)) return;
            competences.Add(new Competence
            {
                CollaborateurId       = colId,
                Nom                   = nom,
                CategorieCompetenceId = Cat(cat) == 0 ? null : (int?)Cat(cat),
                NiveauActuel          = actuel,
                NiveauCible           = cible,
                DateEvaluation        = now.AddDays(-daysAgo)
            });
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PARTNERS — référentiel : Leadership stratégique(5), Business Dev(5),
        //             Stakeholder management(5), Communication executive(5)
        //  Problème : Sami et Hatem manquent Business Dev + Stakeholder
        // ══════════════════════════════════════════════════════════════════════════

        // Sami Trabelsi (Partner, Assurance) — déjà : Leadership strat(5), Audit(5), IFRS(5), Comm exec(5)
        Add("sami.trabelsi@ey.com",  "Business Development",   "Management", 4, 5, 60);
        Add("sami.trabelsi@ey.com",  "Stakeholder management", "Management", 4, 5, 55);

        // Hatem Gharbi (Partner, Tax) — déjà : Leadership strat(5), Tax comp(5), Fisc int(5)
        Add("hatem.gharbi@ey.com",   "Business Development",   "Management", 4, 5, 65);
        Add("hatem.gharbi@ey.com",   "Stakeholder management", "Management", 4, 5, 60);
        Add("hatem.gharbi@ey.com",   "Communication executive","Soft skills", 4, 5, 55);

        // ══════════════════════════════════════════════════════════════════════════
        //  DIRECTORS — référentiel : Leadership(5), Conseil stratégique(5),
        //              Gestion de projet(4), Analyse & résolution(4)
        //  Problème : chaque directeur manque 1-3 de ces compétences
        // ══════════════════════════════════════════════════════════════════════════

        // Yosra Hammami (Director, Technology) — déjà : Leadership(5), Gestion projet(4)
        Add("yosra.hammami@ey.com",  "Conseil stratégique",              "Management", 4, 5, 45);
        Add("yosra.hammami@ey.com",  "Analyse & résolution de problèmes","Méthodes",   4, 5, 40);

        // Mehdi Jlassi (Director, Data) — déjà : Leadership(4), Analyse(5)
        Add("mehdi.jlassi@ey.com",   "Conseil stratégique",              "Management", 4, 5, 50);
        Add("mehdi.jlassi@ey.com",   "Gestion de projet",                "Management", 4, 5, 45);

        // Walid Khelifi (Director, Cybersecurity) — déjà : Leadership(4), ISO 27001(5)
        Add("walid.khelifi@ey.com",  "Conseil stratégique",              "Management", 3, 5, 50);
        Add("walid.khelifi@ey.com",  "Gestion de projet",                "Management", 3, 5, 45);
        Add("walid.khelifi@ey.com",  "Analyse & résolution de problèmes","Méthodes",   3, 5, 40);

        // Rania Chebbi (Director, People Consulting) — déjà : Leadership(4), Communication(5)
        Add("rania.chebbi@ey.com",   "Conseil stratégique",              "Management", 4, 5, 48);
        Add("rania.chebbi@ey.com",   "Gestion de projet",                "Management", 4, 5, 42);
        Add("rania.chebbi@ey.com",   "Analyse & résolution de problèmes","Méthodes",   4, 5, 38);

        // ══════════════════════════════════════════════════════════════════════════
        //  SENIOR MANAGERS — référentiel : Leadership(4), Gestion de projet(4),
        //                    Stakeholder management(4), Analyse & résolution(4)
        //  Problème : Sarra a 0 compétence du référentiel ; Omar et Aymen manquent
        //             Stakeholder + Analyse
        // ══════════════════════════════════════════════════════════════════════════

        // Sarra Ben Ali (SM, Data) — déjà : Machine Learning(4), Power BI(4), SQL(4)
        Add("sarra.benali@ey.com",   "Leadership",                       "Leadership", 3, 5, 35);
        Add("sarra.benali@ey.com",   "Gestion de projet",                "Management", 3, 5, 32);
        Add("sarra.benali@ey.com",   "Stakeholder management",           "Management", 3, 5, 28);
        Add("sarra.benali@ey.com",   "Analyse & résolution de problèmes","Méthodes",   3, 5, 25);

        // Omar Ben Salah (SM, Audit Manager) — déjà : Audit(4), IFRS(4), Leadership(3), Gestion projet(4)
        Add("omar.bensalah@ey.com",  "Stakeholder management",           "Management", 3, 4, 30);
        Add("omar.bensalah@ey.com",  "Analyse & résolution de problèmes","Méthodes",   4, 4, 25);

        // Aymen Trabelsi (SM, Technology) — déjà : Cloud(4), Gestion projet(4), Leadership(3)
        Add("aymen.trabelsi@ey.com", "Stakeholder management",           "Management", 3, 5, 30);
        Add("aymen.trabelsi@ey.com", "Analyse & résolution de problèmes","Méthodes",   3, 5, 25);

        // ══════════════════════════════════════════════════════════════════════════
        //  MANAGERS — postes critiques : HR Director, Audit Manager, Risk Manager
        // ══════════════════════════════════════════════════════════════════════════

        // Ahmed Ben Youssef (Manager, Audit Manager) — AUCUNE compétence seedée !
        // HR Director referentiel : Leadership(4), Communication(5), Gestion talents(4), Conduite(4)
        // Audit Manager referentiel : Leadership(4), Gestion projet(4), Stakeholder(4), Quality review(4)
        Add("Ahmed.benyoussef@ey.com","Leadership",                       "Leadership", 3, 4, 20);
        Add("Ahmed.benyoussef@ey.com","Gestion de projet",                "Management", 3, 4, 18);
        Add("Ahmed.benyoussef@ey.com","Communication",                    "Soft skills",3, 4, 16);
        Add("Ahmed.benyoussef@ey.com","Analyse & résolution de problèmes","Méthodes",   3, 4, 14);

        // Yasmine Kooli (Manager, People Consulting) — déjà : Gestion talents(3), Conduite(3), Comm(4)
        // Manque Leadership → complète le profil HR Director (Leadership=4 requis)
        Add("yasmine.kooli@ey.com",  "Leadership",                       "Leadership", 3, 4, 22);

        // Ibtissem Bessrour (Manager, Risk Manager) — déjà : Leadership(4), Gestion projet(4), Stakeholder(4), QRev(4)
        // Ajout Risk assessment (terme anglais cohérent avec le référentiel Risk Manager)
        // et Communication pour apparaître dans HR Director pool
        Add("ibtissem.bessrour@ey.com","Risk assessment",                 "Risk",       4, 4, 25);
        Add("ibtissem.bessrour@ey.com","Communication",                   "Soft skills",4, 4, 20);

        // Meriem Gharbi (Manager, Cybersecurity) — déjà : RGPD(3), ISO(3), Gestion risques(4)
        // Risk Manager referentiel utilise "Risk assessment" (terme anglais) — on l'ajoute
        // car "Gestion des risques" n'est pas reconnu comme "Risk assessment" par le moteur
        Add("meriem.gharbi@ey.com",  "Risk assessment",                  "Risk",       3, 4, 22);

        // Nidhal Hammami (Manager, Business Transformation) — déjà : Change management(3), Gestion projet(3), Comm(3)
        // Risk assessment niveau 2 → partial credit (gap critique : 4-2=2, 0%) mais utile pour pipeline
        // On lui donne niveau 3 → gap=1 → 60% partial coverage
        Add("nidhal.hammami@ey.com", "Risk assessment",                  "Risk",       3, 4, 20);

        // ══════════════════════════════════════════════════════════════════════════
        //  SENIOR CONSULTANTS — enrichir pour pipeline plausible
        // ══════════════════════════════════════════════════════════════════════════

        // Mehdi Mabrouk (Senior, Assurance, Auditeur Senior) — déjà : Audit(3), IFRS(3), Communication(3)
        // Potentiel Audit Manager → ajouter Gestion de projet(2) pour apparaître dans "en attente"
        Add("mehdi.mabrouk@ey.com",  "Gestion de projet",                "Management", 2, 4, 15);
        Add("mehdi.mabrouk@ey.com",  "Leadership",                       "Leadership", 2, 4, 12);

        if (competences.Any())
        {
            ctx.Competences.AddRange(competences);
        }

        ctx.Parametres.Add(new Parametre
        {
            Code             = SeedVersion,
            Valeur           = DateTime.UtcNow.ToString("O"),
            TypeValeur       = "string",
            Description      = "Enrichissement succession — alignement référentiel / profils pour pools cohérents",
            EstModifiable    = false,
            DerniereModification = DateTime.Now
        });

        await ctx.SaveChangesAsync();
    }
}
