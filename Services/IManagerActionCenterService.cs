using System.Security.Claims;

namespace SIRH.EY.Services;

public record PendingValidationItem(
    int CompetenceId,
    int CollaborateurId,
    string CollaborateurNom,
    string CompetenceNom,
    DateTime DateAutoEvaluation,
    int JoursEnAttente,
    int NiveauCible);

public record PendingDevelopmentPlanItem(
    int PlanId,
    int CollaborateurId,
    string CollaborateurNom,
    string FormationTitre,
    DateTime DateRecommandation,
    int JoursEnAttente);

public record PendingInscriptionApprovalItem(
    int InscriptionId,
    int CollaborateurId,
    string CollaborateurNom,
    string FormationTitre,
    DateTime DateInscription,
    int JoursEnAttente);

public record ExpiringCertificationItem(
    int CollaborateurCertificationId,
    int CollaborateurId,
    string CollaborateurNom,
    string CertificationNom,
    DateTime DateExpiration,
    int JoursRestants,
    string Urgence);

public record HrInboxSummary(
    IReadOnlyList<PendingValidationItem> PendingValidations,
    IReadOnlyList<PendingDevelopmentPlanItem> PendingDevelopmentPlans,
    IReadOnlyList<ExpiringCertificationItem> ExpiringCertifications,
    IReadOnlyList<PendingInscriptionApprovalItem> PendingInscriptionApprovals)
{
    public int TotalCount => PendingValidations.Count + PendingDevelopmentPlans.Count
        + ExpiringCertifications.Count + PendingInscriptionApprovals.Count;
}

public interface IManagerActionCenterService
{
    // Auto-evaluations awaiting manager validation, scoped to the current user's team via
    // ITeamAccessService. "Pending" is the existing EvaluationCompetence.ValidationManager flag —
    // no new business rule.
    Task<IReadOnlyList<PendingValidationItem>> GetPendingValidationsAsync(ClaimsPrincipal user);

    // Unified "Smart HR Inbox" — resolves the team scope once and combines all three pending
    // action sources for the current manager.
    Task<HrInboxSummary> GetInboxAsync(ClaimsPrincipal user);
}
