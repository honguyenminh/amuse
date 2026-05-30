using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public sealed class Organization
{
    public const int MaxDisplayNameLength = 200;
    public const int MaxRejectionReasonLength = 2000;

    public OrganizationId Id { get; private set; }
    public string DisplayName { get; private set; } = null!;
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
        OrganizationClass orgClass,
        OrganizationLifecycleStatus lifecycleStatus,
        OrganizationOnboardingStatus onboardingStatus,
        OrganizationTrustTier trustTier,
        AccountId createdByAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
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
        DateTimeOffset now) =>
        Create(
            displayName,
            OrganizationClass.IndieGroup,
            OrganizationLifecycleStatus.Active,
            OrganizationOnboardingStatus.NotRequired,
            OrganizationTrustTier.Unverified,
            createdBy,
            now);

    public static Result<Organization> RegisterBackingOrg(
        string displayName,
        AccountId createdBy,
        DateTimeOffset now) =>
        Create(
            displayName,
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
                    CanPublishPublic: false,
                    CanReadPayout: false),

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

        return Result<Organization>.Success(new Organization(
            OrganizationId.New(),
            normalized,
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
}
