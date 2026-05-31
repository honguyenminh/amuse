namespace Amuse.Domain.Tenancy;

public sealed record OrgCapabilities(
    bool CanReadOrg,
    bool CanReadMembership,
    bool CanUpload,
    bool CanWriteDraft,
    bool CanPublishPublic,
    bool CanReadPayout)
{
    public IReadOnlyList<string> ToClaimStrings()
    {
        var claims = new List<string>();
        if (CanReadOrg) claims.Add(OrgClaim.ScopeWideClaim("read", "org"));
        if (CanReadMembership) claims.Add(OrgClaim.ScopeWideClaim("read", "membership"));
        if (CanUpload) claims.Add(OrgClaim.ScopeWideClaim("upload", "catalog"));
        if (CanWriteDraft) claims.Add(OrgClaim.ScopeWideClaim("write_draft", "catalog"));
        if (CanPublishPublic) claims.Add(OrgClaim.ScopeWideClaim("publish_public", "catalog"));
        if (CanReadPayout) claims.Add(OrgClaim.ScopeWideClaim("read", "payout"));
        return claims;
    }

    public static IReadOnlyList<string> MergeClaims(
        IEnumerable<string> memberClaims,
        OrgCapabilities capabilities)
    {
        return memberClaims
            .Concat(capabilities.ToClaimStrings())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> FilterClaimsForCapabilities(
        IEnumerable<string> memberClaims,
        OrgCapabilities capabilities)
    {
        if (capabilities.CanUpload || capabilities.CanWriteDraft || capabilities.CanPublishPublic)
            return MergeClaims(memberClaims, capabilities);

        var normalizedMember = OrgClaim.NormalizeClaims(memberClaims);
        var filteredMember = normalizedMember
            .Where(c => !IsCatalogWriteClaim(c))
            .ToArray();

        return MergeClaims(filteredMember, capabilities);
    }

    public static IReadOnlyList<string> FilterAssignableClaims(
        IEnumerable<string> claims,
        OrgCapabilities capabilities)
    {
        var normalized = OrgClaim.NormalizeClaims(claims);
        return normalized
            .Where(c => IsAllowedForCapabilities(c, capabilities))
            .ToArray();
    }

    private static bool IsCatalogWriteClaim(string claim)
    {
        if (!OrgClaim.TryParse(claim, out var parsed))
            return false;

        return parsed.Scope == "catalog" && OrgClaim.IsCatalogWriteAction(parsed.Action);
    }

    private static bool IsAllowedForCapabilities(string claim, OrgCapabilities capabilities)
    {
        if (!OrgClaim.TryParse(claim, out var parsed))
            return false;

        return parsed.Scope switch
        {
            "org" => parsed.Action switch
            {
                "read" => capabilities.CanReadOrg,
                "manage" => capabilities.CanReadOrg,
                _ => false,
            },
            "membership" => parsed.Action switch
            {
                "read" => capabilities.CanReadMembership,
                "manage" => capabilities.CanReadMembership,
                _ => false,
            },
            "catalog" => parsed.Action switch
            {
                "read" => capabilities.CanReadOrg || capabilities.CanReadMembership,
                "upload" => capabilities.CanUpload,
                "write_draft" => capabilities.CanWriteDraft,
                "publish_public" => capabilities.CanPublishPublic,
                _ => false,
            },
            "payout" => parsed.Action == "read" && capabilities.CanReadPayout,
            _ => false,
        };
    }
}
