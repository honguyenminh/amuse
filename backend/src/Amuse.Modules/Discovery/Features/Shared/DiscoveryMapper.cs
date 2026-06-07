using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.Shared;

internal static class DiscoveryMapper
{
    public static async Task<IReadOnlyDictionary<Guid, PlaylistOwnerDto>> LoadOwnersAsync(
        IEnumerable<ListenerProfileId> ownerIds,
        IListenerProfilePresentationReadModel presentationReadModel,
        IMediaPublicUrlBuilder mediaUrls,
        CancellationToken cancellationToken)
    {
        var ids = ownerIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, PlaylistOwnerDto>();

        var rows = await presentationReadModel.GetPresentationsAsync(ids, cancellationToken);
        return rows.ToDictionary(
            kvp => kvp.Key,
            kvp => new PlaylistOwnerDto(
                kvp.Key,
                kvp.Value.DisplayName,
                AvatarUrlFor(mediaUrls, kvp.Value.AvatarObjectKey)));
    }

    public static PlaylistSummaryDto ToSummary(
        Playlist playlist,
        PlaylistOwnerDto? owner,
        ListenerProfileId? viewerProfileId,
        PlaylistEngagementState? engagement,
        IReadOnlyList<string>? coverArtUrls = null)
    {
        var playlistId = playlist.Id.Value;
        var isOwned = viewerProfileId is not null && playlist.OwnerListenerProfileId == viewerProfileId;
        return new PlaylistSummaryDto(
            playlistId,
            playlist.Title,
            DiscoveryKind.ToApiValue(playlist.Kind),
            playlist.IsLikedCollection ? null : playlist.Description,
            DiscoveryVisibility.ToApiValue(playlist.Visibility),
            playlist.Items.Count,
            playlist.UpdatedAt,
            playlist.Visibility == PlaylistVisibility.Public ? owner : null,
            playlist.ForkedFromPlaylistId?.Value,
            isOwned,
            engagement?.SavedPlaylistIds.Contains(playlistId) ?? false,
            engagement?.FollowedPlaylistIds.Contains(playlistId) ?? false,
            !playlist.IsLikedCollection,
            coverArtUrls ?? []);
    }

    public static PlaylistDetailDto ToDetail(
        Playlist playlist,
        IReadOnlyList<PlaylistItemDto> items,
        PlaylistOwnerDto? owner,
        ListenerProfileId? viewerProfileId,
        PlaylistEngagementState? engagement,
        bool includeShareEmails)
    {
        var playlistId = playlist.Id.Value;
        var isOwned = viewerProfileId is not null && playlist.OwnerListenerProfileId == viewerProfileId;
        IReadOnlyList<string>? shareEmails = includeShareEmails
            ? playlist.ShareGrants.Select(g => g.Email.Value).ToArray()
            : null;

        return new PlaylistDetailDto(
            playlistId,
            playlist.Title,
            DiscoveryKind.ToApiValue(playlist.Kind),
            playlist.IsLikedCollection ? null : playlist.Description,
            DiscoveryVisibility.ToApiValue(playlist.Visibility),
            playlist.Visibility == PlaylistVisibility.Public ? owner : null,
            playlist.ForkedFromPlaylistId?.Value,
            items,
            shareEmails,
            playlist.CreatedAt,
            playlist.UpdatedAt,
            isOwned,
            engagement?.SavedPlaylistIds.Contains(playlistId) ?? false,
            engagement?.FollowedPlaylistIds.Contains(playlistId) ?? false,
            !playlist.IsLikedCollection);
    }

    public static PlaylistItemDto ToItemDto(
        PlaylistItem item,
        CatalogTrackPlayableRow row,
        IMediaPublicUrlBuilder mediaUrls,
        string? coverArtKey)
    {
        return new PlaylistItemDto(
            item.Id.Value,
            item.TrackId.Value,
            item.Position,
            row.Title,
            row.DurationMs,
            row.HasAudio,
            CoverArtUrlFor(mediaUrls, coverArtKey),
            row.ReleaseId,
            row.ReleaseTitle,
            row.ArtistName);
    }

    public static SearchItemDto ToSearchItem(CatalogSearchItem item, IMediaPublicUrlBuilder mediaUrls) =>
        new(
            item.Kind.ToString().ToLowerInvariant(),
            item.Id,
            item.Title,
            item.Subtitle,
            item.ArtistSlug,
            item.ReleaseSlug,
            item.ArtistId,
            item.ReleaseId,
            CoverArtUrlFor(mediaUrls, item.CoverArtKey),
            item.IsVerified);

    public static PublicPlaylistSearchCardDto ToPublicPlaylistSearchCard(
        Playlist playlist,
        PlaylistOwnerDto owner,
        IReadOnlyList<string>? coverArtUrls = null) =>
        new(
            playlist.Id.Value,
            playlist.Title,
            playlist.Description,
            playlist.Items.Count,
            owner,
            playlist.UpdatedAt,
            coverArtUrls ?? []);

    public static PlayableTrackDto ToPlayableTrack(
        CatalogTrackPlayableRow row,
        IMediaPublicUrlBuilder mediaUrls,
        string? coverArtKey) =>
        new(
            row.TrackId,
            row.Title,
            row.TrackNumber,
            row.DurationMs,
            row.HasAudio,
            CoverArtUrlFor(mediaUrls, coverArtKey),
            row.ReleaseId,
            row.ReleaseTitle,
            row.ArtistName,
            row.ArtistSlug,
            row.ReleaseSlug);

    public static LikedTrackRowDto ToLikedTrack(
        LikedTrack liked,
        CatalogTrackPlayableRow row,
        IMediaPublicUrlBuilder mediaUrls,
        string? coverArtKey) =>
        new(
            row.TrackId,
            row.Title,
            row.DurationMs,
            row.HasAudio,
            CoverArtUrlFor(mediaUrls, coverArtKey),
            row.ReleaseId,
            row.ReleaseTitle,
            row.ArtistName,
            liked.LikedAt);

    public static SavedReleaseRowDto ToSavedRelease(
        LibraryEntry entry,
        CatalogReleaseSummaryRow row,
        IMediaPublicUrlBuilder mediaUrls) =>
        new(
            row.ReleaseId,
            row.Title,
            row.ArtistName,
            row.ArtistSlug,
            row.ReleaseSlug,
            CoverArtUrlFor(mediaUrls, row.CoverArtKey),
            entry.SavedAt);

    private static string? AvatarUrlFor(IMediaPublicUrlBuilder mediaUrls, string? objectKey) =>
        mediaUrls.BuildCoverArtUrl(objectKey);

    private static string? CoverArtUrlFor(IMediaPublicUrlBuilder mediaUrls, string? key) =>
        CoverArtUrlForPublic(mediaUrls, key);

    internal static string? CoverArtUrlForPublic(IMediaPublicUrlBuilder mediaUrls, string? key) =>
        mediaUrls.BuildCoverArtUrl(key);
}
