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

    public static readonly DomainError InvalidAudioUploadRequest =
        new("catalog.invalid_audio_upload_request", "Audio upload request is invalid.");

    public static readonly DomainError AudioMasterObjectMissing =
        new("catalog.audio_master_object_missing", "Uploaded audio master was not found in object storage.");

    public static readonly DomainError TrackStreamNotReady =
        new("catalog.track_stream_not_ready", "Track stream is not ready yet.");

    public static readonly DomainError StreamAssetNotFound =
        new("catalog.stream_asset_not_found", "Requested stream asset was not found.");

    public static readonly DomainError InvalidSlug =
        new("catalog.invalid_slug", "Slug must be lowercase alphanumerics with single hyphens.");

    public static readonly DomainError InvalidArtist =
        new("catalog.invalid_artist", "Artist data is invalid.");

    public static readonly DomainError InvalidRelease =
        new("catalog.invalid_release", "Release data is invalid.");

    public static readonly DomainError InvalidTrack =
        new("catalog.invalid_track", "Track data is invalid.");
}
