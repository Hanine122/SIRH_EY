using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

public interface IWorkforceImpactService
{
    Task<WorkforceImpactSimulatorViewModel> BuildSimulatorAsync();
    Task<WorkforceImpactResultViewModel?> SimulateAsync(int collaborateurId);
}
