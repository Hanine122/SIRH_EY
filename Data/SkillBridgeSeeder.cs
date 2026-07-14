using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Peuple le référentiel canonique Skill (jusqu'ici vide — voir commentaire dans
/// Models\Skill.cs) à partir des noms déjà en usage dans Competence.Nom et
/// Formation.CompetenceVisee, puis relie chaque Competence/Formation à son Skill via
/// le nouveau FK nullable SkillId. Correspondance exacte (trim + insensible à la casse) —
/// c'est la construction du catalogue elle-même, donc la correspondance est garantie
/// par construction, pas un matching approximatif.
///
/// Cette relation FK devient le chemin PRIORITAIRE pour PlanDeveloppementService /
/// FormationCompletionEngine ; l'ancien texte-matching reste un filet de sécurité
/// inchangé pour les lignes que ce backfill ne couvrirait pas (ex. données ajoutées
/// après coup sans SkillId).
/// </summary>
public static class SkillBridgeSeeder
{
    private const string SeedVersion = "SKILL_BRIDGE_V1_2026_07";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var competences = await ctx.Competences.ToListAsync();
        var formations = await ctx.Formations.ToListAsync();
        var existingSkills = await ctx.Skills.ToListAsync();

        var skillsByKey = existingSkills
            .GroupBy(s => s.Nom.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        Skill GetOrCreateSkill(string rawName)
        {
            var key = rawName.Trim().ToLowerInvariant();
            if (skillsByKey.TryGetValue(key, out var skill))
                return skill;

            skill = new Skill { Nom = rawName.Trim() };
            ctx.Skills.Add(skill);
            skillsByKey[key] = skill;
            return skill;
        }

        foreach (var competence in competences)
        {
            if (string.IsNullOrWhiteSpace(competence.Nom)) continue;
            competence.Skill = GetOrCreateSkill(competence.Nom);
        }

        foreach (var formation in formations)
        {
            if (string.IsNullOrWhiteSpace(formation.CompetenceVisee)) continue;
            formation.Skill = GetOrCreateSkill(formation.CompetenceVisee);
        }

        ctx.Parametres.Add(new Parametre
        {
            Code                = SeedVersion,
            Valeur              = DateTime.UtcNow.ToString("O"),
            TypeValeur          = "string",
            Description         = "Backfill du catalogue Skill depuis Competence.Nom / Formation.CompetenceVisee, pont FK SkillId",
            EstModifiable       = false,
            DerniereModification = DateTime.Now
        });

        await ctx.SaveChangesAsync();
    }
}
