using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Shared;
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
    IObjectStorage storage)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MinQueryLength = 1;

    public async Task<Result<SearchResponse>> HandleAsync(
        string? query,
        int? pageSize,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < MinQueryLength)
            return Result<SearchResponse>.Failure(DiscoveryErrors.InvalidSearchQuery);

        var limit = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        var catalogResult = await catalog.SearchAsync(trimmed, limit, cancellationToken);

        var verified = catalogResult.Verified
            .Select(item => DiscoveryMapper.ToSearchItem(item, storage))
            .ToArray();

        var unverified = catalogResult.Unverified
            .Select(item => DiscoveryMapper.ToSearchItem(item, storage))
            .ToArray();

        var pattern = $"%{trimmed}%";
        var publicPlaylists = await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Where(p =>
                p.Kind == PlaylistKind.User
                && p.Visibility == PlaylistVisibility.Public
                && EF.Functions.ILike(p.Title, pattern))
            .OrderBy(p => p.Title)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            publicPlaylists.Select(p => p.OwnerListenerProfileId),
            presentationReadModel,
            storage,
            cancellationToken);

        var coverArtUrls = await DiscoveryPlaylistCoverArt.LoadAsync(
            publicPlaylists,
            catalog,
            storage,
            cancellationToken);

        var playlistCards = publicPlaylists
            .Select(p =>
            {
                owners.TryGetValue(p.OwnerListenerProfileId.Value, out var owner);
                coverArtUrls.TryGetValue(p.Id.Value, out var covers);
                return DiscoveryMapper.ToPublicPlaylistSearchCard(
                    p,
                    owner ?? new PlaylistOwnerDto(p.OwnerListenerProfileId.Value, null, null),
                    covers);
            })
            .ToArray();

        _ = await TryLoadPreferenceAsync(principal, cancellationToken);

        return Result<SearchResponse>.Success(new SearchResponse(
            verified,
            unverified,
            playlistCards));
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
