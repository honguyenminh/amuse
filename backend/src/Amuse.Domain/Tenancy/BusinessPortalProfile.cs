using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public sealed class BusinessPortalProfile
{
    public const int MaxDisplayNameLength = 80;
    public const int MinAvatarAccentSeed = 0;
    public const int MaxAvatarAccentSeed = 11;
    public const int MaxAvatarObjectKeyLength = 500;

    public AccountId AccountId { get; private set; }
    public string? DisplayName { get; private set; }
    public int? AvatarAccentSeed { get; private set; }
    public string? AvatarObjectKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private BusinessPortalProfile()
    {
    }

    private BusinessPortalProfile(
        AccountId accountId,
        DateTimeOffset createdAt)
    {
        AccountId = accountId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public bool IsComplete => !string.IsNullOrEmpty(DisplayName);

    public static BusinessPortalProfile Create(AccountId accountId, DateTimeOffset now) =>
        new(accountId, now);

    public Result UpdatePresentation(string displayName, int? avatarAccentSeed, DateTimeOffset now)
    {
        var normalizedName = NormalizeDisplayName(displayName);
        if (normalizedName is null)
            return Result.Failure(TenancyErrors.InvalidPortalProfileDisplayName);

        if (avatarAccentSeed is not null
            && (avatarAccentSeed < MinAvatarAccentSeed || avatarAccentSeed > MaxAvatarAccentSeed))
        {
            return Result.Failure(TenancyErrors.InvalidPortalProfileAvatarAccentSeed);
        }

        DisplayName = normalizedName;
        if (avatarAccentSeed is not null)
            AvatarAccentSeed = avatarAccentSeed;

        UpdatedAt = now;
        return Result.Success();
    }

    public Result SetAvatarObjectKey(string? objectKey, DateTimeOffset now)
    {
        if (objectKey is null)
        {
            AvatarObjectKey = null;
            UpdatedAt = now;
            return Result.Success();
        }

        var trimmed = objectKey.Trim();
        if (trimmed.Length is 0 or > MaxAvatarObjectKeyLength)
            return Result.Failure(TenancyErrors.InvalidPortalAvatarObjectKey);

        AvatarObjectKey = trimmed;
        UpdatedAt = now;
        return Result.Success();
    }

    private static string? NormalizeDisplayName(string displayName)
    {
        var trimmed = (displayName ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxDisplayNameLength)
            return null;

        return trimmed;
    }
}
