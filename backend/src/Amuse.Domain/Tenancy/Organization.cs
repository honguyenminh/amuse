using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public sealed class Organization
{
    public const int MaxDisplayNameLength = 200;
    public const int MaxRejectionReasonLength = 2000;
    public const int MaxDescriptionLength = 2000;
    public const int MaxWebsiteUrlLength = 500;
    public const int MaxCountryCodeLength = 2;
    public const int MaxImprintNameLength = 200;

    public OrganizationId Id { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? CountryCode { get; private set; }
    public string? ImprintName { get; private set; }
    public OrganizationClass OrgClass { get; private set; }
    public OrganizationLifecycleStatus LifecycleStatus { get; private set; }
    public OrganizationOnboardingStatus OnboardingStatus { get; private set; }
    public OrganizationTrustTier TrustTier { get; private set; }
    public AccountId CreatedByAccountId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public PlatformOperatorId? ApprovedByOperatorId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Organization()
    {
    }

    private Organization(
        OrganizationId id,
        string displayName,
        string? description,
        string? websiteUrl,
        string? countryCode,
        string? imprintName,
        OrganizationClass orgClass,
        OrganizationLifecycleStatus lifecycleStatus,
        OrganizationOnboardingStatus onboardingStatus,
        OrganizationTrustTier trustTier,
        AccountId createdByAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        WebsiteUrl = websiteUrl;
        CountryCode = countryCode;
        ImprintName = imprintName;
        OrgClass = orgClass;
        LifecycleStatus = lifecycleStatus;
        OnboardingStatus = onboardingStatus;
        TrustTier = trustTier;
        CreatedByAccountId = createdByAccountId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public bool IsClosed => LifecycleStatus == OrganizationLifecycleStatus.Closed;

    public static Result<Organization> RegisterIndieGroup(
        string displayName,
        AccountId createdBy,
        DateTimeOffset now,
        string? description = null,
        string? websiteUrl = null,
        string? countryCode = null,
        string? imprintName = null) =>
        Create(
            displayName,
            description,
            websiteUrl,
            countryCode,
            imprintName,
            OrganizationClass.IndieGroup,
            OrganizationLifecycleStatus.Active,
            OrganizationOnboardingStatus.NotRequired,
            OrganizationTrustTier.Unverified,
            createdBy,
            now);

    public static Result<Organization> RegisterBackingOrg(
        string displayName,
        AccountId createdBy,
        DateTimeOffset now,
        string? description = null,
        string? websiteUrl = null,
        string? countryCode = null,
        string? imprintName = null) =>
        Create(
            displayName,
            description,
            websiteUrl,
            countryCode,
            imprintName,
            OrganizationClass.BackingOrg,
            OrganizationLifecycleStatus.Active,
            OrganizationOnboardingStatus.PendingReview,
            OrganizationTrustTier.Unverified,
            createdBy,
            now);

    public Result Approve(PlatformOperatorId operatorId, DateTimeOffset now)
    {
        if (OrgClass != OrganizationClass.BackingOrg)
            return Result.Failure(TenancyErrors.InvalidOnboardingTransition);

        if (OnboardingStatus != OrganizationOnboardingStatus.PendingReview)
            return Result.Failure(TenancyErrors.InvalidOnboardingTransition);

        if (LifecycleStatus == OrganizationLifecycleStatus.Closed)
            return Result.Failure(TenancyErrors.OrganizationClosed);

        OnboardingStatus = OrganizationOnboardingStatus.Approved;
        TrustTier = OrganizationTrustTier.PlatformVerified;
        ApprovedAt = now;
        ApprovedByOperatorId = operatorId;
        RejectionReason = null;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Reject(string reason, DateTimeOffset now)
    {
        if (OrgClass != OrganizationClass.BackingOrg)
            return Result.Failure(TenancyErrors.InvalidOnboardingTransition);

        if (OnboardingStatus != OrganizationOnboardingStatus.PendingReview)
            return Result.Failure(TenancyErrors.InvalidOnboardingTransition);

        var trimmed = (reason ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxRejectionReasonLength)
            return Result.Failure(TenancyErrors.InvalidRejectionReason);

        OnboardingStatus = OrganizationOnboardingStatus.Rejected;
        RejectionReason = trimmed;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset now)
    {
        if (LifecycleStatus == OrganizationLifecycleStatus.Closed)
            return Result.Failure(TenancyErrors.OrganizationClosed);

        LifecycleStatus = OrganizationLifecycleStatus.Suspended;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Reactivate(DateTimeOffset now)
    {
        if (LifecycleStatus != OrganizationLifecycleStatus.Suspended)
            return Result.Failure(TenancyErrors.InvalidLifecycleTransition);

        LifecycleStatus = OrganizationLifecycleStatus.Active;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Close(DateTimeOffset now)
    {
        if (LifecycleStatus == OrganizationLifecycleStatus.Closed)
            return Result.Failure(TenancyErrors.OrganizationClosed);

        LifecycleStatus = OrganizationLifecycleStatus.Closed;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RecoverFromClosed(DateTimeOffset now)
    {
        if (LifecycleStatus != OrganizationLifecycleStatus.Closed)
            return Result.Failure(TenancyErrors.InvalidLifecycleTransition);

        LifecycleStatus = OrganizationLifecycleStatus.Active;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateProfile(
        string? description,
        string? websiteUrl,
        string? countryCode,
        string? imprintName,
        DateTimeOffset now)
    {
        if (IsClosed)
            return Result.Failure(TenancyErrors.OrganizationClosed);

        if (description is { Length: > MaxDescriptionLength })
            return Result.Failure(TenancyErrors.InvalidOrganizationDescription);

        var normalizedDescription = NormalizeOptionalDescription(description);

        var normalizedWebsiteUrl = NormalizeUrl(websiteUrl);
        if (websiteUrl is not null && normalizedWebsiteUrl is null)
            return Result.Failure(TenancyErrors.InvalidOrganizationWebsiteUrl);

        var normalizedCountryCode = NormalizeOptionalText(countryCode, MaxCountryCodeLength)?.ToUpperInvariant();
        if (countryCode is not null && normalizedCountryCode is null)
            return Result.Failure(TenancyErrors.InvalidOrganizationCountryCode);

        var normalizedImprintName = NormalizeOptionalText(imprintName, MaxImprintNameLength);
        if (imprintName is not null && normalizedImprintName is null)
            return Result.Failure(TenancyErrors.InvalidOrganizationImprintName);

        Description = normalizedDescription;
        WebsiteUrl = normalizedWebsiteUrl;
        CountryCode = normalizedCountryCode;
        ImprintName = normalizedImprintName;
        UpdatedAt = now;
        return Result.Success();
    }

    public OrgCapabilities EvaluateCapabilities()
    {
        if (LifecycleStatus == OrganizationLifecycleStatus.Suspended
            || OnboardingStatus == OrganizationOnboardingStatus.Rejected)
        {
            return new OrgCapabilities(
                CanReadOrg: true,
                CanReadMembership: OnboardingStatus != OrganizationOnboardingStatus.Rejected,
                CanUpload: false,
                CanWriteDraft: false,
                CanPublishPublic: false,
                CanReadPayout: false);
        }

        if (LifecycleStatus != OrganizationLifecycleStatus.Active)
        {
            return new OrgCapabilities(false, false, false, false, false, false);
        }

        return OrgClass switch
        {
            OrganizationClass.IndieGroup when OnboardingStatus == OrganizationOnboardingStatus.NotRequired =>
                new OrgCapabilities(
                    CanReadOrg: true,
                    CanReadMembership: true,
                    CanUpload: true,
                    CanWriteDraft: true,
                    CanPublishPublic: true,
                    CanReadPayout: true),

            OrganizationClass.BackingOrg when OnboardingStatus == OrganizationOnboardingStatus.PendingReview =>
                new OrgCapabilities(
                    CanReadOrg: true,
                    CanReadMembership: true,
                    CanUpload: false,
                    CanWriteDraft: false,
                    CanPublishPublic: false,
                    CanReadPayout: false),

            OrganizationClass.BackingOrg when OnboardingStatus == OrganizationOnboardingStatus.Approved =>
                new OrgCapabilities(
                    CanReadOrg: true,
                    CanReadMembership: true,
                    CanUpload: true,
                    CanWriteDraft: true,
                    CanPublishPublic: true,
                    CanReadPayout: true),

            _ => new OrgCapabilities(false, false, false, false, false, false),
        };
    }

    private static Result<Organization> Create(
        string displayName,
        string? description,
        string? websiteUrl,
        string? countryCode,
        string? imprintName,
        OrganizationClass orgClass,
        OrganizationLifecycleStatus lifecycleStatus,
        OrganizationOnboardingStatus onboardingStatus,
        OrganizationTrustTier trustTier,
        AccountId createdBy,
        DateTimeOffset now)
    {
        var normalized = NormalizeDisplayName(displayName);
        if (normalized is null)
            return Result<Organization>.Failure(TenancyErrors.InvalidDisplayName);

        var normalizedDescription = NormalizeOptionalText(description, MaxDescriptionLength);
        if (description is not null && normalizedDescription is null)
            return Result<Organization>.Failure(TenancyErrors.InvalidOrganizationDescription);

        var normalizedWebsiteUrl = NormalizeUrl(websiteUrl);
        if (websiteUrl is not null && normalizedWebsiteUrl is null)
            return Result<Organization>.Failure(TenancyErrors.InvalidOrganizationWebsiteUrl);

        var normalizedCountryCode = NormalizeOptionalText(countryCode, MaxCountryCodeLength)?.ToUpperInvariant();
        if (countryCode is not null && normalizedCountryCode is null)
            return Result<Organization>.Failure(TenancyErrors.InvalidOrganizationCountryCode);

        var normalizedImprintName = NormalizeOptionalText(imprintName, MaxImprintNameLength);
        if (imprintName is not null && normalizedImprintName is null)
            return Result<Organization>.Failure(TenancyErrors.InvalidOrganizationImprintName);

        return Result<Organization>.Success(new Organization(
            OrganizationId.New(),
            normalized,
            normalizedDescription,
            normalizedWebsiteUrl,
            normalizedCountryCode,
            normalizedImprintName,
            orgClass,
            lifecycleStatus,
            onboardingStatus,
            trustTier,
            createdBy,
            now));
    }

    private static string? NormalizeDisplayName(string displayName)
    {
        var trimmed = (displayName ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxDisplayNameLength)
            return null;

        return trimmed;
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

    private static string? NormalizeOptionalDescription(string? description)
    {
        if (description is null)
            return null;
        var trimmed = description.Trim();
        if (trimmed.Length == 0)
            return null;
        if (trimmed.Length > MaxDescriptionLength)
            return null;
        return trimmed;
    }

    private static string? NormalizeUrl(string? url)
    {
        var normalized = NormalizeOptionalText(url, MaxWebsiteUrlLength);
        if (normalized is null)
            return null;
        return Uri.TryCreate(normalized, UriKind.Absolute, out var parsed)
               && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            ? normalized
            : null;
    }
}
