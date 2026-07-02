namespace SIRH.EY.Services;

public static class CompetenceCatalogService
{
    public static readonly List<string> Departements = new()
    {
        "Assurance",
        "Consulting",
        "Strategy & Transactions",
        "TAX",
        "Talent Team",
        "Service IT",
        "Finances et contrôle",
        "Facilities",
        "MBD",
        "Risk management"
    };

    public static readonly List<string> Postes = new()
    {
        "Consultant",
        "Senior Consultant",
        "Manager",
        "Senior Manager",
        "Director",
        "Partner",
        "Analyste",
        "Développeur",
        "Chef de projet",
        "Auditeur",
        "Contrôleur de gestion"
    };

    public static readonly List<string> Grades = new()
    {
        "Junior",
        "Senior",
        "Manager",
        "Senior Manager",
        "Director",
        "Partner"
    };

    public static string GetCompetenceType(string? categorie)
    {
        if (string.IsNullOrWhiteSpace(categorie)) return "Technique";

        var techCategories = new[] { "Tech", "Outils", "Data", "Audit" };
        var fonctionnelCategories = new[] { "Méthodes", "Management", "Soft skills", "RH" };

        if (techCategories.Any(t => categorie.Contains(t, StringComparison.OrdinalIgnoreCase)))
            return "Technique";
        if (fonctionnelCategories.Any(f => categorie.Contains(f, StringComparison.OrdinalIgnoreCase)))
            return "Fonctionnel";

        return "Transverse";
    }

    public static string GetDefaultCategorie(string competenceNom)
    {
        var lower = competenceNom.ToLower();
        if (lower.Contains("azure") || lower.Contains("d365") || lower.Contains("power automate") ||
            lower.Contains("integration") || lower.Contains("data migration"))
            return "Tech";
        if (lower.Contains("audit") || lower.Contains("ifrs") || lower.Contains("financial"))
            return "Audit";
        if (lower.Contains("fiscal") || lower.Contains("tax") || lower.Contains("vat") || lower.Contains("transfer pricing"))
            return "Fiscalité";
        if (lower.Contains("management") || lower.Contains("stakeholder") || lower.Contains("change"))
            return "Management";
        if (lower.Contains("brd") || lower.Contains("requirements") || lower.Contains("business process"))
            return "Méthodes";

        return "Métier";
    }
}
