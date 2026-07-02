using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class DecisionEngineTests
{
    [Theory]
    [InlineData(0, "Ready")]
    [InlineData(1, "Low")]
    [InlineData(2, "Medium")]
    [InlineData(3, "High")]
    [InlineData(5, "High")]
    public void ClassifyGapSeverity_MapsGapToSeverity(int gap, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifyGapSeverity(gap));

    [Theory]
    [InlineData(0, "Couvert")]
    [InlineData(1, "A renforcer")]
    [InlineData(2, "Prioritaire")]
    [InlineData(3, "Critique")]
    public void ClassifyGapPriorityLabel_MapsGapToLabel(int gap, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifyGapPriorityLabel(gap));

    [Theory]
    [InlineData(0.0, "High potential")]
    [InlineData(44.9, "High potential")]
    [InlineData(45.0, "Partial successor")]
    [InlineData(74.9, "Partial successor")]
    [InlineData(75.0, "Immediate successor")]
    [InlineData(100.0, "Immediate successor")]
    public void ClassifySuccessorType_MapsReadinessToType(double readiness, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifySuccessorType(readiness));

    [Theory]
    [InlineData(0.0, "Controlled")]
    [InlineData(54.9, "Controlled")]
    [InlineData(55.0, "Elevated")]
    [InlineData(74.9, "Elevated")]
    [InlineData(75.0, "Critical")]
    public void ClassifyRiskLevel_MapsScoreToRiskLevel(double score, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifyRiskLevel(score));

    [Theory]
    [InlineData(0.0, "Exposition controlee")]
    [InlineData(44.9, "Exposition controlee")]
    [InlineData(45.0, "Fragilite a surveiller")]
    [InlineData(69.9, "Fragilite a surveiller")]
    [InlineData(70.0, "Forte dependance")]
    public void ClassifyExposureSignal_MapsScoreToSignal(double score, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifyExposureSignal(score));

    [Theory]
    [InlineData("Critical", "High")]
    [InlineData("Elevated", "Medium")]
    [InlineData("Controlled", "Low")]
    public void ClassifyActionPriority_MapsRiskLevelToPriority(string riskLevel, string expected) =>
        Assert.Equal(expected, DecisionEngine.ClassifyActionPriority(riskLevel));
}
