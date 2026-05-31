namespace Amuse.Domain.Tenancy;

/// <summary>
/// Claim string <c>{action}:{scope}:{target}</c> where target is <c>all</c> or <c>{resourceKind}:{resourceId}</c>.
/// </summary>
public readonly record struct OrgClaim
{
    public const string TargetAll = "all";
    public const string MemberPermissionsClaim = "manage:member_permissions:all";

    public string Action { get; }
    public string Scope { get; }
    public string Target { get; }
    public string? ResourceKind { get; }
    public string? ResourceId { get; }

    public string Value { get; }

    private OrgClaim(
        string action,
        string scope,
        string target,
        string? resourceKind,
        string? resourceId,
        string value)
    {
        Action = action;
        Scope = scope;
        Target = target;
        ResourceKind = resourceKind;
        ResourceId = resourceId;
        Value = value;
    }

    public static OrgClaim From(string value)
    {
        if (!TryParse(value, out var claim))
            throw new ArgumentException($"Invalid org claim '{value}'.", nameof(value));
        return claim;
    }

    public static bool TryParse(string? value, out OrgClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        var parts = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is not (3 or 4))
            return false;

        var action = parts[0].ToLowerInvariant();
        var scope = parts[1].ToLowerInvariant();
        if (!IsKnownAction(action) || !IsKnownScope(scope))
            return false;

        if (parts.Length == 3)
        {
            var target = parts[2].ToLowerInvariant();
            if (scope == "platform")
            {
                if (target is not (TargetAll or "organizations"))
                    return false;
            }
            else if (!string.Equals(target, TargetAll, StringComparison.Ordinal))
            {
                return false;
            }

            var claimValue = $"{action}:{scope}:{target}";
            claim = new OrgClaim(action, scope, target, null, null, claimValue);
            return true;
        }

        var resourceKind = parts[2].ToLowerInvariant();
        var resourceId = parts[3];
        if (!IsKnownCatalogResourceKind(resourceKind) || !Guid.TryParse(resourceId, out _))
            return false;

        var formatted = $"{action}:{scope}:{resourceKind}:{resourceId}";
        claim = new OrgClaim(action, scope, $"{resourceKind}:{resourceId}", resourceKind, resourceId, formatted);
        return true;
    }

    public static bool IsValid(string? value) => TryParse(value, out _);

    public static string ScopeWideClaim(string action, string scope) =>
        $"{action.ToLowerInvariant()}:{scope.ToLowerInvariant()}:{TargetAll}";

    public static bool Matches(string requiredClaim, IReadOnlySet<string> grantedClaims)
    {
        if (grantedClaims.Contains(requiredClaim))
            return true;

        if (!TryParse(requiredClaim, out var required))
            return false;

        var scopeWide = ScopeWideClaim(required.Action, required.Scope);
        return grantedClaims.Contains(scopeWide);
    }

    public static bool MatchesAny(IEnumerable<string> requiredClaims, IReadOnlySet<string> grantedClaims) =>
        requiredClaims.Any(r => Matches(r, grantedClaims));

    public static bool MatchesAll(IEnumerable<string> requiredClaims, IReadOnlySet<string> grantedClaims) =>
        requiredClaims.All(r => Matches(r, grantedClaims));

    public static IReadOnlyList<string> NormalizeClaims(IEnumerable<string> claims)
    {
        var normalized = new List<string>();
        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim))
                continue;

            if (TryParse(claim, out var parsed))
                normalized.Add(parsed.Value);
            else if (TryMigrateLegacy(claim.Trim(), out var migrated))
                normalized.Add(migrated);
        }

        return normalized
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool ContainsAdminEquivalent(IReadOnlyList<string> claims)
    {
        var set = claims.ToHashSet(StringComparer.Ordinal);
        foreach (var required in OrgClaimPresets.OwnerAdmin)
        {
            if (!Matches(required, set))
                return false;
        }

        return true;
    }

    public static bool IsCatalogWriteAction(string action) =>
        action is "upload" or "write_draft" or "publish_public";

    public static bool TryMigrateLegacy(string legacy, out string migrated)
    {
        migrated = LegacyMap.GetValueOrDefault(legacy, string.Empty);
        return migrated.Length > 0;
    }

    private static bool IsKnownAction(string action) =>
        action is "read" or "manage" or "upload" or "write_draft" or "publish_public" or "review";

    private static bool IsKnownScope(string scope) =>
        scope is "org" or "membership" or "member_permissions" or "catalog" or "payout" or "platform";

    private static bool IsKnownCatalogResourceKind(string kind) =>
        kind is "artist" or "release" or "track" or "release_group";

    private static readonly Dictionary<string, string> LegacyMap = new(StringComparer.Ordinal)
    {
        ["org:read"] = "read:org:all",
        ["org:manage"] = "manage:org:all",
        ["membership:read"] = "read:membership:all",
        ["membership:manage"] = "manage:membership:all",
        ["catalog:read"] = "read:catalog:all",
        ["catalog:upload"] = "upload:catalog:all",
        ["catalog:write_draft"] = "write_draft:catalog:all",
        ["catalog:publish_public"] = "publish_public:catalog:all",
        ["payout:read"] = "read:payout:all",
        ["platform:organizations:review"] = "review:platform:organizations",
        ["platform:admin"] = "review:platform:organizations",
    };
}
