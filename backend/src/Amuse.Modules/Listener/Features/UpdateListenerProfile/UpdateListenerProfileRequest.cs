namespace Amuse.Modules.Listener.Features.UpdateListenerProfile;

public sealed record UpdateListenerProfileRequest(
    string? DisplayName,
    int? AvatarAccentSeed,
    bool? AllowUnverifiedArtists,
    bool? ClearAvatar);
