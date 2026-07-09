using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Backfill ponctuel de CompetenceRequiseParPoste.Priorite pour les lignes existantes,
/// dérivé de NiveauRequis (donnée déjà présente, aucune nouvelle saisie) :
///   NiveauRequis >= 4 -> Obligatoire, == 3 -> Prioritaire, <= 2 -> Optionnel.
/// </summary>
public static class CompetencePrioriteSeeder
{
    private const string SeedVersion = "COMPETENCE_PRIORITE_BACKFILL_V1_2026_07";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var lignes = await ctx.CompetencesRequisesParPoste.ToListAsync();

        foreach (var ligne in lignes)
        {
            ligne.Priorite = ligne.NiveauRequis >= 4 ? "Obligatoire"
                : ligne.NiveauRequis == 3 ? "Prioritaire"
                : "Optionnel";
        }

        ctx.Parametres.Add(new Parametre
        {
            Code                = SeedVersion,
            Valeur              = DateTime.UtcNow.ToString("O"),
            TypeValeur          = "string",
            Description         = "Backfill CompetenceRequiseParPoste.Priorite depuis NiveauRequis existant",
            EstModifiable       = false,
            DerniereModification = DateTime.Now
        });

        await ctx.SaveChangesAsync();
    }
}
