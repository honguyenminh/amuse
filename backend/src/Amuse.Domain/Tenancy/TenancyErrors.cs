using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public static class TenancyErrors
{
    public static readonly DomainError OrganizationNotFound =
        new("tenancy.organization_not_found", "Organization was not found.");

    public static readonly DomainError OrganizationClosed =
        new("tenancy.organization_closed", "Organization is closed.");

    public static readonly DomainError InvalidDisplayName =
        new("tenancy.invalid_display_name", "Organization display name is invalid.");

    public static readonly DomainError NotOrganizationMember =
        new("tenancy.not_organization_member", "Account is not a member of this organization.");

    public static readonly DomainError InvalidOnboardingTransition =
        new("tenancy.invalid_onboarding_transition", "Organization onboarding state does not allow this action.");

    public static readonly DomainError InvalidLifecycleTransition =
        new("tenancy.invalid_lifecycle_transition", "Organization lifecycle state does not allow this action.");

    public static readonly DomainError InvalidRejectionReason =
        new("tenancy.invalid_rejection_reason", "Rejection reason is required and must not exceed the maximum length.");

    public static readonly DomainError InvalidOnboardingStatusFilter =
        new("tenancy.invalid_onboarding_status_filter", "Onboarding status filter is invalid.");
}
