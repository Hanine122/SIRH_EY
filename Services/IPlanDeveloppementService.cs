namespace SIRH.EY.Services;

public record PlanDeveloppementResult(int CreatedCount, string Message);

public interface IPlanDeveloppementService
{
    Task<PlanDeveloppementResult> GenererPourCollaborateurAsync(int collaborateurId);
}
