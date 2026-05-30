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
        if (CanReadOrg) claims.Add("org:read");
        if (CanReadMembership) claims.Add("membership:read");
        if (CanUpload) claims.Add("catalog:upload");
        if (CanWriteDraft) claims.Add("catalog:write_draft");
        if (CanPublishPublic) claims.Add("catalog:publish_public");
        if (CanReadPayout) claims.Add("payout:read");
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

        return capabilities.ToClaimStrings();
    }
}
