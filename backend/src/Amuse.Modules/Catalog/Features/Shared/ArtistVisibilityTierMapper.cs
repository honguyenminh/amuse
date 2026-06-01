using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class ArtistVisibilityTierMapper
{
    internal static ArtistVisibilityTier FromOrganizationTrustTier(OrganizationTrustTier trustTier) =>
        trustTier == OrganizationTrustTier.PlatformVerified
            ? ArtistVisibilityTier.PlatformVerified
            : ArtistVisibilityTier.Unverified;
}
