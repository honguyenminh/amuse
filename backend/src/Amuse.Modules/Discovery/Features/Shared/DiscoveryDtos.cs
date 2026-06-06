namespace Amuse.Modules.Discovery.Features.Shared;

public sealed record CreatePlaylistRequest(string Title, string Visibility, string? Description);

public sealed record UpdatePlaylistRequest(string? Title, string? Description, string? Visibility);

public sealed record AddPlaylistItemRequest(Guid TrackId);

public sealed record ReorderPlaylistItemsRequest(Guid ItemId, int NewPosition);

public sealed record ReplacePlaylistSharesRequest(IReadOnlyList<string> Emails);

public sealed record PlaylistOwnerDto(
    Guid ListenerProfileId,
    string? DisplayName,
    string? AvatarUrl);

public sealed record PlaylistSummaryDto(
    Guid Id,
    string Title,
    string Kind,
    string Visibility,
    int TrackCount,
    DateTimeOffset UpdatedAt,
    PlaylistOwnerDto? Owner,
    Guid? ForkedFromPlaylistId,
    bool IsOwned,
    bool IsSaved,
    bool IsFollowed,
    bool IsDeletable,
    IReadOnlyList<string> CoverArtUrls);

public sealed record PlaylistItemDto(
    Guid ItemId,
    Guid TrackId,
    int Position,
    string Title,
    int DurationMs,
    bool HasAudio,
    string? CoverArtUrl,
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName);

public sealed record PlaylistDetailDto(
    Guid Id,
    string Title,
    string Kind,
    string? Description,
    string Visibility,
    PlaylistOwnerDto? Owner,
    Guid? ForkedFromPlaylistId,
    IReadOnlyList<PlaylistItemDto> Items,
    IReadOnlyList<string>? ShareEmails,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsOwned,
    bool IsSaved,
    bool IsFollowed,
    bool IsDeletable);

public sealed record PlaylistListResponse(IReadOnlyList<PlaylistSummaryDto> Playlists);

public sealed record SearchItemDto(
    string Kind,
    Guid Id,
    string Title,
    string? Subtitle,
    string? ArtistSlug,
    string? ReleaseSlug,
    Guid? ArtistId,
    Guid? ReleaseId,
    string? CoverArtUrl,
    bool IsVerified);

public sealed record PublicPlaylistSearchCardDto(
    Guid Id,
    string Title,
    int TrackCount,
    PlaylistOwnerDto Owner,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> CoverArtUrls);

public sealed record SearchResponse(
    IReadOnlyList<SearchItemDto> Verified,
    IReadOnlyList<SearchItemDto> Unverified,
    IReadOnlyList<PublicPlaylistSearchCardDto> PublicPlaylists);

public sealed record LikedTrackRowDto(
    Guid TrackId,
    string Title,
    int DurationMs,
    bool HasAudio,
    string? CoverArtUrl,
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName,
    DateTimeOffset LikedAt);

public sealed record LikedTracksResponse(IReadOnlyList<LikedTrackRowDto> Tracks);

public sealed record SavedReleaseRowDto(
    Guid ReleaseId,
    string Title,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug,
    string? CoverArtUrl,
    DateTimeOffset SavedAt);

public sealed record SavedReleasesResponse(IReadOnlyList<SavedReleaseRowDto> Releases);

public sealed record PlayableTrackDto(
    Guid TrackId,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool HasAudio,
    string? CoverArtUrl,
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug);

public sealed record PlayableTracksResponse(IReadOnlyList<PlayableTrackDto> Tracks);

public sealed record AddPlaylistItemResponse(PlaylistItemDto Item);
