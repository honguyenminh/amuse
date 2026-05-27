using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public static class CatalogErrors
{
    public static readonly DomainError ArtistNotFound =
        new("catalog.artist_not_found", "Artist was not found.");

    public static readonly DomainError ReleaseNotFound =
        new("catalog.release_not_found", "Release was not found.");

    public static readonly DomainError TrackNotFound =
        new("catalog.track_not_found", "Track was not found.");

    public static readonly DomainError TrackHasNoAudio =
        new("catalog.track_has_no_audio", "Track does not have an audio master assigned yet.");

    public static readonly DomainError InvalidSlug =
        new("catalog.invalid_slug", "Slug must be lowercase alphanumerics with single hyphens.");

    public static readonly DomainError InvalidArtist =
        new("catalog.invalid_artist", "Artist data is invalid.");

    public static readonly DomainError InvalidRelease =
        new("catalog.invalid_release", "Release data is invalid.");

    public static readonly DomainError InvalidTrack =
        new("catalog.invalid_track", "Track data is invalid.");
}
