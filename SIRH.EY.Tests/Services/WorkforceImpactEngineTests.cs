using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class WorkforceImpactEngineTests
{
    private static Competence Comp(string nom, int niveauActuel = 3) => new() { Nom = nom, NiveauActuel = niveauActuel, NiveauCible = 3 };

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(150, 100)]
    public void Clamp_BoundsToZeroAndHundred(double value, double expected) =>
        Assert.Equal(expected, WorkforceImpactEngine.Clamp(value));

    [Fact]
    public void GetSkillNames_OrdersByLevelDescending_DistinctCaseInsensitive()
    {
        var collaborateur = new Collaborateur
        {
            Nom = "X",
            Prenom = "Y",
            Competences = new List<Competence> { Comp("power bi", 2), Comp("Power BI", 5), Comp("Audit", 4) }
        };

        var result = WorkforceImpactEngine.GetSkillNames(collaborateur);

        Assert.Equal(2, result.Count);
        Assert.Equal("Power BI", result[0]); // highest NiveauActuel (5) wins, kept first due to OrderByDescending before Distinct
    }

    [Fact]
    public void IsSeniorProfile_TrueForSeniorOrManagerGrades()
    {
        Assert.True(WorkforceImpactEngine.IsSeniorProfile("Senior"));
        Assert.True(WorkforceImpactEngine.IsSeniorProfile("Manager"));
        Assert.False(WorkforceImpactEngine.IsSeniorProfile("Junior"));
        Assert.False(WorkforceImpactEngine.IsSeniorProfile(null));
    }

    [Fact]
    public void GetAverageLevel_NoCompetences_ReturnsNeutral1Point5()
    {
        var collaborateur = new Collaborateur { Nom = "X", Prenom = "Y" };
        Assert.Equal(1.5, WorkforceImpactEngine.GetAverageLevel(collaborateur));
    }

    [Fact]
    public void ComputeDepartmentFragility_UsesTopExposure_WhenPresent()
    {
        // (25 + 40) clamped -> 65
        Assert.Equal(65, WorkforceImpactEngine.ComputeDepartmentFragility(40));
    }

    [Fact]
    public void ComputeDepartmentFragility_NullExposure_FallsBackTo45_NotTwentyFivePlusFortyFive()
    {
        // Preserves original "?? 45" precedence: null short-circuits the whole (25 + x) expression to 45, not 25+45=70
        Assert.Equal(45, WorkforceImpactEngine.ComputeDepartmentFragility(null));
    }

    [Fact]
    public void ComputeSkillExposure_NoTargetSkills_ReturnsDefault55()
    {
        Assert.Equal(55, WorkforceImpactEngine.ComputeSkillExposure(new List<string>(), new List<Collaborateur>()));
    }

    [Fact]
    public void ComputeContinuityRisk_HigherBestReadiness_LowersRisk()
    {
        var lowReadinessRisk = WorkforceImpactEngine.ComputeContinuityRisk(targetSkillsCount: 5, impactedTeamCount: 3, bestReadiness: 0);
        var highReadinessRisk = WorkforceImpactEngine.ComputeContinuityRisk(targetSkillsCount: 5, impactedTeamCount: 3, bestReadiness: 90);

        Assert.True(highReadinessRisk < lowReadinessRisk);
    }

    [Fact]
    public void ComputeStrategicDependency_WeightsThreeInputs()
    {
        // 0.38*100 + 0.32*100 + 0.30*100 = 100
        Assert.Equal(100, WorkforceImpactEngine.ComputeStrategicDependency(100, 100, 100));
    }

    [Fact]
    public void BuildSuccessors_RanksByReadinessDescending_AndClassifiesSuccessorType()
    {
        var target = new Collaborateur { Nom = "Target", Prenom = "T", Departement = "Consulting", Competences = new List<Competence> { Comp("Audit", 4), Comp("IFRS", 4) } };

        var strongCandidate = new Collaborateur { Id = 1, Nom = "Strong", Prenom = "S", Departement = "Consulting", Grade = "Senior", Competences = new List<Competence> { Comp("Audit", 5), Comp("IFRS", 5) } };
        var weakCandidate = new Collaborateur { Id = 2, Nom = "Weak", Prenom = "W", Departement = "TAX", Grade = "Junior", Competences = new List<Competence>() };

        var result = WorkforceImpactEngine.BuildSuccessors(target, WorkforceImpactEngine.GetSkillNames(target), new List<Collaborateur> { weakCandidate, strongCandidate });

        Assert.Equal(2, result.Count);
        Assert.Equal(strongCandidate.Id, result[0].CollaborateurId);
        Assert.True(result[0].ReadinessScore > result[1].ReadinessScore);
        Assert.Equal(DecisionEngine.ClassifySuccessorType(result[0].ReadinessScore), result[0].SuccessorType);
    }
}
