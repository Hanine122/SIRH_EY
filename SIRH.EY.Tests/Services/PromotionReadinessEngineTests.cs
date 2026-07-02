using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class PromotionReadinessEngineTests
{
    [Fact]
    public void BuildCompetencyGaps_ComputesGapAndClassifiesSeverity()
    {
        var required = new[] { ("Leadership", 4), ("Power BI", 2) };
        var current = new Dictionary<string, int> { ["Leadership"] = 1, ["Power BI"] = 2 };

        var gaps = PromotionReadinessEngine.BuildCompetencyGaps(required, current);

        var leadership = gaps.Single(g => g.Competence == "Leadership");
        Assert.Equal(3, leadership.Gap);
        Assert.Equal("High", leadership.Severity);
        Assert.Equal("Critique", leadership.PriorityLabel);

        var powerBi = gaps.Single(g => g.Competence == "Power BI");
        Assert.Equal(0, powerBi.Gap);
        Assert.Equal("Ready", powerBi.Severity);
    }

    [Fact]
    public void BuildCompetencyGaps_MissingSkillDefaultsToZeroCurrentLevel()
    {
        var required = new[] { ("Data analytics", 3) };
        var current = new Dictionary<string, int>();

        var gaps = PromotionReadinessEngine.BuildCompetencyGaps(required, current);

        Assert.Equal(0, gaps.Single().CurrentLevel);
        Assert.Equal(3, gaps.Single().Gap);
    }

    [Fact]
    public void BuildCompetencyGaps_OrdersByGapDescendingThenNameAscending()
    {
        var required = new[] { ("B", 2), ("A", 2), ("C", 5) };
        var current = new Dictionary<string, int>();

        var gaps = PromotionReadinessEngine.BuildCompetencyGaps(required, current);

        Assert.Equal(new[] { "C", "A", "B" }, gaps.Select(g => g.Competence));
    }

    [Fact]
    public void ComputeCompatibilityScore_RoundsToOneDecimal()
    {
        Assert.Equal(66.7, PromotionReadinessEngine.ComputeCompatibilityScore(coveredCount: 2, totalCount: 3));
    }

    [Fact]
    public void ComputeCompatibilityScore_ZeroTotal_UsesMinimumDivisorOfOne()
    {
        Assert.Equal(0, PromotionReadinessEngine.ComputeCompatibilityScore(0, 0));
    }

    [Fact]
    public void ComputeReadiness_FloorsAt35()
    {
        // totalGap huge relative to maxGap -> would go far below 35 without the floor
        Assert.Equal(35, PromotionReadinessEngine.ComputeReadiness(totalGap: 100, maxGap: 10));
    }

    [Fact]
    public void ComputeReadiness_NoGap_Returns100()
    {
        Assert.Equal(100, PromotionReadinessEngine.ComputeReadiness(totalGap: 0, maxGap: 10));
    }

    [Fact]
    public void ComputeScorePerformance_UsesTalentEvalWhenPresent()
    {
        var eval = new TalentEvaluation { PerformanceScore = 4 };
        Assert.Equal(80.0, PromotionReadinessEngine.ComputeScorePerformance(eval, niveauPreparationSuccession: null));
    }

    [Fact]
    public void ComputeScorePerformance_FallsBackToNiveauPreparationSuccession()
    {
        Assert.Equal(65, PromotionReadinessEngine.ComputeScorePerformance(null, niveauPreparationSuccession: 65));
    }

    [Fact]
    public void ComputeScorePerformance_NoDataAtAll_DefaultsToNeutral50()
    {
        Assert.Equal(50, PromotionReadinessEngine.ComputeScorePerformance(null, null));
    }

    [Theory]
    [InlineData("high", 100.0)]
    [InlineData("Medium", 60.0)]
    [InlineData("LOW", 20.0)]
    [InlineData(null, 40.0)]
    [InlineData("unknown", 40.0)]
    public void ComputeScorePotentiel_FallsBackToPotentielCarriereWhenNoEval(string? potentielCarriere, double expected)
    {
        Assert.Equal(expected, PromotionReadinessEngine.ComputeScorePotentiel(null, potentielCarriere));
    }

    [Fact]
    public void ComputeScorePotentiel_UsesTalentEvalWhenPresent()
    {
        var eval = new TalentEvaluation { PotentielScore = 5 };
        Assert.Equal(100.0, PromotionReadinessEngine.ComputeScorePotentiel(eval, "low"));
    }

    [Fact]
    public void ComputeScoreAnciennete_RelativeToGradeThreshold()
    {
        Assert.Equal(50.0, PromotionReadinessEngine.ComputeScoreAnciennete(ancienneteAns: 2, ancienneteMinAnsGradeCible: 4));
    }

    [Fact]
    public void ComputeScoreAnciennete_NoThreshold_UsesFlatMultiplier()
    {
        Assert.Equal(50.0, PromotionReadinessEngine.ComputeScoreAnciennete(ancienneteAns: 2, ancienneteMinAnsGradeCible: null));
    }

    [Fact]
    public void ComputeMultiCriteriaScore_AppliesFortyTwentyFiveTwentyFifteenWeights()
    {
        // 0.40*100 + 0.25*100 + 0.20*100 + 0.15*100 = 100
        Assert.Equal(100.0, PromotionReadinessEngine.ComputeMultiCriteriaScore(100, 100, 100, 100));
        // 0.40*80 + 0.25*60 + 0.20*40 + 0.15*20 = 32+15+8+3 = 58
        Assert.Equal(58.0, PromotionReadinessEngine.ComputeMultiCriteriaScore(80, 60, 40, 20));
    }

    [Fact]
    public void ComputePromotionPotential_CapsAt98()
    {
        Assert.Equal(98.0, PromotionReadinessEngine.ComputePromotionPotential(100));
    }

    [Fact]
    public void EstimateMonths_TrainingReducesMinimumButCapsReductionAtFour()
    {
        var (min, max) = PromotionReadinessEngine.EstimateMonths(totalGap: 5, completedTrainings: 10, recommendationCount: 2);

        // baseMonths = max(2, 5*2+2) = 12; reduction = min(4,10) = 4; min = max(1, 12-4) = 8
        Assert.Equal(8, min);
        Assert.Equal(10, max);
    }

    [Fact]
    public void EstimateMonths_NeverGoesBelowOneMonth()
    {
        var (min, _) = PromotionReadinessEngine.EstimateMonths(totalGap: 0, completedTrainings: 10, recommendationCount: 0);

        Assert.Equal(1, min);
    }
}
