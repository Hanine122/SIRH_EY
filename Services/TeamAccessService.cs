using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Authorization;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class TeamAccessService : ITeamAccessService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamAccessService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public bool IsPrivileged(ClaimsPrincipal user)
        => user.IsInRole(Roles.ITAdmin) || user.IsInRole(Roles.RH);

    public async Task<bool> CanAccessCollaborateurAsync(ClaimsPrincipal user, int collaborateurId)
    {
        // Privileged roles: unrestricted
        if (IsPrivileged(user))
            return true;

        var currentId = await GetCurrentCollaborateurIdAsync(user);
        if (currentId == null)
            return false;

        // Manager: own record + direct reports
        if (user.IsInRole(Roles.Manager))
        {
            if (currentId.Value == collaborateurId)
                return true;

            return await _context.Collaborateurs
                .AnyAsync(c => c.Id == collaborateurId && c.ManagerId == currentId.Value);
        }

        // Collaborateur: own record only
        if (user.IsInRole(Roles.Collaborateur))
            return currentId.Value == collaborateurId;

        return false;
    }

    public async Task<IQueryable<Collaborateur>> ApplyAccessFilterAsync(
        ClaimsPrincipal user,
        IQueryable<Collaborateur> query)
    {
        if (IsPrivileged(user))
            return query;

        var currentId = await GetCurrentCollaborateurIdAsync(user);

        // No linked collaborateur → empty result-set
        if (currentId == null)
            return query.Where(_ => false);

        int ownId = currentId.Value;

        if (user.IsInRole(Roles.Manager))
            return query.Where(c => c.Id == ownId || c.ManagerId == ownId);

        // Collaborateur (or unrecognised role): own record only
        return query.Where(c => c.Id == ownId);
    }

    public async Task<int?> GetCurrentCollaborateurIdAsync(ClaimsPrincipal user)
    {
        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null)
            return null;

        var collab = await _context.Collaborateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == appUser.Id);

        return collab?.Id;
    }
}
