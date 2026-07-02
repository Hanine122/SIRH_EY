using SIRH.EY.Services.PowerAutomate;

namespace SIRH.EY.Services;

public interface IPowerAutomateService
{
    /// <summary>
    /// Triggers the "Promotion Ready" Power Automate flow.
    /// Call this after a promotion simulation confirms a collaborateur has reached readiness.
    /// </summary>
    Task<PowerAutomateResult> NotifyPromotionReadyAsync(
        PromotionReadyNotification notification,
        CancellationToken ct = default);

Task<PowerAutomateResult> NotifyTalentReviewCompletedAsync(
    TalentReviewNotification notification,
    CancellationToken ct = default);
    /// <summary>
    /// Triggers the "Succession Risk" Power Automate flow.
    /// Call this when a workforce impact simulation identifies a critical succession gap.
    /// </summary>
    Task<PowerAutomateResult> NotifySuccessionRiskAsync(
        SuccessionRiskNotification notification,
        CancellationToken ct = default);

    /// <summary>
    /// Triggers the "Certification Expiration" Power Automate flow.
    /// Call this when a certification is within the configured expiration window.
    /// </summary>
    Task<PowerAutomateResult> NotifyCertificationExpirationAsync(
        CertificationExpirationNotification notification,
        CancellationToken ct = default);

    /// <summary>
    /// Triggers the "Development Plan Created" Power Automate flow.
    /// Call this when an RH assigns or updates a development plan for a collaborateur.
    /// </summary>
    Task<PowerAutomateResult> NotifyDevelopmentPlanCreatedAsync(
        DevelopmentPlanNotification notification,
        CancellationToken ct = default);
}
