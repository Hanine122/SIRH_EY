namespace SIRH.EY.Authorization;

/// <summary>
/// Named policy keys for use with [Authorize(Policy = "...")].
/// Policies are registered in Program.cs.
/// </summary>
public static class Policies
{
    /// <summary>Requires ITAdmin or RH role.</summary>
    public const string HrPrivileged = "HrPrivileged";

    /// <summary>Requires ITAdmin, RH, or Manager role.</summary>
    public const string ManagerOrAbove = "ManagerOrAbove";

    /// <summary>Requires ITAdmin role only.</summary>
    public const string ITAdminOnly = "ITAdminOnly";
}
