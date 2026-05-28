using System.Security.Claims;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public interface IUserContextService
{
    Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task<Collaborateur?> GetCurrentCollaborateurAsync(ClaimsPrincipal principal);
    Task<List<Collaborateur>> GetTeamMembersAsync(ClaimsPrincipal principal);
    Task<bool> IsUserInRoleAsync(ClaimsPrincipal principal, string role);
}
