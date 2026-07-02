namespace SIRH.EY.Services;

public sealed class PowerAutomateSettings
{
    public const string SectionName = "PowerAutomate";

    /// <summary>Master switch — set false to silently skip all notifications without errors.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>HTTP timeout in seconds for each flow call.</summary>
    public int TimeoutSeconds { get; init; } = 30;

    public PowerAutomateFlowUrls Flows { get; init; } = new();
}

public sealed class PowerAutomateFlowUrls
{
    /// <summary>HTTP POST trigger URL for the "Promotion Ready" flow.</summary>
    public string? PromotionReady { get; init; }

    /// <summary>HTTP POST trigger URL for the "Succession Risk" flow.</summary>
    public string? SuccessionRisk { get; init; }

    /// <summary>HTTP POST trigger URL for the "Certification Expiration" flow.</summary>
    public string? CertificationExpiration { get; init; }

    /// <summary>HTTP POST trigger URL for the "Development Plan Created" flow.</summary>
    public string? DevelopmentPlanCreated { get; init; }

    public string? TalentReviewCompleted { get; set; }
}
