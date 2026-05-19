using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

public interface IPromotionReadinessService
{
    Task<PromotionReadinessSimulatorViewModel> BuildSimulatorAsync();
    Task<PromotionReadinessResultViewModel?> SimulateAsync(int collaborateurId, string targetKey);
}
