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

    public static readonly DomainError InvalidOrganizationDescription =
        new("tenancy.invalid_organization_description", "Organization description is invalid.");

    public static readonly DomainError InvalidOrganizationWebsiteUrl =
        new("tenancy.invalid_organization_website_url", "Organization website URL is invalid.");

    public static readonly DomainError InvalidOrganizationCountryCode =
        new("tenancy.invalid_organization_country_code", "Organization country code is invalid.");

    public static readonly DomainError InvalidOrganizationImprintName =
        new("tenancy.invalid_organization_imprint_name", "Organization imprint name is invalid.");

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

    public static readonly DomainError InsufficientClaim =
        new("tenancy.insufficient_claim", "The current persona does not have the required permission.");

    public static readonly DomainError InvalidClaim =
        new("tenancy.invalid_claim", "One or more permission claims are invalid.");

    public static readonly DomainError ClaimNotAllowedForOrganization =
        new("tenancy.claim_not_allowed_for_organization", "One or more claims are not allowed for this organization's current capabilities.");

    public static readonly DomainError CannotModifyOwner =
        new("tenancy.cannot_modify_owner", "The organization owner cannot be removed or modified in this way.");

    public static readonly DomainError CannotRemoveSelf =
        new("tenancy.cannot_remove_self", "You cannot remove yourself from the organization.");

    public static readonly DomainError OwnerCannotLeaveOrganization =
        new(
            "tenancy.owner_cannot_leave_organization",
            "Organization owners cannot leave. Transfer ownership to another member first, or contact platform support.");

    public static readonly DomainError CannotDemoteOwner =
        new("tenancy.cannot_demote_owner", "The organization owner must retain full administrator permissions.");

    public static readonly DomainError NotOrganizationOwner =
        new("tenancy.not_organization_owner", "Only the organization owner can perform this action.");

    public static readonly DomainError CannotTransferOwnershipToOwner =
        new("tenancy.cannot_transfer_ownership_to_owner", "Ownership is already held by the target member.");

    public static readonly DomainError DuplicateMember =
        new("tenancy.duplicate_member", "An active member already exists for this account.");

    public static readonly DomainError MemberNotFound =
        new("tenancy.member_not_found", "Organization member was not found.");

    public static readonly DomainError InvalidMembershipTransition =
        new("tenancy.invalid_membership_transition", "Membership state does not allow this action.");

    public static readonly DomainError InviteNotFound =
        new("tenancy.invite_not_found", "Organization invite was not found.");

    public static readonly DomainError InviteExpired =
        new("tenancy.invite_expired", "Organization invite has expired.");

    public static readonly DomainError InviteEmailMismatch =
        new("tenancy.invite_email_mismatch", "Signed-in account email does not match the invite email.");

    public static readonly DomainError InvalidInviteEmail =
        new("tenancy.invalid_invite_email", "Invite email is invalid.");

    public static readonly DomainError InvalidInviteTransition =
        new("tenancy.invalid_invite_transition", "Invite state does not allow this action.");

    public static readonly DomainError DuplicatePendingInvite =
        new("tenancy.duplicate_pending_invite", "A pending invite already exists for this email.");

    public static readonly DomainError InvalidPortalProfileDisplayName =
        new("tenancy.invalid_portal_profile_display_name", "Business portal display name is invalid.");

    public static readonly DomainError InvalidPortalProfileAvatarAccentSeed =
        new("tenancy.invalid_portal_profile_avatar_accent_seed", "Avatar accent seed is out of range.");

    public static readonly DomainError PortalProfileIncomplete =
        new("tenancy.portal_profile_incomplete", "Business portal profile is not complete.");

    public static readonly DomainError InvalidPortalAvatarUploadRequest =
        new("tenancy.invalid_portal_avatar_upload_request", "Avatar upload request is invalid.");

    public static readonly DomainError InvalidPortalAvatarObjectKey =
        new("tenancy.invalid_portal_avatar_object_key", "Avatar object key is invalid.");

    public static readonly DomainError PortalAvatarObjectMissing =
        new("tenancy.portal_avatar_object_missing", "Avatar image was not found in storage.");
}
