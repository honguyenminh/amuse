using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Identity.Contracts;

public sealed record OrgPersonaListing(
    Guid OrganizationId,
    string DisplayName,
    string? PresetRoleLabel,
    OrganizationClass OrgClass,
    OrganizationLifecycleStatus LifecycleStatus,
    OrganizationOnboardingStatus OnboardingStatus,
    OrganizationTrustTier TrustTier);
