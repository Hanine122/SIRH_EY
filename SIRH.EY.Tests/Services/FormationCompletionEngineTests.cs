using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class FormationCompletionEngineTests
{
    [Fact]
    public void ResolveCompetenceUpdate_NoCompetenceVisee_ReturnsNoCompetence()
    {
        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing: null, competenceVisee: null, grade: "Junior");

        Assert.Equal(CompetenceCompletionOutcome.NoCompetence, result.Outcome);
    }

    [Fact]
    public void ResolveCompetenceUpdate_EmptyCompetenceVisee_ReturnsNoCompetence()
    {
        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing: null, competenceVisee: "", grade: "Senior");

        Assert.Equal(CompetenceCompletionOutcome.NoCompetence, result.Outcome);
    }

    [Theory]
    [InlineData("Junior", 3)]
    [InlineData("Senior", 4)]
    [InlineData("Manager", 5)]
    public void ResolveCompetenceUpdate_NoExistingCompetence_ReturnsCreatedWithGradeBasedTarget(string grade, int expectedNiveauCible)
    {
        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing: null, competenceVisee: "Power BI", grade: grade);

        Assert.Equal(CompetenceCompletionOutcome.Created, result.Outcome);
        Assert.Equal("Power BI", result.CompetenceNom);
        Assert.Equal(expectedNiveauCible, result.NiveauCible);
    }

    [Fact]
    public void ResolveCompetenceUpdate_ExistingBelowTarget_ReturnsIncrementedByOne()
    {
        var existing = new Competence { Nom = "Power BI", NiveauActuel = 2, NiveauCible = 4 };

        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing, "Power BI", "Junior");

        Assert.Equal(CompetenceCompletionOutcome.Incremented, result.Outcome);
        Assert.Equal(3, result.NouveauNiveau);
    }

    [Fact]
    public void ResolveCompetenceUpdate_ExistingOneBelowTarget_CapsAtNiveauCible()
    {
        var existing = new Competence { Nom = "Power BI", NiveauActuel = 3, NiveauCible = 4 };

        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing, "Power BI", "Junior");

        Assert.Equal(CompetenceCompletionOutcome.Incremented, result.Outcome);
        Assert.Equal(4, result.NouveauNiveau);
    }

    [Fact]
    public void ResolveCompetenceUpdate_ExistingAtTarget_ReturnsAlreadyAtTarget()
    {
        var existing = new Competence { Nom = "Power BI", NiveauActuel = 4, NiveauCible = 4 };

        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing, "Power BI", "Junior");

        Assert.Equal(CompetenceCompletionOutcome.AlreadyAtTarget, result.Outcome);
    }

    [Fact]
    public void ResolveCompetenceUpdate_ExistingAboveTarget_ReturnsAlreadyAtTarget()
    {
        var existing = new Competence { Nom = "Power BI", NiveauActuel = 5, NiveauCible = 4 };

        var result = FormationCompletionEngine.ResolveCompetenceUpdate(existing, "Power BI", "Junior");

        Assert.Equal(CompetenceCompletionOutcome.AlreadyAtTarget, result.Outcome);
    }
}
