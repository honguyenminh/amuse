using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Catalog;

public sealed class Artist
{
    public const int MaxNameLength = 200;
    public const int MaxBioLength = CatalogFormattedText.MaxLength;
    public const int MaxKeyLength = 512;
    public const int MaxCountryCodeLength = 2;
    public const int MaxUrlLength = 500;
    public const int MaxAliasesLength = 1000;

    public ArtistId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public string? Bio { get; private set; }
    public string? CountryCode { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? Aliases { get; private set; }
    public string? AvatarKey { get; private set; }
    public string? CoverKey { get; private set; }
    public OrganizationId? ManagingOrganizationId { get; private set; }
    public ArtistVisibilityTier VisibilityTier { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Artist()
    {
    }

    private Artist(
        ArtistId id,
        string name,
        Slug slug,
        string? bio,
        string? countryCode,
        string? websiteUrl,
        string? aliases,
        string? avatarKey,
        string? coverKey,
        OrganizationId? managingOrganizationId,
        ArtistVisibilityTier visibilityTier,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Bio = bio;
        CountryCode = countryCode;
        WebsiteUrl = websiteUrl;
        Aliases = aliases;
        AvatarKey = avatarKey;
        CoverKey = coverKey;
        ManagingOrganizationId = managingOrganizationId;
        VisibilityTier = visibilityTier;
        CreatedAt = createdAt;
    }

    public static Result<Artist> Create(
        ArtistId id,
        string name,
        Slug slug,
        DateTimeOffset createdAt,
        OrganizationId? managingOrganizationId = null,
        ArtistVisibilityTier visibilityTier = ArtistVisibilityTier.Unverified,
        string? bio = null,
        string? countryCode = null,
        string? websiteUrl = null,
        string? aliases = null,
        string? avatarKey = null,
        string? coverKey = null)
    {
        var trimmedName = (name ?? throw new ArgumentNullException(nameof(name))).Trim();
        if (trimmedName.Length is 0 or > MaxNameLength)
            return Result<Artist>.Failure(CatalogErrors.InvalidArtist);

        var bioResult = CatalogFormattedText.TryCreate(bio);
        if (!bioResult.IsSuccess)
            return Result<Artist>.Failure(bioResult.Error!);

        var normalizedCountryCode = NormalizeCountryCode(countryCode);
        if (countryCode is not null && normalizedCountryCode is null)
            return Result<Artist>.Failure(CatalogErrors.InvalidArtist);

        var normalizedWebsiteUrl = NormalizeUrl(websiteUrl);
        if (websiteUrl is not null && normalizedWebsiteUrl is null)
            return Result<Artist>.Failure(CatalogErrors.InvalidArtist);

        var normalizedAliases = NormalizeOptionalText(aliases, MaxAliasesLength);
        if (aliases is not null && normalizedAliases is null)
            return Result<Artist>.Failure(CatalogErrors.InvalidArtist);

        var keyResult = ValidateKey(avatarKey, nameof(avatarKey));
        if (!keyResult.IsSuccess)
            return Result<Artist>.Failure(keyResult.Error!);

        keyResult = ValidateKey(coverKey, nameof(coverKey));
        if (!keyResult.IsSuccess)
            return Result<Artist>.Failure(keyResult.Error!);

        return Result<Artist>.Success(
            new Artist(
                id,
                trimmedName,
                slug,
                bio: CatalogFormattedText.ToStoredValue(bioResult.Value),
                normalizedCountryCode,
                normalizedWebsiteUrl,
                normalizedAliases,
                avatarKey,
                coverKey,
                managingOrganizationId,
                visibilityTier,
                createdAt));
    }

    public bool IsManagedBy(OrganizationId organizationId) =>
        ManagingOrganizationId.HasValue && ManagingOrganizationId.Value == organizationId;

    public Result UpdateProfile(
        string name,
        string? bio,
        string? countryCode,
        string? websiteUrl,
        string? aliases)
    {
        var trimmedName = (name ?? throw new ArgumentNullException(nameof(name))).Trim();
        if (trimmedName.Length is 0 or > MaxNameLength)
            return Result.Failure(CatalogErrors.InvalidArtist);

        var bioResult = CatalogFormattedText.TryCreate(bio);
        if (!bioResult.IsSuccess)
            return Result.Failure(bioResult.Error!);

        var normalizedCountryCode = NormalizeCountryCode(countryCode);
        if (countryCode is not null && normalizedCountryCode is null)
            return Result.Failure(CatalogErrors.InvalidArtist);

        var normalizedWebsiteUrl = NormalizeUrl(websiteUrl);
        if (websiteUrl is not null && normalizedWebsiteUrl is null)
            return Result.Failure(CatalogErrors.InvalidArtist);

        var normalizedAliases = NormalizeOptionalText(aliases, MaxAliasesLength);
        if (aliases is not null && normalizedAliases is null)
            return Result.Failure(CatalogErrors.InvalidArtist);

        Name = trimmedName;
        Bio = CatalogFormattedText.ToStoredValue(bioResult.Value);
        CountryCode = normalizedCountryCode;
        WebsiteUrl = normalizedWebsiteUrl;
        Aliases = normalizedAliases;
        return Result.Success();
    }

    public void SetVisibilityTier(ArtistVisibilityTier visibilityTier) =>
        VisibilityTier = visibilityTier;

    private static Result ValidateKey(string? key, string paramName)
    {
        if (key is null)
            return Result.Success();

        if (key.Length is 0 or > MaxKeyLength)
            return Result.Failure(CatalogErrors.InvalidArtist);

        return Result.Success();
    }

    private static string? NormalizeCountryCode(string? countryCode)
    {
        var normalized = NormalizeOptionalText(countryCode, MaxCountryCodeLength);
        return normalized?.ToUpperInvariant();
    }

    private static string? NormalizeUrl(string? url)
    {
        var normalized = NormalizeOptionalText(url, MaxUrlLength);
        if (normalized is null)
            return null;

        return Uri.TryCreate(normalized, UriKind.Absolute, out var parsed)
               && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            ? normalized
            : null;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (value is null)
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
            return null;

        return trimmed;
    }
}
