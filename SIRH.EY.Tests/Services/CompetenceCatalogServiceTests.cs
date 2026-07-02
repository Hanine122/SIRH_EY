using SIRH.EY.Services;

namespace SIRH.EY.Tests.Services;

public class CompetenceCatalogServiceTests
{
    [Theory]
    [InlineData(null, "Technique")]
    [InlineData("", "Technique")]
    [InlineData("Tech", "Technique")]
    [InlineData("Outils", "Technique")]
    [InlineData("Data", "Technique")]
    [InlineData("Audit", "Technique")]
    [InlineData("Méthodes", "Fonctionnel")]
    [InlineData("Management", "Fonctionnel")]
    [InlineData("Soft skills", "Fonctionnel")]
    [InlineData("RH", "Fonctionnel")]
    [InlineData("Autre chose", "Transverse")]
    public void GetCompetenceType_ClassifiesByCategoryKeyword(string? categorie, string expected)
    {
        Assert.Equal(expected, CompetenceCatalogService.GetCompetenceType(categorie));
    }

    [Theory]
    [InlineData("Azure Functions", "Tech")]
    [InlineData("Customization D365", "Tech")]
    [InlineData("Power Automate", "Tech")]
    [InlineData("Inbound/Outbound Integration", "Tech")]
    [InlineData("Data Migration", "Tech")]
    [InlineData("Audit financier", "Audit")]
    [InlineData("IFRS", "Audit")]
    [InlineData("Financial Modeling", "Audit")]
    [InlineData("Fiscalité internationale", "Fiscalité")]
    [InlineData("Tax Compliance", "Fiscalité")]
    [InlineData("VAT/GST", "Fiscalité")]
    [InlineData("Transfer Pricing", "Fiscalité")]
    [InlineData("Change Management", "Management")]
    [InlineData("Stakeholder Management", "Management")]
    [InlineData("BRD Execution", "Méthodes")]
    [InlineData("Requirements Gathering", "Méthodes")]
    [InlineData("Business Process Reengineering", "Méthodes")]
    [InlineData("Bilingual Arabic/English", "Métier")]
    public void GetDefaultCategorie_InfersCategoryFromCompetenceName(string competenceNom, string expected)
    {
        Assert.Equal(expected, CompetenceCatalogService.GetDefaultCategorie(competenceNom));
    }
}
