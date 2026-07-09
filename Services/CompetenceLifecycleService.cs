using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class CompetenceLifecycleService : ICompetenceLifecycleService
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanDeveloppementService _planDeveloppementService;

    public CompetenceLifecycleService(ApplicationDbContext context, IPlanDeveloppementService planDeveloppementService)
    {
        _context = context;
        _planDeveloppementService = planDeveloppementService;
    }

    public async Task RecordLevelChangeAsync(Competence competence, int ancienNiveau, int nouveauNiveau, string raison)
    {
        _context.EvaluationsHistoriques.Add(new EvaluationHistorique
        {
            CompetenceId = competence.Id,
            NiveauAncien = ancienNiveau,
            NiveauNouveau = nouveauNiveau,
            DateChangement = DateTime.Now,
            Raison = raison
        });

        await _planDeveloppementService.FermerPlansSiObjectifAtteintAsync(competence);
    }
}
