namespace SIRH.EY.Services.PowerAutomate;

// ─── Generic result ───────────────────────────────────────────────────────────

public sealed record PowerAutomateResult(
    bool Success,
    string? ErrorMessage = null,
    int? HttpStatusCode = null)
{
    public static PowerAutomateResult Ok()
        => new(true);

    public static PowerAutomateResult Disabled()
        => new(true, "Power Automate notifications are disabled.");

    public static PowerAutomateResult NotConfigured(string flowName)
        => new(false, $"Flow URL for '{flowName}' is not configured in appsettings.json.");

    public static PowerAutomateResult Fail(string error, int? code = null)
        => new(false, error, code);
}

// ─── Promotion Ready ──────────────────────────────────────────────────────────

/// <summary>
/// Sent when a collaborateur reaches a promotion readiness threshold.
/// Triggers an approval workflow and an email to the RH manager.
/// </summary>
public sealed record PromotionReadyNotification(
    int    CollaborateurId,
    string NomComplet,
    string? Email,
    string Poste,
    string Grade,
    string GradeCible,
    string? Departement,
    double MultiCriteriaScore,
    double ReadinessPercentage,
    double PromotionPotential,
    int    EstimatedMonthsMin,
    int    EstimatedMonthsMax,
    IReadOnlyList<string> CompetencesManquantes,
    IReadOnlyList<string> FormationsRecommandees,
    DateTime GeneratedAt)
{
    public static PromotionReadyNotification Create(
        int    collaborateurId,
        string nomComplet,
        string? email,
        string poste,
        string grade,
        string gradeCible,
        string? departement,
        double multiCriteriaScore,
        double readinessPercentage,
        double promotionPotential,
        int    estimatedMonthsMin,
        int    estimatedMonthsMax,
        IEnumerable<string>? competencesManquantes = null,
        IEnumerable<string>? formationsRecommandees = null)
        => new(
            collaborateurId, nomComplet, email, poste, grade, gradeCible, departement,
            multiCriteriaScore, readinessPercentage, promotionPotential,
            estimatedMonthsMin, estimatedMonthsMax,
            competencesManquantes?.ToList().AsReadOnly() ?? [],
            formationsRecommandees?.ToList().AsReadOnly() ?? [],
            DateTime.UtcNow);
}

// ─── Succession Risk ──────────────────────────────────────────────────────────

/// <summary>
/// Sent when a critical collaborateur has no identified successor.
/// Triggers an alert to the HR Director and the department manager.
/// </summary>
public sealed record SuccessionRiskNotification(
    int    CollaborateurId,
    string NomComplet,
    string? Email,
    string Poste,
    string? Grade,
    string? Departement,
    double ContinuityRisk,
    double OperationalImpact,
    double StrategicDependencyScore,
    string RiskLevel,
    int    ImmediateSuccessorsCount,
    IReadOnlyList<string> CompetencesCritiques,
    DateTime GeneratedAt)
{
    public static SuccessionRiskNotification Create(
        int    collaborateurId,
        string nomComplet,
        string? email,
        string poste,
        string? grade,
        string? departement,
        double continuityRisk,
        double operationalImpact,
        double strategicDependencyScore,
        string riskLevel,
        int    immediateSuccessorsCount,
        IEnumerable<string>? competencesCritiques = null)
        => new(
            collaborateurId, nomComplet, email, poste, grade, departement,
            continuityRisk, operationalImpact, strategicDependencyScore,
            riskLevel, immediateSuccessorsCount,
            competencesCritiques?.ToList().AsReadOnly() ?? [],
            DateTime.UtcNow);
}

// ─── Certification Expiration ─────────────────────────────────────────────────

/// <summary>
/// Sent when a collaborateur's certification is approaching expiration.
/// Urgence: "Critique" (≤30 days), "Urgent" (≤60 days), "À planifier" (≤90 days).
/// </summary>
public sealed record CertificationExpirationNotification(
    int    CollaborateurId,
    string NomComplet,
    string? Email,
    string? Poste,
    string? Grade,
    string? Departement,
    string CertificationNom,
    string? Organisme,
    string? Domaine,
    string? CodeExamen,
    DateTime DateExpiration,
    int    JoursRestants,
    string Urgence,
    DateTime GeneratedAt)
{
    public static CertificationExpirationNotification Create(
        int    collaborateurId,
        string nomComplet,
        string? email,
        string? poste,
        string? grade,
        string? departement,
        string certificationNom,
        string? organisme,
        string? domaine,
        string? codeExamen,
        DateTime dateExpiration)
    {
        var joursRestants = (int)(dateExpiration.Date - DateTime.Today).TotalDays;
        var urgence = joursRestants <= 30 ? "Critique"
                    : joursRestants <= 60 ? "Urgent"
                    : "À planifier";

        return new(
            collaborateurId, nomComplet, email, poste, grade, departement,
            certificationNom, organisme, domaine, codeExamen,
            dateExpiration, joursRestants, urgence, DateTime.UtcNow);
    }
}

// ─── Development Plan Created ─────────────────────────────────────────────────

/// <summary>
/// Sent when an RH creates or updates a development plan for a collaborateur.
/// Triggers a notification to the collaborateur and their manager.
/// </summary>
public sealed record DevelopmentPlanNotification(
    int    CollaborateurId,
    string NomComplet,
    string? Email,
    string? ManagerEmail,
    string GradeActuel,
    string? PosteCible,
    string? Departement,
    double ScoreCouvertureActuel,
    int    NombreCompetencesADevelopper,
    int    DureeEstimeeSemaines,
    IReadOnlyList<DevelopmentPlanFormation> FormationsRecommandees,
    DateTime GeneratedAt)
{
    public static DevelopmentPlanNotification Create(
        int    collaborateurId,
        string nomComplet,
        string? email,
        string? managerEmail,
        string gradeActuel,
        string? posteCible,
        string? departement,
        double scoreCouvertureActuel,
        int    nombreCompetencesADevelopper,
        int    dureeEstimeeSemaines,
        IEnumerable<DevelopmentPlanFormation>? formations = null)
        => new(
            collaborateurId, nomComplet, email, managerEmail,
            gradeActuel, posteCible, departement,
            scoreCouvertureActuel, nombreCompetencesADevelopper, dureeEstimeeSemaines,
            formations?.ToList().AsReadOnly() ?? [],
            DateTime.UtcNow);
}

public sealed record DevelopmentPlanFormation(
    string Titre,
    string Competence,
    int    Gap,
    string Priorite,
    int?   DureeHeures);
