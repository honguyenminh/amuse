namespace Amuse.Domain.Platform;

public static class PlatformClaims
{
    public const string Root = "platform:root";
    public const string ManageOrganizations = "manage:platform:organizations";
    public const string ManageAll = "manage:platform:all";

    public static bool CanAssumeAnyOrganizationPersona(IReadOnlyList<string>? claims)
    {
        if (claims is null || claims.Count == 0)
            return false;

        var set = claims.ToHashSet(StringComparer.Ordinal);
        return set.Contains(Root)
               || set.Contains(ManageOrganizations)
               || set.Contains(ManageAll);
    }
}
