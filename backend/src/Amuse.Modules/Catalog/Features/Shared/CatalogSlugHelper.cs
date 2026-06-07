using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static partial class CatalogSlugHelper
{
    private static readonly Regex MultiHyphenRegex = new("-{2,}", RegexOptions.Compiled);

    public static Result<Slug> TryFromTitle(string title)
    {
        var slugValue = SlugifyFromTitle(title);
        if (string.IsNullOrEmpty(slugValue) || !Slug.IsValid(slugValue))
            return Result<Slug>.Failure(CatalogErrors.InvalidSlug);

        return Result<Slug>.Success(Slug.From(slugValue));
    }

    public static Result<Slug> TryParseArtistSlug(string rawSlug) => TryParseCatalogSlug(rawSlug);

    public static Result<Slug> TryParseReleaseSlug(string rawSlug) => TryParseCatalogSlug(rawSlug);

    private static Result<Slug> TryParseCatalogSlug(string rawSlug)
    {
        var normalized = NormalizeSlugInput(rawSlug);
        if (string.IsNullOrEmpty(normalized) || !Slug.IsValid(normalized))
            return Result<Slug>.Failure(CatalogErrors.InvalidSlug);

        return Result<Slug>.Success(Slug.From(normalized));
    }

    public static string SuggestArtistSlugFromName(string name) => SlugifyFromTitle(name);

    public static async Task<bool> IsArtistSlugAvailableAsync(
        CatalogDbContext db,
        string rawSlug,
        ArtistId? excludingArtistId,
        CancellationToken cancellationToken)
    {
        var parseResult = TryParseArtistSlug(rawSlug);
        if (!parseResult.IsSuccess)
            return false;

        var typedSlug = parseResult.Value!;
        var query = db.Artists.IgnoreQueryFilters().AsNoTracking().Where(a => a.Slug == typedSlug);
        if (excludingArtistId is { } artistId)
            query = query.Where(a => a.Id != artistId);

        return !await query.AnyAsync(cancellationToken);
    }

    public static async Task<Result<Slug>> EnsureAvailableArtistSlugAsync(
        CatalogDbContext db,
        string rawSlug,
        CancellationToken cancellationToken)
    {
        var parseResult = TryParseArtistSlug(rawSlug);
        if (!parseResult.IsSuccess)
            return parseResult;

        var typedSlug = parseResult.Value!;
        var taken = await db.Artists
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(a => a.Slug == typedSlug, cancellationToken);

        return taken
            ? Result<Slug>.Failure(CatalogErrors.DuplicateSlug)
            : Result<Slug>.Success(typedSlug);
    }

    public static async Task<Result<Slug>> AllocateUniqueArtistSlugAsync(
        CatalogDbContext db,
        string title,
        CancellationToken cancellationToken)
    {
        var baseResult = TryFromTitle(title);
        if (!baseResult.IsSuccess)
            return baseResult;

        return await AllocateArtistSlugCoreAsync(db, baseResult.Value!.Value, cancellationToken);
    }

    public static async Task<bool> IsReleaseSlugAvailableAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string rawSlug,
        ReleaseId? excludingReleaseId,
        CancellationToken cancellationToken)
    {
        var parseResult = TryParseReleaseSlug(rawSlug);
        if (!parseResult.IsSuccess)
            return false;

        var typedSlug = parseResult.Value!;
        var query = db.Releases.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.ArtistId == artistId && r.Slug == typedSlug);
        if (excludingReleaseId is { } releaseId)
            query = query.Where(r => r.Id != releaseId);

        return !await query.AnyAsync(cancellationToken);
    }

    public static async Task<Result<Slug>> EnsureAvailableReleaseSlugAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string rawSlug,
        ReleaseId? excludingReleaseId,
        CancellationToken cancellationToken)
    {
        var parseResult = TryParseReleaseSlug(rawSlug);
        if (!parseResult.IsSuccess)
            return parseResult;

        var typedSlug = parseResult.Value!;
        var query = db.Releases.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.ArtistId == artistId && r.Slug == typedSlug);
        if (excludingReleaseId is { } releaseId)
            query = query.Where(r => r.Id != releaseId);

        var taken = await query.AnyAsync(cancellationToken);
        return taken
            ? Result<Slug>.Failure(CatalogErrors.DuplicateSlug)
            : Result<Slug>.Success(typedSlug);
    }

    public static async Task<Result<Slug>> ResolveReleaseSlugForCreateAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string title,
        string? requestedSlug,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedSlug))
            return await EnsureAvailableReleaseSlugAsync(
                db,
                artistId,
                requestedSlug,
                excludingReleaseId: null,
                cancellationToken);

        return await AllocateUniqueReleaseSlugAsync(db, artistId, title, cancellationToken);
    }

    public static async Task<Result<Slug>> AllocateUniqueReleaseSlugAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string title,
        CancellationToken cancellationToken)
    {
        var baseResult = TryFromTitle(title);
        if (!baseResult.IsSuccess)
            return baseResult;

        return await AllocateReleaseSlugCoreAsync(db, artistId, baseResult.Value!.Value, cancellationToken);
    }

    public static async Task<Result<Slug>> AllocateUniqueReleaseGroupSlugAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string title,
        string? requestedReleaseSlug,
        CancellationToken cancellationToken)
    {
        var baseResult = ResolveReleaseGroupSlugBase(title, requestedReleaseSlug);
        if (!baseResult.IsSuccess)
            return baseResult;

        return await AllocateReleaseGroupSlugCoreAsync(
            db,
            artistId,
            baseResult.Value!.Value,
            cancellationToken);
    }

    /// <summary>
    /// When the client supplies a release slug, use it for the auto-provisioned release group.
    /// Otherwise derive the group slug from the release title.
    /// </summary>
    internal static Result<Slug> ResolveReleaseGroupSlugBase(
        string title,
        string? requestedReleaseSlug)
    {
        if (!string.IsNullOrWhiteSpace(requestedReleaseSlug))
            return TryParseReleaseSlug(requestedReleaseSlug);

        return TryFromTitle(title);
    }

    internal static string NormalizeSlugInput(string rawSlug)
    {
        if (string.IsNullOrWhiteSpace(rawSlug))
            return string.Empty;

        var normalized = StripDiacritics(rawSlug.Trim().ToLowerInvariant());
        normalized = normalized.Replace('_', '-');
        normalized = AmpersandRegex().Replace(normalized, " and ");

        var sb = new StringBuilder(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
                lastDash = false;
                continue;
            }

            if (ch is '-' or ' ' or '/' or '.' or ',')
            {
                if (!lastDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }
        }

        if (lastDash && sb.Length > 0)
            sb.Length--;

        var result = MultiHyphenRegex.Replace(sb.ToString(), "-").Trim('-');
        if (result.Length > Slug.MaxLength)
            result = result[..Slug.MaxLength].TrimEnd('-');

        return result;
    }

    private static string SlugifyFromTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = StripDiacritics(value.Trim().ToLowerInvariant());
        normalized = AmpersandRegex().Replace(normalized, " and ");
        normalized = ApostropheRegex().Replace(normalized, string.Empty);

        var sb = new StringBuilder(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
                lastDash = false;
                continue;
            }

            if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        if (lastDash && sb.Length > 0)
            sb.Length--;

        var result = MultiHyphenRegex.Replace(sb.ToString(), "-").Trim('-');
        if (result.Length > Slug.MaxLength)
            result = result[..Slug.MaxLength].TrimEnd('-');

        return result;
    }

    private static string StripDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static async Task<Result<Slug>> AllocateArtistSlugCoreAsync(
        CatalogDbContext db,
        string baseSlug,
        CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await db.Artists.IgnoreQueryFilters().AsNoTracking().AnyAsync(a => a.Slug == Slug.From(candidate), cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
            if (candidate.Length > Slug.MaxLength)
                return Result<Slug>.Failure(CatalogErrors.DuplicateSlug);
        }

        if (!Slug.IsValid(candidate))
            return Result<Slug>.Failure(CatalogErrors.InvalidSlug);

        return Result<Slug>.Success(Slug.From(candidate));
    }

    private static async Task<Result<Slug>> AllocateReleaseSlugCoreAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string baseSlug,
        CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await db.Releases.IgnoreQueryFilters().AsNoTracking().AnyAsync(
                   r => r.ArtistId == artistId && r.Slug == Slug.From(candidate),
                   cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
            if (candidate.Length > Slug.MaxLength)
                return Result<Slug>.Failure(CatalogErrors.DuplicateSlug);
        }

        if (!Slug.IsValid(candidate))
            return Result<Slug>.Failure(CatalogErrors.InvalidSlug);

        return Result<Slug>.Success(Slug.From(candidate));
    }

    private static async Task<Result<Slug>> AllocateReleaseGroupSlugCoreAsync(
        CatalogDbContext db,
        ArtistId artistId,
        string baseSlug,
        CancellationToken cancellationToken)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await db.ReleaseGroups.IgnoreQueryFilters().AsNoTracking().AnyAsync(
                   g => g.ArtistId == artistId && g.Slug == Slug.From(candidate),
                   cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
            if (candidate.Length > Slug.MaxLength)
                return Result<Slug>.Failure(CatalogErrors.DuplicateSlug);
        }

        if (!Slug.IsValid(candidate))
            return Result<Slug>.Failure(CatalogErrors.InvalidSlug);

        return Result<Slug>.Success(Slug.From(candidate));
    }

    [GeneratedRegex(@"&+")]
    private static partial Regex AmpersandRegex();

    [GeneratedRegex(@"[''`´]+")]
    private static partial Regex ApostropheRegex();
}
