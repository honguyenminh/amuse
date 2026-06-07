using Amuse.Domain.Catalog;
using FluentValidation;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogManageValidators
{
    internal static IRuleBuilderOptions<T, DateTimeOffset> MustBeUtc<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder) =>
        ruleBuilder
            .Must(d => d.Offset == TimeSpan.Zero)
            .WithMessage("Timestamp must be UTC (offset Z).");

    internal static IRuleBuilderOptions<T, string?> MustBeValidFormattedCatalogText<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder
            .Must(text => CatalogFormattedText.TryCreate(text).IsSuccess)
            .WithMessage(CatalogErrors.InvalidFormattedText.Message);
}

internal sealed class CreateReleaseGroupRequestValidator : AbstractValidator<CreateReleaseGroupRequest>
{
    public CreateReleaseGroupRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(ReleaseGroup.MaxTitleLength);
        RuleFor(x => x.Description).MustBeValidFormattedCatalogText();
    }
}

internal sealed class UpdateReleaseGroupRequestValidator : AbstractValidator<UpdateReleaseGroupRequest>
{
    public UpdateReleaseGroupRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(ReleaseGroup.MaxTitleLength);
        RuleFor(x => x.Description).MustBeValidFormattedCatalogText();
    }
}

internal sealed class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Artist.MaxNameLength);
        RuleFor(x => x.Bio).MustBeValidFormattedCatalogText();
        RuleFor(x => x.CountryCode).MaximumLength(Artist.MaxCountryCodeLength);
        RuleFor(x => x.WebsiteUrl).MaximumLength(Artist.MaxUrlLength);
        RuleFor(x => x.Aliases).MaximumLength(Artist.MaxAliasesLength);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Slug.MaxLength)
            .Must(slug => CatalogSlugHelper.TryParseArtistSlug(slug).IsSuccess)
            .WithMessage("Slug must be lowercase letters, numbers, and single hyphens (e.g. my-artist-name).");
    }
}

internal sealed class UpdateArtistRequestValidator : AbstractValidator<UpdateArtistRequest>
{
    public UpdateArtistRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Artist.MaxNameLength);
        RuleFor(x => x.Bio).MustBeValidFormattedCatalogText();
        RuleFor(x => x.CountryCode).MaximumLength(Artist.MaxCountryCodeLength);
        RuleFor(x => x.WebsiteUrl).MaximumLength(Artist.MaxUrlLength);
        RuleFor(x => x.Aliases).MaximumLength(Artist.MaxAliasesLength);
    }
}

internal sealed class CreateReleaseRequestValidator : AbstractValidator<CreateReleaseRequest>
{
    public CreateReleaseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Release.MaxTitleLength);
        RuleFor(x => x.ReleaseType).IsInEnum();
        RuleFor(x => x.ReleaseDate).MustBeUtc();
        RuleFor(x => x.Description).MustBeValidFormattedCatalogText();
        RuleFor(x => x.Upc).MaximumLength(Release.MaxUpcLength);
        RuleFor(x => x.PrimaryGenre).MaximumLength(Release.MaxGenreLength);
        RuleFor(x => x.Tags).MaximumLength(Release.MaxTagsLength);
        RuleFor(x => x.LanguageCode).MaximumLength(Release.MaxLanguageCodeLength);
        RuleFor(x => x.LabelName).MaximumLength(Release.MaxLabelNameLength);
        RuleFor(x => x.PLine).MaximumLength(Release.MaxRightsLineLength);
        RuleFor(x => x.CLine).MaximumLength(Release.MaxRightsLineLength);
        RuleFor(x => x.OriginalReleaseDate)
            .Must(d => d is null || d.Value.Offset == TimeSpan.Zero)
            .WithMessage("Timestamp must be UTC (offset Z).");
        RuleFor(x => x.Slug)
            .MaximumLength(Slug.MaxLength)
            .Must(slug => slug is null || CatalogSlugHelper.TryParseReleaseSlug(slug).IsSuccess)
            .WithMessage("Slug must be lowercase letters, numbers, and single hyphens (e.g. my-release-name).")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));
    }
}

internal sealed class UpdateReleaseRequestValidator : AbstractValidator<UpdateReleaseRequest>
{
    public UpdateReleaseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Release.MaxTitleLength);
        RuleFor(x => x.ReleaseType).IsInEnum();
        RuleFor(x => x.ReleaseDate).MustBeUtc();
        RuleFor(x => x.Description).MustBeValidFormattedCatalogText();
        RuleFor(x => x.Upc).MaximumLength(Release.MaxUpcLength);
        RuleFor(x => x.PrimaryGenre).MaximumLength(Release.MaxGenreLength);
        RuleFor(x => x.Tags).MaximumLength(Release.MaxTagsLength);
        RuleFor(x => x.LanguageCode).MaximumLength(Release.MaxLanguageCodeLength);
        RuleFor(x => x.LabelName).MaximumLength(Release.MaxLabelNameLength);
        RuleFor(x => x.PLine).MaximumLength(Release.MaxRightsLineLength);
        RuleFor(x => x.CLine).MaximumLength(Release.MaxRightsLineLength);
        RuleFor(x => x.OriginalReleaseDate)
            .Must(d => d is null || d.Value.Offset == TimeSpan.Zero)
            .WithMessage("Timestamp must be UTC (offset Z).");
        RuleFor(x => x.Slug)
            .MaximumLength(Slug.MaxLength)
            .Must(slug => slug is null || CatalogSlugHelper.TryParseReleaseSlug(slug).IsSuccess)
            .WithMessage("Slug must be lowercase letters, numbers, and single hyphens (e.g. my-release-name).")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));
    }
}

internal sealed class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Track.MaxTitleLength);
        RuleFor(x => x.TrackNumber).GreaterThan(0);
        RuleFor(x => x.DurationMs).GreaterThan(0);
        RuleFor(x => x.Isrc).MaximumLength(Track.MaxIsrcLength);
        RuleFor(x => x.Lyrics).MaximumLength(Track.MaxLyricsLength);
        RuleFor(x => x.LanguageCode).MaximumLength(Track.MaxLanguageCodeLength);
        RuleFor(x => x.VersionTitle).MaximumLength(Track.MaxVersionTitleLength);
        RuleFor(x => x.ComposerCredits).MaximumLength(Track.MaxCreditsLength);
    }
}

internal sealed class UpdateTrackRequestValidator : AbstractValidator<UpdateTrackRequest>
{
    public UpdateTrackRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Track.MaxTitleLength);
        RuleFor(x => x.TrackNumber).GreaterThan(0);
        RuleFor(x => x.Isrc).MaximumLength(Track.MaxIsrcLength);
        RuleFor(x => x.Lyrics).MaximumLength(Track.MaxLyricsLength);
        RuleFor(x => x.LanguageCode).MaximumLength(Track.MaxLanguageCodeLength);
        RuleFor(x => x.VersionTitle).MaximumLength(Track.MaxVersionTitleLength);
        RuleFor(x => x.ComposerCredits).MaximumLength(Track.MaxCreditsLength);
    }
}

internal sealed class PresignReleaseCoverUploadRequestValidator : AbstractValidator<PresignReleaseCoverUploadRequest>
{
    public PresignReleaseCoverUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
    }
}

internal sealed class CompleteReleaseCoverUploadRequestValidator : AbstractValidator<CompleteReleaseCoverUploadRequest>
{
    public CompleteReleaseCoverUploadRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(Release.MaxKeyLength);
    }
}

internal sealed class PresignArtistAvatarUploadRequestValidator : AbstractValidator<PresignArtistAvatarUploadRequest>
{
    public PresignArtistAvatarUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
    }
}

internal sealed class CompleteArtistAvatarUploadRequestValidator : AbstractValidator<CompleteArtistAvatarUploadRequest>
{
    public CompleteArtistAvatarUploadRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(Artist.MaxKeyLength);
    }
}

internal sealed class PresignArtistCoverUploadRequestValidator : AbstractValidator<PresignArtistCoverUploadRequest>
{
    public PresignArtistCoverUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
    }
}

internal sealed class CompleteArtistCoverUploadRequestValidator : AbstractValidator<CompleteArtistCoverUploadRequest>
{
    public CompleteArtistCoverUploadRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(Artist.MaxKeyLength);
    }
}
