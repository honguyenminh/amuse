namespace Amuse.Domain.Platform;

/// <summary>
/// Single source of truth for platform operator claim semantics.
/// Authorization handlers, token mint, and tenancy instant-approve must use these helpers —
/// do not duplicate claim string checks elsewhere.
/// </summary>
public static class PlatformClaims
{
    public const string Root = "platform:root";
    public const string ReviewOrganizations = "review:platform:organizations";
    public const string ManageOrganizations = "manage:platform:organizations";
    public const string ManageAll = "manage:platform:all";
    public const string ReadAccounting = "read:platform:accounting:all";
    public const string ManageAccounting = "manage:platform:accounting:all";
    public const string ManagePurchases = "manage:platform:purchases:all";
    public const string ManagePayouts = "manage:platform:payouts:all";

    private const string LegacyReviewOrganizations = "platform:organizations:review";

    /// <summary>
    /// Expands stored operator claims for JWT mint and lookups.
    /// <see cref="Root"/> implies full platform organization manage + review capabilities.
    /// </summary>
    public static IReadOnlyList<string> ExpandEffectiveClaims(
        IReadOnlyList<string> storedClaims,
        bool isRootOperator)
    {
        var set = ToSet(storedClaims);
        if (isRootOperator)
            set.Add(Root);

        if (set.Contains(Root) || set.Contains(ManageAll))
        {
            set.Add(ManageOrganizations);
            set.Add(ReviewOrganizations);
            set.Add(ManageAll);
            set.Add(ReadAccounting);
            set.Add(ManageAccounting);
            set.Add(ManagePurchases);
            set.Add(ManagePayouts);
        }

        return set.OrderBy(c => c, StringComparer.Ordinal).ToArray();
    }

    public static bool CanReviewOrganizations(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ReviewOrganizations, ManageOrganizations, ManageAll, LegacyReviewOrganizations);

    public static bool CanManageOrganizations(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ManageOrganizations, ManageAll);

    public static bool CanReadAccounting(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ManageAll, ReadAccounting, ManageAccounting);

    public static bool CanManageAccounting(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ManageAll, ManageAccounting);

    public static bool CanManagePurchases(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ManageAll, ManagePurchases);

    public static bool CanManagePayouts(IReadOnlyList<string>? claims) =>
        HasAny(claims, Root, ManageAll, ManagePayouts);

    public static bool CanInstantApproveOrganizationsOnCreate(IReadOnlyList<string>? claims) =>
        CanReviewOrganizations(claims);

    public static bool CanAssumeAnyOrganizationPersona(IReadOnlyList<string>? claims) =>
        CanManageOrganizations(claims);

    private static bool HasAny(IReadOnlyList<string>? claims, params string[] required)
    {
        if (claims is null || claims.Count == 0)
            return false;

        var set = ToSet(claims);
        return required.Any(set.Contains);
    }

    private static HashSet<string> ToSet(IReadOnlyList<string> claims) =>
        claims
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToHashSet(StringComparer.Ordinal);
}
