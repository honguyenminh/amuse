namespace Amuse.Domain.Tenancy;

public static class OrgClaimPresets
{
    public const string OwnerPresetLabel = "admin";

    public static readonly IReadOnlyList<string> OwnerAdmin =
    [
        "org:read",
        "org:manage",
        "membership:read",
        "membership:manage",
        "catalog:read",
        "catalog:upload",
        "catalog:write_draft",
    ];
}
