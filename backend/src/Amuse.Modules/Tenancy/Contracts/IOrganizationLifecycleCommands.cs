using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Contracts;

public sealed record OrganizationApplicationSummary(
    Guid OrganizationId,
    string DisplayName,
    OrganizationClass OrgClass,
    OrganizationOnboardingStatus OnboardingStatus,
    OrganizationTrustTier TrustTier,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    OrganizationApplicationOwner Owner);

public interface IOrganizationLifecycleCommands
{
    Task<IReadOnlyList<OrganizationApplicationSummary>> ListPendingBackingApplicationsAsync(
        CancellationToken cancellationToken);

    Task<Result> ApproveBackingOrganizationAsync(
        OrganizationId organizationId,
        PlatformOperatorId operatorId,
        CancellationToken cancellationToken);

    Task<Result> RejectBackingOrganizationAsync(
        OrganizationId organizationId,
        string reason,
        CancellationToken cancellationToken);
}
