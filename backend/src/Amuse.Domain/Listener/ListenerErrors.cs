using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Listener;

public static class ListenerErrors
{
    public static readonly DomainError InvalidDisplayName =
        new("listener.invalid_display_name", "Listener display name is invalid.");

    public static readonly DomainError InvalidAvatarAccentSeed =
        new("listener.invalid_avatar_accent_seed", "Avatar accent seed is out of range.");

    public static readonly DomainError ProfileNotFound =
        new("listener.profile_not_found", "Listener profile was not found.");

    public static readonly DomainError OnboardingIncomplete =
        new("listener.onboarding_incomplete", "Listener onboarding is not complete.");

    public static readonly DomainError InvalidAvatarUploadRequest =
        new("listener.invalid_avatar_upload_request", "Avatar upload request is invalid.");

    public static readonly DomainError InvalidAvatarObjectKey =
        new("listener.invalid_avatar_object_key", "Avatar object key is invalid.");

    public static readonly DomainError AvatarObjectMissing =
        new("listener.avatar_object_missing", "Avatar image was not found in storage.");
}
