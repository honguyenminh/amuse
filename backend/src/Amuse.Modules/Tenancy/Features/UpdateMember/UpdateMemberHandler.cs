using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.UpdateMember;

internal sealed class UpdateMemberHandler(TenancyDbContext dbContext)
{
    public async Task<Result<OrganizationMemberResponse>> HandleAsync(
        Guid organizationId,
        Guid memberId,
        UpdateOrganizationMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return Result<OrganizationMemberResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken);
        if (organization is null || organization.IsClosed)
            return Result<OrganizationMemberResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var member = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.Id == memberId && m.OrganizationId == orgId && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (member is null)
            return Result<OrganizationMemberResponse>.Failure(TenancyErrors.MemberNotFound);

        var capabilities = organization.EvaluateCapabilities();
        var update = member.UpdateClaims(request.PresetRoleLabel, request.Claims ?? [], capabilities);
        if (!update.IsSuccess)
            return Result<OrganizationMemberResponse>.Failure(update.Error!);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<OrganizationMemberResponse>.Success(new OrganizationMemberResponse(
            member.Id,
            member.AccountId.Value,
            null,
            null,
            null,
            null,
            member.Status.ToString(),
            member.PresetRoleLabel,
            member.Claims,
            member.IsOwner,
            null,
            null,
            null));
    }
}
