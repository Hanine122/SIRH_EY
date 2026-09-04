using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class ManagerActionCenterService : IManagerActionCenterService
{
    private readonly ApplicationDbContext _context;
    private readonly ITeamAccessService _teamAccess;

    public ManagerActionCenterService(ApplicationDbContext context, ITeamAccessService teamAccess)
    {
        _context = context;
        _teamAccess = teamAccess;
    }

    public async Task<IReadOnlyList<PendingValidationItem>> GetPendingValidationsAsync(ClaimsPrincipal user)
    {
        var teamIds = await GetTeamIdsAsync(user);
        return await GetPendingValidationsForTeamAsync(teamIds);
    }

    public async Task<HrInboxSummary> GetInboxAsync(ClaimsPrincipal user)
    {
        var teamIds = await GetTeamIdsAsync(user);

        var validations = await GetPendingValidationsForTeamAsync(teamIds);
        var plans = await GetPendingDevelopmentPlansForTeamAsync(teamIds);
        var certifications = await GetExpiringCertificationsForTeamAsync(teamIds);
        var ownId = await _teamAccess.GetCurrentCollaborateurIdAsync(user);
        var inscriptions = await GetPendingInscriptionApprovalsForTeamAsync(teamIds, ownId);

        return new HrInboxSummary(validations, plans, certifications, inscriptions);
    }

    private async Task<List<int>> GetTeamIdsAsync(ClaimsPrincipal user)
    {
        var teamQuery = await _teamAccess.ApplyAccessFilterAsync(user, _context.Collaborateurs);
        return await teamQuery.Select(c => c.Id).ToListAsync();
    }

    private async Task<IReadOnlyList<PendingValidationItem>> GetPendingValidationsForTeamAsync(IReadOnlyList<int> teamIds)
    {
        if (!teamIds.Any()) return Array.Empty<PendingValidationItem>();

        var pending = await _context.Competences
            .Include(c => c.EvaluationCompetence)
            .Include(c => c.Collaborateur)
            .Where(c => teamIds.Contains(c.CollaborateurId)
                     && c.EvaluationCompetence != null
                     && c.EvaluationCompetence.DateAutoEvaluation != null
                     && !c.EvaluationCompetence.ValidationManager)
            .OrderBy(c => c.EvaluationCompetence!.DateAutoEvaluation)
            .ToListAsync();

        var now = DateTime.Now;
        return pending
            .Select(c => new PendingValidationItem(
                c.Id,
                c.CollaborateurId,
                $"{c.Collaborateur?.Prenom} {c.Collaborateur?.Nom}".Trim(),
                c.Nom,
                c.EvaluationCompetence!.DateAutoEvaluation!.Value,
                Math.Max(0, (int)(now - c.EvaluationCompetence!.DateAutoEvaluation!.Value).TotalDays),
                c.NiveauCible))
            .ToList();
    }

    private async Task<IReadOnlyList<PendingDevelopmentPlanItem>> GetPendingDevelopmentPlansForTeamAsync(IReadOnlyList<int> teamIds)
    {
        if (!teamIds.Any()) return Array.Empty<PendingDevelopmentPlanItem>();

        var plans = await _context.PlansDeveloppement
            .Include(p => p.Formation)
            .Include(p => p.Collaborateur)
            .Where(p => teamIds.Contains(p.CollaborateurId) && p.Statut == "À faire")
            .OrderBy(p => p.DateRecommandation)
            .ToListAsync();

        var now = DateTime.Now;
        return plans
            .Select(p => new PendingDevelopmentPlanItem(
                p.Id,
                p.CollaborateurId,
                $"{p.Collaborateur?.Prenom} {p.Collaborateur?.Nom}".Trim(),
                p.Formation?.Titre ?? "Formation",
                p.DateRecommandation,
                Math.Max(0, (int)(now - p.DateRecommandation).TotalDays)))
            .ToList();
    }

    private async Task<IReadOnlyList<PendingInscriptionApprovalItem>> GetPendingInscriptionApprovalsForTeamAsync(
        IReadOnlyList<int> teamIds, int? ownId)
    {
        if (!teamIds.Any()) return Array.Empty<PendingInscriptionApprovalItem>();

        var pending = await _context.Inscriptions
            .Include(i => i.Formation)
            .Include(i => i.Collaborateur)
            .Where(i => teamIds.Contains(i.CollaborateurId)
                     && i.CollaborateurId != ownId
                     && i.Statut == StatutInscription.PendingApproval)
            .OrderBy(i => i.DateInscription)
            .ToListAsync();

        var now = DateTime.Now;
        return pending
            .Select(i => new PendingInscriptionApprovalItem(
                i.Id,
                i.CollaborateurId,
                $"{i.Collaborateur?.Prenom} {i.Collaborateur?.Nom}".Trim(),
                i.Formation?.Titre ?? "Formation",
                i.DateInscription,
                Math.Max(0, (int)(now - i.DateInscription).TotalDays)))
            .ToList();
    }

    private async Task<IReadOnlyList<ExpiringCertificationItem>> GetExpiringCertificationsForTeamAsync(IReadOnlyList<int> teamIds, int jours = 90)
    {
        if (!teamIds.Any()) return Array.Empty<ExpiringCertificationItem>();

        var limite = DateTime.Today.AddDays(jours);
        var certs = await _context.CollaborateurCertifications
            .Include(cc => cc.Collaborateur)
            .Include(cc => cc.Certification)
            .Where(cc => teamIds.Contains(cc.CollaborateurId)
                      && cc.Statut == "Active"
                      && cc.DateExpiration != null
                      && cc.DateExpiration >= DateTime.Today
                      && cc.DateExpiration <= limite)
            .OrderBy(cc => cc.DateExpiration)
            .ToListAsync();

        return certs
            .Select(cc =>
            {
                var joursRestants = (int)(cc.DateExpiration!.Value - DateTime.Today).TotalDays;
                return new ExpiringCertificationItem(
                    cc.Id,
                    cc.CollaborateurId,
                    $"{cc.Collaborateur?.Prenom} {cc.Collaborateur?.Nom}".Trim(),
                    cc.Certification?.Nom ?? "Certification",
                    cc.DateExpiration.Value,
                    joursRestants,
                    DecisionEngine.ClassifyCertificationUrgency(joursRestants));
            })
            .ToList();
    }
}
