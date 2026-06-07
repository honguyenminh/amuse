using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using CatalogSlugHelper = Amuse.Modules.Catalog.Features.Common.CatalogSlugHelper;

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

        return await BuildResponseAsync(group, cancellationToken);
    }

    public async Task<Result<GetReleaseGroupDetailResponse>> HandleBySlugsAsync(
        string artistSlug,
        string groupSlug,
        CancellationToken cancellationToken)
    {
        var artistParse = CatalogSlugHelper.TryParseArtistSlug(artistSlug);
        var groupParse = CatalogSlugHelper.TryParseReleaseSlug(groupSlug);
        if (!artistParse.IsSuccess || !groupParse.IsSuccess)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var artist = await db.Artists
            .AsNoTracking()
            .Where(a => a.Slug == artistParse.Value!)
            .Select(a => new { a.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (artist is null)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var group = await db.ReleaseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.ArtistId == artist.Id && g.Slug == groupParse.Value!,
                cancellationToken);

        if (group is null)
            return Result<GetReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        return await BuildResponseAsync(group, cancellationToken);
    }

    private async Task<Result<GetReleaseGroupDetailResponse>> BuildResponseAsync(
        ReleaseGroup group,
        CancellationToken cancellationToken)
    {
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
                r.ReleaseGroupId == group.Id
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

        endpoints.MapGet("/api/v1/catalog/artists/{artistSlug}/release-groups/{groupSlug}", async (
                string artistSlug,
                string groupSlug,
                GetPublicReleaseGroupDetailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleBySlugsAsync(artistSlug, groupSlug, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetPublicReleaseGroupDetailBySlugs")
            .WithSummary(
                "Get a release group and its published editions by artist and group URL slugs. Public; no authentication required.")
            .Produces<GetReleaseGroupDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
