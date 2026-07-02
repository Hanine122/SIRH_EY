using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class CompetenceRulesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(40, 2)]
    [InlineData(41, 3)]
    [InlineData(60, 3)]
    [InlineData(61, 4)]
    [InlineData(80, 4)]
    [InlineData(81, 5)]
    [InlineData(100, 5)]
    public void NiveauFromScore_MapsScoreToLevel_MatchingOriginalControllerFormula(int score, int expectedNiveau)
    {
        Assert.Equal(expectedNiveau, CompetenceRules.NiveauFromScore(score));
    }

    [Fact]
    public void NiveauFromScore_ClampsToMax5_EvenAboveHundred()
    {
        Assert.Equal(5, CompetenceRules.NiveauFromScore(150));
    }

    [Fact]
    public void NiveauFromScore_ClampsToMin1_EvenAtZeroOrNegative()
    {
        Assert.Equal(1, CompetenceRules.NiveauFromScore(0));
        Assert.Equal(1, CompetenceRules.NiveauFromScore(-10));
    }
}
