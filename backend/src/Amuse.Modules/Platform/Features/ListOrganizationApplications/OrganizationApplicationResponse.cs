namespace Amuse.Modules.Platform.Features.ListOrganizationApplications;

public sealed record OrganizationApplicationOwner(
    Guid AccountId,
    string? Email,
    string IdpIssuer,
    string IdpSubject,
    string AccountStatus);

public sealed record OrganizationApplicationResponse(
    Guid OrganizationId,
    string DisplayName,
    string OrgClass,
    string OnboardingStatus,
    string TrustTier,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    OrganizationApplicationOwner Owner);
