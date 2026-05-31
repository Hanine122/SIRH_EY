using System.Security.Claims;

namespace SIRH.EY.Services;

public interface IOwnershipService
{
    /// <summary>True if the user may read/write the given Competence.</summary>
    Task<bool> OwnsCompetenceAsync(ClaimsPrincipal user, int competenceId);

    /// <summary>True if the user may read/write the given Inscription.</summary>
    Task<bool> OwnsInscriptionAsync(ClaimsPrincipal user, int inscriptionId);

    /// <summary>True if the user may read/write the given OKR.</summary>
    Task<bool> OwnsOkrAsync(ClaimsPrincipal user, int okrId);
}
