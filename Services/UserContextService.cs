using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class UserContextService : IUserContextService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserContextService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal == null || principal.Identity?.IsAuthenticated == false)
            return null;

        return await _userManager.GetUserAsync(principal);
    }

    public async Task<Collaborateur?> GetCurrentCollaborateurAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user == null) return null;

        return await _context.Collaborateurs
            .Include(c => c.Manager)
            .FirstOrDefaultAsync(c => c.UserId == user.Id || c.Email == user.Email);
    }

    public async Task<List<Collaborateur>> GetTeamMembersAsync(ClaimsPrincipal principal)
    {
        var collab = await GetCurrentCollaborateurAsync(principal);
        if (collab == null) return new List<Collaborateur>();

        if (principal.IsInRole("RH"))
        {
            // RH/Admin sees everyone
            return await _context.Collaborateurs.Where(c => c.Actif).ToListAsync();
        }
        else if (principal.IsInRole("Manager"))
        {
            // Manager sees only direct reports
            return await _context.Collaborateurs
                .Where(c => c.ManagerId == collab.Id && c.Actif)
                .ToListAsync();
        }

        // Standard employee sees no one in their team by default except maybe themselves
        return new List<Collaborateur> { collab };
    }

    public async Task<bool> IsUserInRoleAsync(ClaimsPrincipal principal, string role)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user == null) return false;

        return await _userManager.IsInRoleAsync(user, role);
    }
}
