using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class RemplacantMatchingEngineTests
{
    private static Competence Comp(string nom, int niveauActuel = 3, int niveauCible = 3) =>
        new() { Nom = nom, NiveauActuel = niveauActuel, NiveauCible = niveauCible };

    // ── BuildCompetencesManquantesSimple (Depart) ──────────────────────────

    [Fact]
    public void BuildCompetencesManquantesSimple_RemplacantBelowRequiredLevel_IsMissing()
    {
        var partant = new[] { Comp("Audit financier", niveauCible: 4) };
        var remplacant = new[] { Comp("Audit financier", niveauActuel: 2) };

        var result = RemplacantMatchingEngine.BuildCompetencesManquantesSimple(partant, remplacant);

        Assert.Equal(new[] { "Audit financier" }, result);
    }

    [Fact]
    public void BuildCompetencesManquantesSimple_RemplacantMeetsRequiredLevel_NotMissing()
    {
        var partant = new[] { Comp("Audit financier", niveauCible: 4) };
        var remplacant = new[] { Comp("Audit financier", niveauActuel: 4) };

        var result = RemplacantMatchingEngine.BuildCompetencesManquantesSimple(partant, remplacant);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCompetencesManquantesSimple_ComparisonIsCaseSensitive_MatchingOriginalBehavior()
    {
        var partant = new[] { Comp("Audit Financier", niveauCible: 4) };
        var remplacant = new[] { Comp("audit financier", niveauActuel: 5) };

        // Original code used cr.Nom == cp.Nom (ordinal, case-sensitive) — preserved as-is.
        var result = RemplacantMatchingEngine.BuildCompetencesManquantesSimple(partant, remplacant);

        Assert.Equal(new[] { "Audit Financier" }, result);
    }

    // ── CompatibilitePourcent / CompetencesManquantesPourCandidat (ExportComparaisonRemplacantsPdf) ──

    [Fact]
    public void CompatibilitePourcent_NoRequiredCompetences_ReturnsZero()
    {
        Assert.Equal(0, RemplacantMatchingEngine.CompatibilitePourcent(new List<string>(), new[] { Comp("X") }));
    }

    [Fact]
    public void CompatibilitePourcent_FullCoverage_Returns100()
    {
        var requises = new List<string> { "Audit", "IFRS" };
        var candidat = new[] { Comp("Audit"), Comp("IFRS") };

        Assert.Equal(100, RemplacantMatchingEngine.CompatibilitePourcent(requises, candidat));
    }

    [Fact]
    public void CompatibilitePourcent_PartialCoverage_RoundsToNearestPercent()
    {
        var requises = new List<string> { "Audit", "IFRS", "Tax" };
        var candidat = new[] { Comp("Audit") };

        // 1/3 = 33.33% -> rounds to 33
        Assert.Equal(33, RemplacantMatchingEngine.CompatibilitePourcent(requises, candidat));
    }

    [Fact]
    public void CompatibilitePourcent_IsCaseInsensitive()
    {
        var requises = new List<string> { "Audit" };
        var candidat = new[] { Comp("AUDIT") };

        Assert.Equal(100, RemplacantMatchingEngine.CompatibilitePourcent(requises, candidat));
    }

    [Fact]
    public void CompetencesManquantesPourCandidat_ReturnsOnlyUncoveredRequirements()
    {
        var requises = new List<string> { "Audit", "IFRS", "Tax" };
        var candidat = new[] { Comp("Audit") };

        var result = RemplacantMatchingEngine.CompetencesManquantesPourCandidat(requises, candidat);

        Assert.Equal(new[] { "IFRS", "Tax" }, result);
    }

    [Fact]
    public void CompetencesManquantesPourCandidat_NullCandidateCompetences_AllRequirementsMissing()
    {
        var requises = new List<string> { "Audit" };

        var result = RemplacantMatchingEngine.CompetencesManquantesPourCandidat(requises, null);

        Assert.Equal(requises, result);
    }

    // ── CompetencesManquantesParNoms (GetRemplacants) ──────────────────────

    [Fact]
    public void CompetencesManquantesParNoms_UsesOrdinalCaseSensitiveComparison_MatchingOriginalExceptBehavior()
    {
        var requises = new List<string> { "Audit", "IFRS" };
        var candidat = new List<string> { "audit" }; // different case on purpose

        var result = RemplacantMatchingEngine.CompetencesManquantesParNoms(requises, candidat);

        // "audit" != "Audit" under default (ordinal) comparison used by .Except() originally
        Assert.Equal(new[] { "Audit", "IFRS" }, result);
    }

    [Fact]
    public void CompetencesManquantesParNoms_ExactMatch_IsExcluded()
    {
        var requises = new List<string> { "Audit", "IFRS" };
        var candidat = new List<string> { "Audit" };

        var result = RemplacantMatchingEngine.CompetencesManquantesParNoms(requises, candidat);

        Assert.Equal(new[] { "IFRS" }, result);
    }

    // ── ScoreMatching (RhInsightsController.GetMatchingRemplacants) ────────

    [Fact]
    public void ScoreMatching_ComputesScoreRoundedToOneDecimal()
    {
        var requises = new List<string> { "A", "B", "C" };
        var candidat = new[] { Comp("A"), Comp("B") };

        var (communes, manquantes, score, nomsCandidat) = RemplacantMatchingEngine.ScoreMatching(requises, candidat);

        Assert.Equal(2, communes);
        Assert.Equal(new[] { "C" }, manquantes);
        Assert.Equal(66.7, score);
        Assert.Equal(2, nomsCandidat.Count);
    }

    [Fact]
    public void ScoreMatching_NoRequiredCompetences_ReturnsZeroScore()
    {
        var (_, _, score, _) = RemplacantMatchingEngine.ScoreMatching(new List<string>(), new[] { Comp("A") });

        Assert.Equal(0, score);
    }

    [Fact]
    public void ScoreMatching_NullCandidateCompetences_ReturnsAllMissing()
    {
        var requises = new List<string> { "A" };

        var (communes, manquantes, score, nomsCandidat) = RemplacantMatchingEngine.ScoreMatching(requises, null);

        Assert.Equal(0, communes);
        Assert.Equal(requises, manquantes);
        Assert.Equal(0, score);
        Assert.Empty(nomsCandidat);
    }
}
