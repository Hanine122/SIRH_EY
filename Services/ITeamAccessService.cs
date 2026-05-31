using System.Security.Claims;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public interface ITeamAccessService
{
    /// <summary>
    /// Returns true if the current principal is allowed to read/write data
    /// for the given collaborateur.
    /// </summary>
    Task<bool> CanAccessCollaborateurAsync(ClaimsPrincipal user, int collaborateurId);

    /// <summary>
    /// Applies a WHERE filter to a Collaborateurs queryable so only
    /// records accessible to the current principal are returned.
    /// </summary>
    Task<IQueryable<Collaborateur>> ApplyAccessFilterAsync(
        ClaimsPrincipal user,
        IQueryable<Collaborateur> query);

    /// <summary>Returns the Collaborateur.Id linked to the current user, or null.</summary>
    Task<int?> GetCurrentCollaborateurIdAsync(ClaimsPrincipal user);

    /// <summary>True if the user holds ITAdmin or RH role.</summary>
    bool IsPrivileged(ClaimsPrincipal user);
}
