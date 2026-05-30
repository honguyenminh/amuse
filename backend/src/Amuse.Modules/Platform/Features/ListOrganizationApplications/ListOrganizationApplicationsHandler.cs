using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Platform.Features.ListOrganizationApplications;

internal sealed class ListOrganizationApplicationsHandler(
    IOrganizationLifecycleCommands lifecycleCommands)
{
    public async Task<Result<IReadOnlyList<OrganizationApplicationResponse>>> HandleAsync(
        OrganizationOnboardingStatus? status,
        CancellationToken cancellationToken)
    {
        if (status is not null and not OrganizationOnboardingStatus.PendingReview)
            return Result<IReadOnlyList<OrganizationApplicationResponse>>.Success([]);

        var applications = await lifecycleCommands.ListPendingBackingApplicationsAsync(cancellationToken);
        var responses = applications
            .Select(OrganizationApplicationMapper.ToResponse)
            .ToList();

        return Result<IReadOnlyList<OrganizationApplicationResponse>>.Success(responses);
    }
}
