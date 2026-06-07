using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.ListInvites;

internal sealed class ListInvitesHandler(TenancyDbContext dbContext)
{
    public async Task<Result<IReadOnlyList<OrganizationInviteResponse>>> HandleAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return Result<IReadOnlyList<OrganizationInviteResponse>>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var orgExists = await dbContext.Organizations.AsNoTracking()
            .AnyAsync(o => o.Id == orgId, cancellationToken);
        if (!orgExists)
            return Result<IReadOnlyList<OrganizationInviteResponse>>.Failure(TenancyErrors.OrganizationNotFound);

        var invites = await dbContext.OrganizationInvites.AsNoTracking()
            .Where(i => i.OrganizationId == orgId && i.Status == OrganizationInviteStatus.Pending)
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);

        var responses = invites.Select(i => new OrganizationInviteResponse(
            i.Id,
            i.Email,
            i.PresetRoleLabel,
            i.Claims,
            i.Status.ToString(),
            i.ExpiresAt,
            i.CreatedAt)).ToList();

        return Result<IReadOnlyList<OrganizationInviteResponse>>.Success(responses);
    }
}
