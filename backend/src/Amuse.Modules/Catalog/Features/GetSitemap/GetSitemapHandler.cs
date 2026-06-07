using System.Text.Json.Serialization;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetSitemap;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SitemapArtistEntry), "artist")]
[JsonDerivedType(typeof(SitemapReleaseEntry), "release")]
[JsonDerivedType(typeof(SitemapReleaseGroupEntry), "releaseGroup")]
[JsonDerivedType(typeof(SitemapPlaylistEntry), "playlist")]
public abstract record SitemapEntry(DateTimeOffset LastModified);

public sealed record SitemapArtistEntry(string ArtistSlug, DateTimeOffset LastModified)
    : SitemapEntry(LastModified);

public sealed record SitemapReleaseEntry(
    string ArtistSlug,
    string ReleaseSlug,
    DateTimeOffset LastModified) : SitemapEntry(LastModified);

public sealed record SitemapReleaseGroupEntry(
    string ArtistSlug,
    string GroupSlug,
    DateTimeOffset LastModified) : SitemapEntry(LastModified);

public sealed record SitemapPlaylistEntry(Guid PlaylistId, DateTimeOffset LastModified)
    : SitemapEntry(LastModified);

public sealed record GetSitemapResponse(
    IReadOnlyList<SitemapEntry> Entries,
    string? NextCursor);

internal sealed class GetSitemapHandler(CatalogDbContext catalogDb, DiscoveryDbContext discoveryDb)
{
    private const int DefaultPageSize = 5000;
    private const int MaxPageSize = 5000;

    private sealed record SitemapRow(SitemapSegment Segment, Guid SortId, SitemapEntry Entry);

    public async Task<Result<GetSitemapResponse>> HandleAsync(
        string? cursor,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var (segment, lastId) = ParseCursor(cursor);

        var buffer = new List<SitemapRow>(take + 1);

        if (segment == SitemapSegment.Artist)
            await AppendArtistRowsAsync(buffer, take + 1, lastId, cancellationToken);

        if (buffer.Count <= take && segment <= SitemapSegment.Release)
            await AppendReleaseRowsAsync(
                buffer,
                take + 1,
                segment == SitemapSegment.Release ? lastId : null,
                cancellationToken);

        if (buffer.Count <= take && segment <= SitemapSegment.ReleaseGroup)
            await AppendReleaseGroupRowsAsync(
                buffer,
                take + 1,
                segment == SitemapSegment.ReleaseGroup ? lastId : null,
                cancellationToken);

        if (buffer.Count <= take && segment <= SitemapSegment.Playlist)
            await AppendPlaylistRowsAsync(
                buffer,
                take + 1,
                segment == SitemapSegment.Playlist ? lastId : null,
                cancellationToken);

        var hasMore = buffer.Count > take;
        var page = buffer.Take(take).Select(row => row.Entry).ToList();
        string? nextCursor = hasMore && buffer.Count > take
            ? EncodeCursor(buffer[take - 1])
            : null;

        return Result<GetSitemapResponse>.Success(new GetSitemapResponse(page, nextCursor));
    }

    private async Task AppendArtistRowsAsync(
        List<SitemapRow> rows,
        int limit,
        Guid? lastId,
        CancellationToken cancellationToken)
    {
        var remaining = limit - rows.Count;
        if (remaining <= 0)
            return;

        var candidates = await catalogDb.Artists.AsNoTracking()
            .Where(a => catalogDb.Releases.Any(r =>
                r.ArtistId == a.Id && r.LifecycleStatus == ReleaseLifecycleStatus.Published))
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                Slug = a.Slug.Value,
                LastModified = catalogDb.Releases
                    .Where(r => r.ArtistId == a.Id && r.LifecycleStatus == ReleaseLifecycleStatus.Published)
                    .Max(r => (DateTimeOffset?)r.UpdatedAt) ?? a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in candidates.Where(row => lastId is null || row.Id.Value > lastId).Take(remaining))
        {
            rows.Add(new SitemapRow(
                SitemapSegment.Artist,
                row.Id.Value,
                new SitemapArtistEntry(row.Slug, row.LastModified)));
        }
    }

    private async Task AppendReleaseRowsAsync(
        List<SitemapRow> rows,
        int limit,
        Guid? lastId,
        CancellationToken cancellationToken)
    {
        var remaining = limit - rows.Count;
        if (remaining <= 0)
            return;

        var candidates = await catalogDb.Releases.AsNoTracking()
            .Where(r => r.LifecycleStatus == ReleaseLifecycleStatus.Published)
            .OrderBy(r => r.Id)
            .Join(
                catalogDb.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new
                {
                    release.Id,
                    ArtistSlug = artist.Slug.Value,
                    ReleaseSlug = release.Slug.Value,
                    release.UpdatedAt,
                })
            .ToListAsync(cancellationToken);

        foreach (var row in candidates.Where(row => lastId is null || row.Id.Value > lastId).Take(remaining))
        {
            rows.Add(new SitemapRow(
                SitemapSegment.Release,
                row.Id.Value,
                new SitemapReleaseEntry(row.ArtistSlug, row.ReleaseSlug, row.UpdatedAt)));
        }
    }

    private async Task AppendReleaseGroupRowsAsync(
        List<SitemapRow> rows,
        int limit,
        Guid? lastId,
        CancellationToken cancellationToken)
    {
        var remaining = limit - rows.Count;
        if (remaining <= 0)
            return;

        var candidates = await catalogDb.ReleaseGroups.AsNoTracking()
            .Where(g => catalogDb.Releases.Any(r =>
                r.ReleaseGroupId == g.Id && r.LifecycleStatus == ReleaseLifecycleStatus.Published))
            .OrderBy(g => g.Id)
            .Join(
                catalogDb.Artists.AsNoTracking(),
                group => group.ArtistId,
                artist => artist.Id,
                (group, artist) => new
                {
                    group.Id,
                    ArtistSlug = artist.Slug.Value,
                    GroupSlug = group.Slug.Value,
                    group.UpdatedAt,
                })
            .ToListAsync(cancellationToken);

        foreach (var row in candidates.Where(row => lastId is null || row.Id.Value > lastId).Take(remaining))
        {
            rows.Add(new SitemapRow(
                SitemapSegment.ReleaseGroup,
                row.Id.Value,
                new SitemapReleaseGroupEntry(row.ArtistSlug, row.GroupSlug, row.UpdatedAt)));
        }
    }

    private async Task AppendPlaylistRowsAsync(
        List<SitemapRow> rows,
        int limit,
        Guid? lastId,
        CancellationToken cancellationToken)
    {
        var remaining = limit - rows.Count;
        if (remaining <= 0)
            return;

        var candidates = await discoveryDb.Playlists.AsNoTracking()
            .Where(p => p.Visibility == PlaylistVisibility.Public && p.Kind == PlaylistKind.User)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.UpdatedAt })
            .ToListAsync(cancellationToken);

        foreach (var row in candidates.Where(row => lastId is null || row.Id.Value > lastId).Take(remaining))
        {
            rows.Add(new SitemapRow(
                SitemapSegment.Playlist,
                row.Id.Value,
                new SitemapPlaylistEntry(row.Id.Value, row.UpdatedAt)));
        }
    }

    private static string EncodeCursor(SitemapRow row) => $"{row.Segment}:{row.SortId}";

    private static (SitemapSegment Segment, Guid? LastId) ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return (SitemapSegment.Artist, null);

        var separatorIndex = cursor.IndexOf(':');
        if (separatorIndex <= 0)
            return (SitemapSegment.Artist, null);

        var segmentRaw = cursor[..separatorIndex];
        var payload = cursor[(separatorIndex + 1)..];

        if (!Enum.TryParse<SitemapSegment>(segmentRaw, ignoreCase: true, out var segment))
            return (SitemapSegment.Artist, null);

        return Guid.TryParse(payload, out var id)
            ? (segment, id)
            : (SitemapSegment.Artist, null);
    }

    private enum SitemapSegment
    {
        Artist = 0,
        Release = 1,
        ReleaseGroup = 2,
        Playlist = 3,
    }
}

public static class GetSitemapEndpoint
{
    public static IEndpointRouteBuilder MapGetSitemapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/sitemap", async (
                string? cursor,
                int? pageSize,
                GetSitemapHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cursor, pageSize, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetCatalogSitemap")
            .WithSummary("Paginated sitemap index for public catalog and playlist URLs.")
            .Produces<GetSitemapResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
