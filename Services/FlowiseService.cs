namespace SIRH.EY.Services;

public class FlowiseService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FlowiseService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string?> GetPredictionAsync(string userPrompt)
    {
        // Implementation placeholder - retourne une réponse simulée
        await Task.Delay(100);
        return $"Réponse simulée pour: {userPrompt}";
    }
}
