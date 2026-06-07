using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

internal sealed class CatalogManagedArtistVisibility(CatalogDbContext db) : ICatalogManagedArtistVisibility
{
    public async Task SyncManagedArtistsForOrganizationAsync(
        OrganizationId organizationId,
        OrganizationTrustTier organizationTrustTier,
        CancellationToken cancellationToken = default)
    {
        var visibilityTier = ArtistVisibilityTierMapper.FromOrganizationTrustTier(organizationTrustTier);
        var artists = await db.Artists
            .Where(a => a.ManagingOrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (artists.Count == 0)
            return;

        foreach (var artist in artists)
            artist.SetVisibilityTier(visibilityTier);

        await db.SaveChangesAsync(cancellationToken);
    }
}
