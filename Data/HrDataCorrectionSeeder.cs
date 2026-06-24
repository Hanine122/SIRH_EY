using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Corrige trois catégories de défauts détectés lors de l'audit RH global (2026-06) :
///
///  1. PotentielCarriere — toutes les valeurs françaises sont converties vers
///     les clés anglaises attendues par SuccessionEngine.Score("high"/"medium"/"low").
///     Sans cette correction, le critère 15 % du score de succession est inopérant
///     pour tous les collaborateurs d'EnterpriseDemoSeeder.
///
///  2. RoleRH — les trois Senior Managers avaient RoleRH="Manager" (erreur de copie
///     dans EnterpriseDemoSeeder) ; corrigé en "Senior Manager".
///
///  3. Compétences Leadership manquantes pour le pool Manager — pour que le moteur
///     trouve des candidats crédibles lors d'une succession "Manager", au moins les
///     Seniors les plus expérimentés reçoivent Leadership(3).  Aymen Trabelsi reçoit
///     Leadership(4) (mise à niveau depuis 3) afin de couvrir complètement le
///     référentiel Senior Manager.
/// </summary>
public static class HrDataCorrectionSeeder
{
    private const string SeedVersion = "HR_DATA_CORRECTION_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        // ── 1. PotentielCarriere : français → anglais (SuccessionEngine) ─────────
        //
        // Mapping métier :
        //   "Haut potentiel"      → "High"   (score 100/100 dans le moteur)
        //   "Potentiel solide"    → "Medium"  (score  60/100)
        //   "Développement requis"→ "Low"    (score  20/100)
        //
        // Sans la correction, chaque collaborateur reçoit le score neutre 40/100,
        // ce qui aplatit le classement et rend le critère Potentiel sans effet.

        var potMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Haut potentiel",       "High"   },
            { "Potentiel solide",     "Medium" },
            { "Développement requis", "Low"    },
            // Variantes éventuellement saisies manuellement
            { "Succession prioritaire", "High"   },
            { "Emergent",               "Medium" },
        };

        var collabsToFix = await ctx.Collaborateurs
            .Where(c => c.PotentielCarriere != null
                     && c.PotentielCarriere != "High"
                     && c.PotentielCarriere != "Medium"
                     && c.PotentielCarriere != "Low")
            .ToListAsync();

        foreach (var c in collabsToFix)
        {
            if (potMapping.TryGetValue(c.PotentielCarriere!, out var mapped))
                c.PotentielCarriere = mapped;
        }

        // ── 2. RoleRH : Senior Managers corrigés ─────────────────────────────────
        //
        // EnterpriseDemoSeeder (lignes 271, 287, 335) a positionné RoleRH="Manager"
        // pour les trois Senior Managers.  Ce champ est utilisé par l'interface
        // RH et les filtres de tableau de bord.

        var seniorManagers = new[] { "aymen.trabelsi@ey.com", "sarra.benali@ey.com", "omar.bensalah@ey.com" };

        var smsToFix = await ctx.Collaborateurs
            .Where(c => c.Email != null && seniorManagers.Contains(c.Email) && c.RoleRH == "Manager")
            .ToListAsync();

        foreach (var sm in smsToFix)
            sm.RoleRH = "Senior Manager";

        await ctx.SaveChangesAsync();

        // ── 3. Compétences Leadership manquantes ──────────────────────────────────
        //
        // Le référentiel "Manager" (ajouté par MissingReferentielSeeder) exige
        // Leadership(3).  Pour que les Seniors les plus expérimentés apparaissent
        // dans le pool de succession Manager, on leur ajoute Leadership(3).
        //
        // Aymen Trabelsi : Leadership est déjà à 3 dans la base ; on le monte à 4
        // pour qu'il couvre à 100 % le critère Leadership du référentiel SM(4).

        var now = DateTime.Today;
        var all = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);
        var cats = await ctx.CategoriesCompetences.ToDictionaryAsync(c => c.Nom, c => c.Id);
        int Cat(string nom) => cats.TryGetValue(nom, out var id) ? id : 0;

        var newComps = new List<Competence>();

        void AddComp(string email, string nom, string cat, int actuel, int cible)
        {
            if (!all.TryGetValue(email, out var colId)) return;
            if (ctx.Competences.Any(x => x.CollaborateurId == colId && x.Nom == nom)) return;
            newComps.Add(new Competence
            {
                CollaborateurId       = colId,
                Nom                   = nom,
                CategorieCompetenceId = Cat(cat) == 0 ? null : (int?)Cat(cat),
                NiveauActuel          = actuel,
                NiveauCible           = cible,
                DateEvaluation        = now
            });
        }

        // Aziz Belhadj (Senior Consultant, Consulting — EY-SRC-001, depuis 2020)
        // Profil le plus complet parmi les Seniors (Gestion projet, Communication,
        // Analyse, Excel tous à 3) → leadership manquant pour le pool Manager.
        AddComp("aziz.belhadj@ey.com",      "Leadership", "Leadership", 3, 4);

        // Khalil Ben Romdhane (Senior, Data & Analytics — EY-SRC-002, "Haut potentiel")
        // RisingStar dans la nine-box ; leadership ajouté pour le pipeline Manager.
        AddComp("khalil.benromdhane@ey.com", "Leadership", "Leadership", 3, 4);

        if (newComps.Any())
            ctx.Competences.AddRange(newComps);

        // Upgrade Leadership 3→4 pour Aymen Trabelsi (mise à niveau SM référentiel)
        if (all.TryGetValue("aymen.trabelsi@ey.com", out var aymenId))
        {
            var aymenLeadership = await ctx.Competences
                .FirstOrDefaultAsync(x => x.CollaborateurId == aymenId && x.Nom == "Leadership");
            if (aymenLeadership != null && aymenLeadership.NiveauActuel < 4)
                aymenLeadership.NiveauActuel = 4;
        }

        // ── Paramètre de garde (idempotence) ────────────────────────────────────
        ctx.Parametres.Add(new Parametre
        {
            Code                = SeedVersion,
            Valeur              = DateTime.UtcNow.ToString("O"),
            TypeValeur          = "string",
            Description         = "Audit RH 2026-06 — Correction PotentielCarriere + RoleRH SM + Leadership Seniors",
            EstModifiable       = false,
            DerniereModification = DateTime.Now
        });

        await ctx.SaveChangesAsync();
    }
}
