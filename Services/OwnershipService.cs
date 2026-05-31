using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;

namespace SIRH.EY.Services;

public class OwnershipService : IOwnershipService
{
    private readonly ApplicationDbContext _context;
    private readonly ITeamAccessService _teamAccess;

    public OwnershipService(ApplicationDbContext context, ITeamAccessService teamAccess)
    {
        _context = context;
        _teamAccess = teamAccess;
    }

    public async Task<bool> OwnsCompetenceAsync(ClaimsPrincipal user, int competenceId)
    {
        var competence = await _context.Competences
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == competenceId);

        if (competence == null) return false;

        return await _teamAccess.CanAccessCollaborateurAsync(user, competence.CollaborateurId);
    }

    public async Task<bool> OwnsInscriptionAsync(ClaimsPrincipal user, int inscriptionId)
    {
        var inscription = await _context.Inscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inscriptionId);

        if (inscription == null) return false;

        return await _teamAccess.CanAccessCollaborateurAsync(user, inscription.CollaborateurId);
    }

    public async Task<bool> OwnsOkrAsync(ClaimsPrincipal user, int okrId)
    {
        var okr = await _context.OKRs
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == okrId);

        if (okr == null) return false;

        return await _teamAccess.CanAccessCollaborateurAsync(user, okr.CollaborateurId);
    }
}
