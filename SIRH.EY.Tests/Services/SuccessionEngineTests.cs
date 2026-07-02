using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class SuccessionEngineTests
{
    private static Competence Comp(string nom) => new() { Nom = nom, NiveauActuel = 3, NiveauCible = 3 };

    [Fact]
    public void BuildCompetencesRequisesUnion_MergesProfileAndPosteRequirements_NoDuplicates()
    {
        var profil = new[] { Comp("Audit financier"), Comp("IFRS") };
        var poste = new[] { "IFRS", "Risk Assessment" };

        var result = SuccessionEngine.BuildCompetencesRequisesUnion(profil, poste);

        Assert.Equal(3, result.Count);
        Assert.Contains("Audit financier", result);
        Assert.Contains("IFRS", result);
        Assert.Contains("Risk Assessment", result);
    }

    [Fact]
    public void BuildCompetencesRequisesUnion_IsCaseInsensitive()
    {
        var profil = new[] { Comp("power bi") };
        var poste = new[] { "Power BI" };

        var result = SuccessionEngine.BuildCompetencesRequisesUnion(profil, poste);

        Assert.Single(result);
    }

    [Fact]
    public void BuildCompetencesRequisesUnion_NullProfile_ReturnsOnlyPosteRequirements()
    {
        var result = SuccessionEngine.BuildCompetencesRequisesUnion(null, new[] { "Audit interne" });

        Assert.Single(result);
        Assert.Contains("Audit interne", result);
    }

    [Fact]
    public void BuildCompetencesRequisesUnion_BlankCompetenceNames_AreExcluded()
    {
        var profil = new[] { Comp(""), Comp("  "), Comp("Compliance") };

        var result = SuccessionEngine.BuildCompetencesRequisesUnion(profil, Enumerable.Empty<string>());

        Assert.Single(result);
        Assert.Contains("Compliance", result);
    }
}
