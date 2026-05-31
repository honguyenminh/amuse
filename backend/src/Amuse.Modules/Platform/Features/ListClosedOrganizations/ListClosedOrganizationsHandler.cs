using Amuse.Domain.SharedKernel;
using Amuse.Modules.Platform.Features.ListOrganizationApplications;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Platform.Features.ListClosedOrganizations;

internal sealed class ListClosedOrganizationsHandler(
    IOrganizationLifecycleCommands lifecycleCommands)
{
    public async Task<Result<IReadOnlyList<OrganizationApplicationResponse>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var organizations = await lifecycleCommands.ListClosedOrganizationsAsync(cancellationToken);
        var responses = organizations
            .Select(OrganizationApplicationMapper.ToResponse)
            .ToList();

        return Result<IReadOnlyList<OrganizationApplicationResponse>>.Success(responses);
    }
}
