namespace SIRH.EY.Authorization;

/// <summary>
/// Centralised role name constants. Use these everywhere instead of raw strings.
/// </summary>
public static class Roles
{
    public const string ITAdmin     = "ITAdmin";
    public const string RH          = "RH";
    public const string Manager     = "Manager";
    public const string Collaborateur = "Collaborateur";

    // ── Composite role strings for [Authorize(Roles = "...")] ────────────────
    /// <summary>ITAdmin or RH — HR privileged access.</summary>
    public const string ITAdminOrRH = ITAdmin + "," + RH;

    /// <summary>ITAdmin, RH, or Manager — people-management access.</summary>
    public const string ITAdminOrRHOrManager = ITAdmin + "," + RH + "," + Manager;

    /// <summary>All authenticated roles.</summary>
    public const string All = ITAdmin + "," + RH + "," + Manager + "," + Collaborateur;
}
