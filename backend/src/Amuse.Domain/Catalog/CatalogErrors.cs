using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public static class CatalogErrors
{
    public static readonly DomainError ArtistNotFound =
        new("catalog.artist_not_found", "Artist was not found.");

    public static readonly DomainError ReleaseNotFound =
        new("catalog.release_not_found", "Release was not found.");

    public static readonly DomainError ReleaseGroupNotFound =
        new("catalog.release_group_not_found", "Release group was not found.");

    public static readonly DomainError TrackNotFound =
        new("catalog.track_not_found", "Track was not found.");

    public static readonly DomainError TrackHasNoAudio =
        new("catalog.track_has_no_audio", "Track does not have an audio master assigned yet.");

    public static readonly DomainError InvalidAudioUploadRequest =
        new("catalog.invalid_audio_upload_request", "Audio upload request is invalid.");

    public static readonly DomainError AudioMasterObjectMissing =
        new("catalog.audio_master_object_missing", "Uploaded audio master was not found in object storage.");

    public static readonly DomainError AudioDurationUnreadable =
        new("catalog.audio_duration_unreadable", "Could not determine audio duration from the uploaded master.");

    public static readonly DomainError AudioDurationOutOfRange =
        new("catalog.audio_duration_out_of_range", "Audio duration is outside the allowed range.");

    public static readonly DomainError TranscodeAlreadyInProgress =
        new("catalog.transcode_already_in_progress", "A transcode job is already in progress for this track.");

    public static readonly DomainError TrackStreamNotReady =
        new("catalog.track_stream_not_ready", "Track stream is not ready yet.");

    public static readonly DomainError TrackStreamAlreadyReady =
        new("catalog.track_stream_already_ready", "Track stream is already available; no retry is required.");

    public static readonly DomainError TranscodeRetryNotAllowed =
        new("catalog.transcode_retry_not_allowed", "Manual transcode retry is only allowed after a failed job.");

    public static readonly DomainError NoTranscodeJobToRetry =
        new("catalog.no_transcode_job_to_retry", "No transcode job is available to retry for this track.");

    public static readonly DomainError StreamAssetNotFound =
        new("catalog.stream_asset_not_found", "Requested stream asset was not found.");

    public static readonly DomainError InvalidSlug =
        new("catalog.invalid_slug", "Slug must be lowercase alphanumerics with single hyphens.");

    public static readonly DomainError InvalidArtist =
        new("catalog.invalid_artist", "Artist data is invalid.");

    public static readonly DomainError InvalidRelease =
        new("catalog.invalid_release", "Release data is invalid.");

    public static readonly DomainError InvalidReleaseGroup =
        new("catalog.invalid_release_group", "Release group data is invalid.");

    public static readonly DomainError ReleaseGroupArtistMismatch =
        new("catalog.release_group_artist_mismatch", "Release group does not belong to this artist.");

    public static readonly DomainError InvalidTrack =
        new("catalog.invalid_track", "Track data is invalid.");

    public static readonly DomainError InvalidLifecycleTransition =
        new("catalog.invalid_lifecycle_transition", "Catalog lifecycle transition is not allowed.");

    public static readonly DomainError NotOrganizationCatalog =
        new("catalog.not_organization_catalog", "Catalog resource does not belong to the active organization.");

    public static readonly DomainError Forbidden =
        new("catalog.forbidden", "You do not have permission to perform this catalog action.");

    public static readonly DomainError DuplicateTrackNumber =
        new("catalog.duplicate_track_number", "Track number already exists on this release.");

    public static readonly DomainError ReleaseHasNoTracks =
        new("catalog.release_has_no_tracks", "Release must have at least one track before publishing.");

    public static readonly DomainError TracksNotReady =
        new("catalog.tracks_not_ready", "All tracks must be ready before publishing.");

    public static readonly DomainError ReleaseNotReadyToSchedule =
        new("catalog.release_not_ready_to_schedule", "All tracks must be ready before scheduling a release.");

    public static readonly DomainError ReleaseDateNotInFuture =
        new("catalog.release_date_not_in_future", "Release date must be in the future to schedule automatic publishing.");

    public static readonly DomainError ArtistAlreadyManaged =
        new("catalog.artist_already_managed", "Artist is already managed by another organization.");

    public static readonly DomainError DuplicateSlug =
        new("catalog.duplicate_slug", "Slug is already in use.");

    public static readonly DomainError InvalidCoverUploadRequest =
        new("catalog.invalid_cover_upload_request", "Cover upload request is invalid.");

    public static readonly DomainError CoverObjectMissing =
        new("catalog.cover_object_missing", "Uploaded cover image was not found in object storage.");

    public static readonly DomainError InvalidArtistAvatarUploadRequest =
        new("catalog.invalid_artist_avatar_upload_request", "Artist avatar upload request is invalid.");

    public static readonly DomainError ArtistAvatarObjectMissing =
        new("catalog.artist_avatar_object_missing", "Uploaded artist avatar image was not found in object storage.");

    public static readonly DomainError InvalidArtistCoverUploadRequest =
        new("catalog.invalid_artist_cover_upload_request", "Artist cover upload request is invalid.");

    public static readonly DomainError ArtistCoverObjectMissing =
        new("catalog.artist_cover_object_missing", "Uploaded artist cover image was not found in object storage.");

    public static readonly DomainError InvalidCollaborator =
        new("catalog.invalid_collaborator", "Collaborating artist is invalid for this release.");

    public static readonly DomainError ReleaseNotDeletable =
        new("catalog.release_not_deletable", "Only unpublished releases can be deleted.");

    public static readonly DomainError TrackNotDeletable =
        new("catalog.track_not_deletable", "Tracks can only be deleted from unpublished releases.");

    public static readonly DomainError InvalidFormattedText =
        new("catalog.invalid_formatted_text", "Text contains unsupported formatting or invalid links.");
}
