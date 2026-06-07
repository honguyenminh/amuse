using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetReleaseGroupDetail;

public sealed record ReleaseEditionSummary(
    Guid Id,
    string Slug,
    string Title,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl);

public sealed record GetReleaseGroupDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    Guid ArtistId,
    string ArtistName,
    string ArtistSlug,
    IReadOnlyList<ReleaseEditionSummary> Releases);

internal sealed class GetPublicReleaseGroupDetailHandler(CatalogDbContext db, IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<GetReleaseGroupDetailResponse>> HandleAsync(
        Guid releaseGroupId,
        CancellationToken cancellationToken)
    {
        if (releaseGroupId == Guid.Empty)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var typedId = ReleaseGroupId.From(releaseGroupId);
        var group = await db.ReleaseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == typedId, cancellationToken);

        if (group is null)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == group.ArtistId)
            .Select(a => new { a.Id, a.Name, a.Slug })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var releases = await db.Releases
            .AsNoTracking()
            .Where(r =>
                r.ReleaseGroupId == typedId
                && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .OrderByDescending(r => r.ReleaseDate)
            .Select(r => new ReleaseEditionSummary(
                r.Id.Value,
                r.Slug.Value,
                r.Title,
                r.ReleaseType,
                r.ReleaseDate,
                mediaUrls.BuildCoverArtUrl(r.CoverArtKey)))
            .ToListAsync(cancellationToken);

        return Result<GetReleaseGroupDetailResponse>.Success(
            new GetReleaseGroupDetailResponse(
                group.Id.Value,
                group.Slug.Value,
                group.Title,
                group.Description,
                artist.Id.Value,
                artist.Name,
                artist.Slug.Value,
                releases));
    }
}

public static class GetPublicReleaseGroupDetailEndpoint
{
    public static IEndpointRouteBuilder MapGetPublicReleaseGroupDetailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/release-groups/{releaseGroupId:guid}", async (
                Guid releaseGroupId,
                GetPublicReleaseGroupDetailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseGroupId, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetPublicReleaseGroupDetail")
            .WithSummary("Get a release group and its published editions.")
            .Produces<GetReleaseGroupDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
