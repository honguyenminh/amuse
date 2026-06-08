using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Search;

internal sealed class SearchHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPreferenceReadModel preferenceReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IMediaPublicUrlBuilder mediaUrls)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MinQueryLength = 1;
    private const int MaxSearchLimit = 50;

    public async Task<Result<SearchResponse>> HandleAsync(
        string? query,
        int? pageSize,
        string[]? kinds,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < MinQueryLength)
            return Result<SearchResponse>.Failure(DiscoveryErrors.InvalidSearchQuery);

        var limit = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var kindFilter = DiscoverySearchKinds.Parse(kinds);
        var candidateLimit = Math.Min(Math.Max(limit * 3, limit), MaxSearchLimit);
        var allowUnverifiedArtists = await TryLoadPreferenceAsync(principal, cancellationToken);

        IReadOnlyList<CatalogSearchItem> catalogItems = [];
        if (DiscoverySearchKinds.IncludesCatalog(kindFilter))
        {
            var catalogResult = await catalog.SearchAsync(
                trimmed,
                candidateLimit,
                kindFilter,
                cancellationToken);
            catalogItems = catalogResult.Items;
        }

        IReadOnlyList<Playlist> publicPlaylists = [];
        if (DiscoverySearchKinds.IncludesPlaylist(kindFilter))
        {
            var pattern = $"%{trimmed}%";
            publicPlaylists = await db.Playlists.AsNoTracking()
                .Include(p => p.Items)
                .Where(p =>
                    p.Kind == PlaylistKind.User
                    && p.Visibility == PlaylistVisibility.Public
                    && EF.Functions.ILike(p.Title, pattern))
                .OrderBy(p => p.Title)
                .Take(candidateLimit)
                .ToListAsync(cancellationToken);
        }

        var ranked = DiscoverySearchRanker.RankMixed(
            trimmed,
            catalogItems,
            publicPlaylists,
            allowUnverifiedArtists,
            kindFilter,
            limit);

        var playlistPayloads = ranked.OfType<Playlist>().ToArray();
        var owners = await DiscoveryMapper.LoadOwnersAsync(
            playlistPayloads.Select(p => p.OwnerListenerProfileId),
            presentationReadModel,
            mediaUrls,
            cancellationToken);

        var coverArtUrls = await DiscoveryPlaylistCoverArt.LoadAsync(
            playlistPayloads,
            catalog,
            mediaUrls,
            cancellationToken);

        var items = ranked
            .Select(payload => payload switch
            {
                CatalogSearchItem catalogItem => DiscoveryMapper.ToSearchResultItem(catalogItem, mediaUrls),
                Playlist playlist =>
                    DiscoveryMapper.ToSearchResultItem(
                        playlist,
                        owners.TryGetValue(playlist.OwnerListenerProfileId.Value, out var owner)
                            ? owner
                            : new PlaylistOwnerDto(playlist.OwnerListenerProfileId.Value, null, null),
                        coverArtUrls.TryGetValue(playlist.Id.Value, out var covers) ? covers : []),
                _ => throw new InvalidOperationException("Unexpected search payload type."),
            })
            .ToArray();

        return Result<SearchResponse>.Success(new SearchResponse(items));
    }

    private async Task<bool?> TryLoadPreferenceAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = DiscoveryPrincipal.ResolveAccountId(principal);
        if (accountId is null)
            return null;

        return await preferenceReadModel.GetAllowUnverifiedArtistsAsync(accountId.Value, cancellationToken);
    }
}
