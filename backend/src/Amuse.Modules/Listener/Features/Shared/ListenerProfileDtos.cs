using Amuse.Domain.Listener;
using Amuse.Modules.Media;

namespace Amuse.Modules.Listener.Features.Shared;

public sealed record ListenerProfileResponse(
    Guid ListenerId,
    string? DisplayName,
    int? AvatarAccentSeed,
    string? AvatarUrl,
    bool? AllowUnverifiedArtists,
    bool OnboardingComplete,
    DateTimeOffset UpdatedAt);

public sealed record PresignListenerAvatarUploadRequest(string FileName, string ContentType);

public sealed record PresignListenerAvatarUploadResponse(
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

public sealed record CompleteListenerAvatarUploadRequest(string Key);

public sealed record CompleteListenerAvatarUploadResponse(string Key, string AvatarUrl);

internal static class ListenerProfileMapper
{
    public static ListenerProfileResponse ToResponse(
        ListenerProfile profile,
        ListenerPreference? preference,
        IMediaPublicUrlBuilder? mediaUrls = null)
    {
        var updatedAt = preference?.UpdatedAt ?? profile.UpdatedAt;
        if (preference?.UpdatedAt > updatedAt)
            updatedAt = preference.UpdatedAt;

        return new ListenerProfileResponse(
            profile.Id.Value,
            profile.DisplayName,
            profile.AvatarAccentSeed,
            AvatarUrlFor(mediaUrls, profile.AvatarObjectKey),
            preference?.AllowUnverifiedArtists,
            ListenerOnboarding.IsComplete(profile, preference),
            updatedAt);
    }

    internal static string? AvatarUrlFor(IMediaPublicUrlBuilder? mediaUrls, string? objectKey) =>
        mediaUrls?.BuildCoverArtUrl(objectKey);
}
