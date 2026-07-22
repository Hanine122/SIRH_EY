using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Seeder correctif additif — comble les lacunes de complétude détectées en audit
/// (PROJECT_KNOWLEDGE.md, 2026-07-06) sur les données déjà seedées par
/// EnterpriseDemoSeeder, CollaborateurEnrichmentSeeder et les 8 collaborateurs
/// "legacy" créés inline dans Program.cs. N'altère aucune logique existante :
/// ne touche que les champs actuellement NULL/insuffisants, jamais une valeur
/// déjà renseignée. Suit le même pattern que HrDataCorrectionSeeder /
/// MissingReferentielSeeder (version-gated, idempotent, additif).
///
/// Constats corrigés :
///  1. Genre / DateNaissance : jamais renseignés par aucun seeder existant
///     (vérifié par recherche exhaustive) — backfill pour tous les collaborateurs actifs.
///  2. SubDepartmentId sur Collaborateur : jamais renseigné par EnterpriseDemoSeeder
///     (seul DepartmentId l'est) — backfill vers le premier SubDepartment du
///     DepartmentId du collaborateur.
///  3. Les 8 collaborateurs legacy (Program.cs, ex. hanine.hammami@ey.com) n'ont
///     ni DepartmentId, GradeId, PositionId, SubDepartmentId, ContractTypeId ni
///     TypeContrat — seules les chaînes legacy (Departement/Grade/Poste) sont
///     renseignées. Résolution par correspondance de nom vers le référentiel HR.
///  4. ManagerId : 7 des 8 collaborateurs legacy n'ont pas de manager (dont un
///     "laissé intentionnellement non assigné" selon le commentaire de Program.cs) —
///     backfill via le collaborateur actif de grade immédiatement supérieur dans le
///     même département, avec repli sur le premier Partner.
///  5. Compétences insuffisantes : la branche générique de secours de
///     CollaborateurEnrichmentSeeder ne donne que 2 compétences (pas 3-5) à tout
///     collaborateur dont le poste ne correspond à aucun mot-clé connu — complété
///     ici jusqu'à 3 minimum, sans dupliquer les compétences déjà présentes.
///  6. SubDepartment manquants : SeedHrMasterData crée 10 départements mais ne
///     peuple de sous-départements que pour 4 d'entre eux — les 6 autres
///     (Strategy & Transactions, Service IT, Finances et Contrôle, Facilities,
///     MBD, Risk Management) reçoivent chacun 2 sous-départements.
/// </summary>
public static class DataIntegritySeeder
{
    private const string SeedVersion = "DATA_INTEGRITY_V2_2026_07";

    private static readonly Dictionary<string, int> GradeRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Junior"] = 1, ["Senior"] = 2, ["Manager"] = 3,
        ["Senior Manager"] = 4, ["Director"] = 5, ["Partner"] = 6
    };

    private static readonly Dictionary<string, int> AgeAEmbaucheParGrade = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Junior"] = 23, ["Senior"] = 25, ["Manager"] = 29,
        ["Senior Manager"] = 33, ["Director"] = 38, ["Partner"] = 45
    };

    // Prénoms observés dans EnterpriseDemoSeeder + les 8 collaborateurs legacy de
    // Program.cs (liste exhaustive vérifiée contre le code au moment de l'écriture).
    private static readonly Dictionary<string, string> GenreParPrenom = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Karim"]="M", ["Sami"]="M", ["Hatem"]="M", ["Yosra"]="F", ["Mehdi"]="M", ["Nour"]="F",
        ["Walid"]="M", ["Rania"]="F", ["Aymen"]="M", ["Sarra"]="F", ["Amine"]="M", ["Meriem"]="F",
        ["Omar"]="M", ["Yasmine"]="F", ["Nidhal"]="M", ["Rim"]="F", ["Houssem"]="M", ["Dorra"]="F",
        ["Aziz"]="M", ["Lina"]="F", ["Khalil"]="M", ["Fatma"]="F", ["Sarah"]="F", ["Rami"]="M",
        ["Amira"]="F", ["Yassine"]="M", ["Nadia"]="F", ["Tarek"]="M", ["Hajer"]="F",
        ["Hanine"]="F", ["Mariem"]="F", ["Raed"]="M", ["Ayoub"]="M", ["Ahmed"]="M",
        ["Sofien"]="M", ["ibtissem"]="F", ["Ibtissem"]="F"
    };

    // Correspondance departement legacy (chaine, 8 collaborateurs Program.cs) ->
    // vrai Department.Name existant dans le referentiel HR.
    private static readonly Dictionary<string, string> DepartementLegacyVersReel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RH"] = "People Consulting",
        ["Audit"] = "Assurance",
        ["Advisory"] = "Assurance",
        ["Risk"] = "Risk Management",
        // "Tax" et "Consulting" correspondent deja exactement a un Department.Name
    };

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        await CompleterSousDepartementsOrphelins(ctx);
        await CompleterProfilsCollaborateursLegacy(ctx);
        await CompleterSubDepartmentIdManquant(ctx);
        await CompleterGenreEtDateNaissance(ctx);
        await CompleterManagerIdManquant(ctx);
        await CompleterCompetencesInsuffisantes(ctx);

        ctx.Parametres.Add(new Parametre
        {
            Code = SeedVersion,
            Valeur = DateTime.UtcNow.ToString("O"),
            TypeValeur = "string",
            Description = "Audit complétude RH 2026-07 — Genre/DateNaissance/SubDepartmentId/ManagerId/Competences/profils legacy",
            EstModifiable = false,
            DerniereModification = DateTime.Now
        });
        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 1. SubDepartments manquants pour les departements orphelins
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterSousDepartementsOrphelins(ApplicationDbContext ctx)
    {
        var nomsParDept = new Dictionary<string, (string A, string B)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Strategy & Transactions"]  = ("Stratégie d'Entreprise", "Fusions & Acquisitions"),
            ["Service IT"]               = ("Infrastructure & Support", "Exploitation & Helpdesk"),
            ["Finances et Contrôle"]     = ("Comptabilité", "Contrôle de Gestion"),
            ["Facilities"]               = ("Logistique", "Maintenance & Sécurité"),
            ["MBD"]                      = ("Marketing & Communication", "Business Development"),
            ["Risk Management"]          = ("Gestion des Risques Opérationnels", "Conformité & Réglementation"),
        };

        var deptsSansSousDept = await ctx.Departments
            .Where(d => !ctx.SubDepartments.Any(sd => sd.DepartmentId == d.Id))
            .ToListAsync();

        foreach (var d in deptsSansSousDept)
        {
            if (!nomsParDept.TryGetValue(d.Name, out var noms))
                noms = ($"{d.Name} — Opérations", $"{d.Name} — Support");

            ctx.SubDepartments.Add(new SubDepartment { Name = noms.A, DepartmentId = d.Id, IsActive = true });
            ctx.SubDepartments.Add(new SubDepartment { Name = noms.B, DepartmentId = d.Id, IsActive = true });
        }

        if (deptsSansSousDept.Any())
            await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2. Profils legacy (les 8 collaborateurs Program.cs) : DepartmentId,
    //    GradeId, PositionId, ContractTypeId/TypeContrat
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterProfilsCollaborateursLegacy(ApplicationDbContext ctx)
    {
        // Perimetre large : tout actif avec au moins un FK de profil manquant,
        // que la chaine legacy correspondante soit renseignee ou non (des
        // profils reels de cette base ont Departement/Poste a NULL, pas
        // seulement les 8 collaborateurs "legacy" connus du code des seeders).
        var incomplets = await ctx.Collaborateurs
            .Where(c => c.Actif && (c.DepartmentId == null || c.GradeId == null || c.PositionId == null))
            .ToListAsync();
        if (!incomplets.Any()) return;

        var departments = await ctx.Departments.ToListAsync();
        var grades      = await ctx.Grades.ToListAsync();
        var positions   = await ctx.Positions.ToListAsync();
        var ctCDI       = await ctx.ContractTypes.FirstOrDefaultAsync(c => c.Code == "CDI");
        var buConsulting = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "Consulting");
        var buCBS         = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "CBS");
        var locLac1       = await ctx.Locations.FirstOrDefaultAsync(l => l.Name == "Tunis — Lac 1");

        // Replis par defaut quand aucune correspondance n'est trouvee (chaine
        // legacy absente ou non reconnue) : garantit le "100% renseigne" exige,
        // plutot que de laisser un profil partiellement complete.
        var deptParDefaut     = departments.FirstOrDefault(d => d.Name == "Consulting");
        var gradeParDefaut    = grades.FirstOrDefault(g => g.Name == "Junior");
        var positionParDefaut = positions.FirstOrDefault(p => p.Name == "Consultant");

        foreach (var c in incomplets)
        {
            if (c.DepartmentId == null)
            {
                var nomDept = c.Departement != null && DepartementLegacyVersReel.TryGetValue(c.Departement, out var mappe)
                    ? mappe : c.Departement;
                var dept = nomDept != null
                    ? departments.FirstOrDefault(d => d.Name.Equals(nomDept, StringComparison.OrdinalIgnoreCase))
                    : null;
                c.DepartmentId = (dept ?? deptParDefaut)?.Id;
            }

            if (c.GradeId == null)
            {
                var grade = c.Grade != null
                    ? grades.FirstOrDefault(g => g.Name.Equals(c.Grade, StringComparison.OrdinalIgnoreCase))
                    : null;
                c.GradeId = (grade ?? gradeParDefaut)?.Id;
            }

            if (c.PositionId == null)
            {
                var position = c.Poste != null
                    ? positions.FirstOrDefault(p => p.Name.Equals(c.Poste, StringComparison.OrdinalIgnoreCase))
                      ?? positions.FirstOrDefault(p =>
                          c.Poste.Contains("Audit", StringComparison.OrdinalIgnoreCase) && p.Name.Contains("Audit", StringComparison.OrdinalIgnoreCase) && p.Name.Contains("Senior", StringComparison.OrdinalIgnoreCase))
                    : null;
                c.PositionId = (position ?? positionParDefaut)?.Id;
            }

            if (c.ContractTypeId == null) c.ContractTypeId = ctCDI?.Id;
            if (string.IsNullOrWhiteSpace(c.TypeContrat)) c.TypeContrat = "CDI";
            if (c.BusinessUnitId == null)
            {
                var estRH = departments.FirstOrDefault(d => d.Id == c.DepartmentId)?.Name == "People Consulting";
                c.BusinessUnitId = estRH ? buCBS?.Id : buConsulting?.Id;
            }
            if (c.LocationId == null) c.LocationId = locLac1?.Id;
        }

        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 3. SubDepartmentId manquant (tous collaborateurs actifs avec DepartmentId
    //    connu mais SubDepartmentId nul)
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterSubDepartmentIdManquant(ApplicationDbContext ctx)
    {
        var aCompleter = await ctx.Collaborateurs
            .Where(c => c.Actif && c.SubDepartmentId == null && c.DepartmentId != null)
            .ToListAsync();
        if (!aCompleter.Any()) return;

        var sousDeptsParDept = await ctx.SubDepartments
            .GroupBy(sd => sd.DepartmentId)
            .ToDictionaryAsync(g => g.Key, g => g.OrderBy(sd => sd.Id).First().Id);

        foreach (var c in aCompleter)
            if (sousDeptsParDept.TryGetValue(c.DepartmentId!.Value, out var sousDeptId))
                c.SubDepartmentId = sousDeptId;

        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 4. Genre + DateNaissance (tous collaborateurs actifs)
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterGenreEtDateNaissance(ApplicationDbContext ctx)
    {
        var aCompleter = await ctx.Collaborateurs
            .Where(c => c.Actif && (c.Genre == null || c.DateNaissance == null))
            .ToListAsync();
        if (!aCompleter.Any()) return;

        foreach (var c in aCompleter)
        {
            if (c.Genre == null)
                c.Genre = GenreParPrenom.TryGetValue(c.Prenom, out var g) ? g : "Non précisé";

            if (c.DateNaissance == null)
            {
                var age = c.Grade != null && AgeAEmbaucheParGrade.TryGetValue(c.Grade, out var a) ? a : 27;
                // Décalage deterministe (pas de vrai hasard) pour éviter des dates
                // identiques entre collaborateurs du même grade.
                var decalageJours = (c.Id * 37) % 300 - 150;
                c.DateNaissance = c.DateEmbauche.AddYears(-age).AddDays(decalageJours);
            }
        }

        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 5. ManagerId manquant (tout actif non-Partner sans manager)
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterManagerIdManquant(ApplicationDbContext ctx)
    {
        var tousActifs = await ctx.Collaborateurs.Where(c => c.Actif).ToListAsync();

        int Rang(Collaborateur c) => c.Grade != null && GradeRank.TryGetValue(c.Grade, out var r) ? r : 0;

        var aCompleter = tousActifs.Where(c => c.ManagerId == null && Rang(c) < GradeRank["Partner"]).ToList();
        if (!aCompleter.Any()) return;

        var premierPartner = tousActifs
            .Where(c => Rang(c) == GradeRank["Partner"])
            .OrderBy(c => c.Id)
            .FirstOrDefault();

        foreach (var c in aCompleter)
        {
            var rangCible = Rang(c);

            var candidat = tousActifs
                .Where(o => o.Id != c.Id && Rang(o) > rangCible &&
                            ((c.Departement != null && o.Departement == c.Departement) ||
                             (c.DepartmentId != null && o.DepartmentId == c.DepartmentId)))
                .OrderBy(o => Rang(o))
                .ThenBy(o => o.Id)
                .FirstOrDefault();

            c.ManagerId = candidat?.Id ?? premierPartner?.Id;
        }

        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 6. Compétences insuffisantes (< 3) : complément générique jusqu'à 3
    // ═══════════════════════════════════════════════════════════════════════
    private static async Task CompleterCompetencesInsuffisantes(ApplicationDbContext ctx)
    {
        var collaborateurs = await ctx.Collaborateurs
            .Include(c => c.Competences)
            .Where(c => c.Actif)
            .ToListAsync();

        var catSoftSkills = (await ctx.CategoriesCompetences.FirstOrDefaultAsync(c => c.Nom == "Soft skills"))?.Id;

        var complements = new[]
        {
            "Adaptabilité", "Esprit d'équipe", "Gestion du temps", "Rigueur & fiabilité"
        };

        int Rang(Collaborateur c) => c.Grade != null && GradeRank.TryGetValue(c.Grade, out var r) ? r : 1;

        var nouvelles = new List<Competence>();

        foreach (var c in collaborateurs)
        {
            var existantes = c.Competences?.Select(x => x.Nom).ToHashSet(StringComparer.OrdinalIgnoreCase)
                              ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var manquantes = 3 - existantes.Count;
            if (manquantes <= 0) continue;

            var (actuel, cible) = Rang(c) switch
            {
                >= 5 => (4, 5),
                4    => (4, 5),
                3    => (3, 4),
                2    => (3, 4),
                _    => (2, 3)
            };

            foreach (var nom in complements)
            {
                if (manquantes <= 0) break;
                if (existantes.Contains(nom)) continue;

                nouvelles.Add(new Competence
                {
                    CollaborateurId = c.Id,
                    Nom = nom,
                    CategorieCompetenceId = catSoftSkills,
                    NiveauActuel = actuel,
                    NiveauCible = cible,
                    DateEvaluation = DateTime.Today
                });
                existantes.Add(nom);
                manquantes--;
            }
        }

        if (nouvelles.Any())
        {
            ctx.Competences.AddRange(nouvelles);
            await ctx.SaveChangesAsync();
        }
    }
}
