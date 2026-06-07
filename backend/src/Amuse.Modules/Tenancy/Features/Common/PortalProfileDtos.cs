namespace Amuse.Modules.Tenancy.Features.Common;

public sealed record BusinessPortalProfileResponse(
    string? DisplayName,
    int? AvatarAccentSeed,
    string? AvatarUrl,
    bool OnboardingComplete,
    DateTimeOffset UpdatedAt);

public sealed record UpdateBusinessPortalProfileRequest(
    string? DisplayName,
    int? AvatarAccentSeed,
    bool? ClearAvatar);

public sealed record PresignPortalAvatarUploadRequest(string FileName, string ContentType);

public sealed record PresignPortalAvatarUploadResponse(
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

public sealed record CompletePortalAvatarUploadRequest(string Key);

public sealed record CompletePortalAvatarUploadResponse(string Key, string AvatarUrl);
