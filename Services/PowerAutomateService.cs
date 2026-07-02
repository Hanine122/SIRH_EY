using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SIRH.EY.Services.PowerAutomate;

namespace SIRH.EY.Services;

public sealed class PowerAutomateService : IPowerAutomateService
{
    private readonly HttpClient _http;
    private readonly PowerAutomateSettings _settings;
    private readonly ILogger<PowerAutomateService> _logger;

    public PowerAutomateService(
        IHttpClientFactory httpClientFactory,
        IOptions<PowerAutomateSettings> options,
        ILogger<PowerAutomateService> logger)
    {
        _http     = httpClientFactory.CreateClient("PowerAutomate");
        _settings = options.Value;
        _logger   = logger;
    }

    

    public Task<PowerAutomateResult> NotifyPromotionReadyAsync(
        PromotionReadyNotification notification, CancellationToken ct = default)
        => PostAsync("PromotionReady", _settings.Flows.PromotionReady, notification, ct);

    public Task<PowerAutomateResult> NotifySuccessionRiskAsync(
        SuccessionRiskNotification notification, CancellationToken ct = default)
        => PostAsync("SuccessionRisk", _settings.Flows.SuccessionRisk, notification, ct);

    public Task<PowerAutomateResult> NotifyCertificationExpirationAsync(
        CertificationExpirationNotification notification, CancellationToken ct = default)
        => PostAsync("CertificationExpiration", _settings.Flows.CertificationExpiration, notification, ct);

    public Task<PowerAutomateResult> NotifyDevelopmentPlanCreatedAsync(
        DevelopmentPlanNotification notification, CancellationToken ct = default)
        => PostAsync("DevelopmentPlanCreated", _settings.Flows.DevelopmentPlanCreated, notification, ct);

    // ── Core dispatch ─────────────────────────────────────────────────────────
public Task<PowerAutomateResult> NotifyTalentReviewCompletedAsync(
    TalentReviewNotification notification,
    CancellationToken ct = default)
    => PostAsync(
        "TalentReviewCompleted",
        _settings.Flows.TalentReviewCompleted,
        notification,
        ct);
    private async Task<PowerAutomateResult> PostAsync<T>(
        string flowName, string? flowUrl, T payload, CancellationToken ct)
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogDebug(
                "Power Automate notifications are disabled. Skipping flow '{FlowName}'.", flowName);
            return PowerAutomateResult.Disabled();
        }

        if (string.IsNullOrWhiteSpace(flowUrl))
        {
            _logger.LogWarning(
                "Power Automate flow URL for '{FlowName}' is not configured. "
                + "Set PowerAutomate:Flows:{FlowName} in appsettings.json.", flowName, flowName);
            return PowerAutomateResult.NotConfigured(flowName);
        }

        try
        {
            _logger.LogInformation(
                "Triggering Power Automate flow '{FlowName}' for payload type {PayloadType}.",
                flowName, typeof(T).Name);

            using var response = await _http.PostAsJsonAsync(flowUrl, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Power Automate flow '{FlowName}' succeeded (HTTP {StatusCode}).",
                    flowName, (int)response.StatusCode);
                return PowerAutomateResult.Ok();
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Power Automate flow '{FlowName}' returned HTTP {StatusCode}. Body: {Body}",
                flowName, (int)response.StatusCode, body);

            return PowerAutomateResult.Fail(
                $"Flow '{flowName}' failed with HTTP {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Power Automate flow '{FlowName}' timed out after {Timeout}s.",
                flowName, _settings.TimeoutSeconds);
            return PowerAutomateResult.Fail($"Flow '{flowName}' timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Network error while triggering Power Automate flow '{FlowName}'.", flowName);
            return PowerAutomateResult.Fail($"Network error for flow '{flowName}': {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while triggering Power Automate flow '{FlowName}'.", flowName);
            return PowerAutomateResult.Fail($"Unexpected error for flow '{flowName}': {ex.Message}");
        }
    }
}
