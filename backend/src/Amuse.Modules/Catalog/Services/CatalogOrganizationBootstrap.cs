using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

internal sealed class CatalogOrganizationBootstrap(CatalogDbContext db) : ICatalogOrganizationBootstrap
{
    public async Task<Result<Guid>> CreateDefaultArtistAsync(
        OrganizationId organizationId,
        string displayName,
        OrganizationTrustTier organizationTrustTier,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var slugResult = await CatalogSlugHelper.AllocateUniqueArtistSlugAsync(db, displayName, cancellationToken);
        if (!slugResult.IsSuccess)
            return Result<Guid>.Failure(slugResult.Error!);

        var visibilityTier = ArtistVisibilityTierMapper.FromOrganizationTrustTier(organizationTrustTier);

        var artistResult = Artist.Create(
            ArtistId.New(),
            displayName,
            slugResult.Value!,
            now,
            managingOrganizationId: organizationId,
            visibilityTier);

        if (!artistResult.IsSuccess)
            return Result<Guid>.Failure(artistResult.Error!);

        var artist = artistResult.Value!;
        db.Artists.Add(artist);
        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(artist.Id.Value);
    }
}
