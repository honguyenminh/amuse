using System.Text.Json;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class OrganizationTrustTierJson
{
    private static readonly JsonNamingPolicy EnumNaming = JsonNamingPolicy.CamelCase;

    internal static string ToApiValue(OrganizationTrustTier trustTier) =>
        EnumNaming.ConvertName(trustTier.ToString());

    internal static bool IsPlatformVerified(OrganizationTrustTier trustTier) =>
        trustTier == OrganizationTrustTier.PlatformVerified;

    internal static OrganizationTrustTier DefaultWhenMissing =>
        OrganizationTrustTier.Unverified;
}
