namespace Amuse.Domain.Platform;

public static class PlatformClaims
{
    public const string Root = "platform:root";
    public const string ReviewOrganizations = "review:platform:organizations";
    public const string ManageOrganizations = "manage:platform:organizations";
    public const string ManageAll = "manage:platform:all";

    private const string LegacyReviewOrganizations = "platform:organizations:review";

    public static bool CanInstantApproveOrganizationsOnCreate(IReadOnlyList<string>? claims)
    {
        if (claims is null || claims.Count == 0)
            return false;

        var set = claims.ToHashSet(StringComparer.Ordinal);
        return set.Contains(Root)
               || set.Contains(ManageOrganizations)
               || set.Contains(ManageAll)
               || set.Contains(ReviewOrganizations)
               || set.Contains(LegacyReviewOrganizations);
    }

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
