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
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        var cursorParse = TryParseCursor(cursor);
        if (!cursorParse.IsSuccess)
            return Result<GetSitemapResponse>.Failure(cursorParse.Error!);

        var take = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var (segment, lastId) = cursorParse.Value!;

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
        string? nextCursor = hasMore
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

        var ids = await QueryGuidPageAsync(
            catalogDb.Database,
            $"""
             SELECT a.id
             FROM catalog.artist AS a
             WHERE EXISTS (
                 SELECT 1
                 FROM catalog.release AS r
                 WHERE r.artist_id = a.id
                   AND r.lifecycle_status = 'published'::catalog.release_lifecycle_status
             )
             AND ({lastId}::uuid IS NULL OR a.id > {lastId}::uuid)
             ORDER BY a.id
             LIMIT {remaining}
             """,
            cancellationToken);

        if (ids.Count == 0)
            return;

        var artistIds = ids.Select(ArtistId.From).ToList();
        var page = await catalogDb.Artists.AsNoTracking()
            .Where(a => artistIds.Contains(a.Id))
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

        foreach (var row in page)
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

        var ids = await QueryGuidPageAsync(
            catalogDb.Database,
            $"""
             SELECT r.id
             FROM catalog.release AS r
             WHERE r.lifecycle_status = 'published'::catalog.release_lifecycle_status
               AND ({lastId}::uuid IS NULL OR r.id > {lastId}::uuid)
             ORDER BY r.id
             LIMIT {remaining}
             """,
            cancellationToken);

        if (ids.Count == 0)
            return;

        var releaseIds = ids.Select(ReleaseId.From).ToList();
        var page = await catalogDb.Releases.AsNoTracking()
            .Where(r => releaseIds.Contains(r.Id))
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

        foreach (var row in page)
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

        var ids = await QueryGuidPageAsync(
            catalogDb.Database,
            $"""
             SELECT g.id
             FROM catalog.release_group AS g
             WHERE EXISTS (
                 SELECT 1
                 FROM catalog.release AS r
                 WHERE r.release_group_id = g.id
                   AND r.lifecycle_status = 'published'::catalog.release_lifecycle_status
             )
             AND ({lastId}::uuid IS NULL OR g.id > {lastId}::uuid)
             ORDER BY g.id
             LIMIT {remaining}
             """,
            cancellationToken);

        if (ids.Count == 0)
            return;

        var groupIds = ids.Select(ReleaseGroupId.From).ToList();
        var page = await catalogDb.ReleaseGroups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
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

        foreach (var row in page)
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

        var ids = await QueryGuidPageAsync(
            discoveryDb.Database,
            $"""
             SELECT p.id
             FROM discovery.playlist AS p
             WHERE p.visibility = 'public'::discovery.playlist_visibility
               AND p.kind = 'user'::discovery.playlist_kind
               AND ({lastId}::uuid IS NULL OR p.id > {lastId}::uuid)
             ORDER BY p.id
             LIMIT {remaining}
             """,
            cancellationToken);

        if (ids.Count == 0)
            return;

        var playlistIds = ids.Select(PlaylistId.From).ToList();
        var page = await discoveryDb.Playlists.AsNoTracking()
            .Where(p => playlistIds.Contains(p.Id))
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.UpdatedAt })
            .ToListAsync(cancellationToken);

        foreach (var row in page)
        {
            rows.Add(new SitemapRow(
                SitemapSegment.Playlist,
                row.Id.Value,
                new SitemapPlaylistEntry(row.Id.Value, row.UpdatedAt)));
        }
    }

    private static Task<List<Guid>> QueryGuidPageAsync(
        DatabaseFacade database,
        FormattableString sql,
        CancellationToken cancellationToken) =>
        database.SqlQuery<Guid>(sql).ToListAsync(cancellationToken);

    private static string EncodeCursor(SitemapRow row) => $"{row.Segment}:{row.SortId}";

    private static Result<(SitemapSegment Segment, Guid? LastId)> TryParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return Result<(SitemapSegment, Guid?)>.Success((SitemapSegment.Artist, null));

        var separatorIndex = cursor.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= cursor.Length - 1)
            return Result<(SitemapSegment, Guid?)>.Failure(CatalogErrors.SitemapInvalidCursor);

        var segmentRaw = cursor[..separatorIndex];
        var payload = cursor[(separatorIndex + 1)..];

        if (!Enum.TryParse(segmentRaw, ignoreCase: true, out SitemapSegment segment))
            return Result<(SitemapSegment, Guid?)>.Failure(CatalogErrors.SitemapInvalidCursor);

        if (!Guid.TryParse(payload, out var id))
            return Result<(SitemapSegment, Guid?)>.Failure(CatalogErrors.SitemapInvalidCursor);

        return Result<(SitemapSegment, Guid?)>.Success((segment, id));
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
