using SIRH.EY.Models;

namespace SIRH.EY.Services;

public record PlanDeveloppementResult(int CreatedCount, string Message);

public interface IPlanDeveloppementService
{
    Task<PlanDeveloppementResult> GenererPourCollaborateurAsync(int collaborateurId);

    // Referme les PlanDeveloppement ouverts dont l'objectif de niveau vient d'etre atteint,
    // via le meme lien Formation.CompetenceVisee deja utilise par TerminerFormation/Terminer.
    Task FermerPlansSiObjectifAtteintAsync(Competence competence);
}
