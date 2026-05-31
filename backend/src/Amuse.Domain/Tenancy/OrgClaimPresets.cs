namespace Amuse.Domain.Tenancy;

public static class OrgClaimPresets
{
    public const string OwnerPresetLabel = "admin";
    public const string MemberManagerPresetLabel = "member_manager";
    public const string CatalogEditorPresetLabel = "catalog_editor";
    public const string ViewerPresetLabel = "viewer";

    public static readonly IReadOnlyList<string> OwnerAdmin =
    [
        OrgClaim.ScopeWideClaim("read", "org"),
        OrgClaim.ScopeWideClaim("manage", "org"),
        OrgClaim.ScopeWideClaim("read", "membership"),
        OrgClaim.ScopeWideClaim("manage", "membership"),
        OrgClaim.MemberPermissionsClaim,
        OrgClaim.ScopeWideClaim("read", "catalog"),
        OrgClaim.ScopeWideClaim("upload", "catalog"),
        OrgClaim.ScopeWideClaim("write_draft", "catalog"),
    ];

    public static readonly IReadOnlyList<string> MemberManager =
    [
        OrgClaim.ScopeWideClaim("read", "org"),
        OrgClaim.ScopeWideClaim("read", "membership"),
        OrgClaim.ScopeWideClaim("manage", "membership"),
    ];

    public static readonly IReadOnlyList<string> CatalogEditor =
    [
        OrgClaim.ScopeWideClaim("read", "org"),
        OrgClaim.ScopeWideClaim("read", "catalog"),
        OrgClaim.ScopeWideClaim("upload", "catalog"),
        OrgClaim.ScopeWideClaim("write_draft", "catalog"),
    ];

    public static readonly IReadOnlyList<string> Viewer =
    [
        OrgClaim.ScopeWideClaim("read", "org"),
        OrgClaim.ScopeWideClaim("read", "membership"),
        OrgClaim.ScopeWideClaim("read", "catalog"),
    ];

    public static IReadOnlyList<OrgClaimPresetDefinition> AllDefinitions { get; } =
    [
        new(
            OwnerPresetLabel,
            "Administrator",
            "Full organization administrator with settings, membership, and catalog access.",
            "shield-check",
            OwnerAdmin),
        new(
            MemberManagerPresetLabel,
            "Member manager",
            "Invite members, remove members, and manage pending invitations.",
            "users",
            MemberManager),
        new(
            CatalogEditorPresetLabel,
            "Catalog editor",
            "Upload masters and create or edit catalog drafts.",
            "disc-3",
            CatalogEditor),
        new(
            ViewerPresetLabel,
            "Viewer",
            "Read-only access to organization, membership, and catalog data.",
            "eye",
            Viewer),
    ];

    public static bool TryResolveClaims(string? presetRoleLabel, IReadOnlyList<string>? explicitClaims, out IReadOnlyList<string> claims)
    {
        claims = [];
        if (!string.IsNullOrWhiteSpace(presetRoleLabel))
        {
            var preset = AllDefinitions.FirstOrDefault(p =>
                string.Equals(p.Label, presetRoleLabel.Trim(), StringComparison.OrdinalIgnoreCase));
            if (preset is null)
                return false;

            claims = preset.Claims;
            return true;
        }

        if (explicitClaims is null || explicitClaims.Count == 0)
            return false;

        claims = OrgClaim.NormalizeClaims(explicitClaims);
        if (claims.Count != explicitClaims.Count(c => !string.IsNullOrWhiteSpace(c)))
            return false;

        return true;
    }
}

public sealed record OrgClaimPresetDefinition(
    string Label,
    string DisplayName,
    string Description,
    string Icon,
    IReadOnlyList<string> Claims);
