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
}
