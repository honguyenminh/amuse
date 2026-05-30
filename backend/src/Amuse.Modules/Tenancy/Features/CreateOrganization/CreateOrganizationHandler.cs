using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

internal sealed class CreateOrganizationHandler(TenancyDbContext dbContext, IClock clock)
{
    public async Task<Result<OrganizationResponse>> HandleAsync(
        CreateOrganizationRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<OrganizationResponse>.Failure(accountResult.Error!);

        var accountId = accountResult.Value!;
        var now = clock.UtcNow;

        var organizationResult = request.OrgClass switch
        {
            OrganizationClass.IndieGroup => Organization.RegisterIndieGroup(
                request.DisplayName,
                accountId,
                now),
            OrganizationClass.BackingOrg => Organization.RegisterBackingOrg(
                request.DisplayName,
                accountId,
                now),
            _ => Result<Organization>.Failure(TenancyErrors.InvalidDisplayName),
        };

        if (!organizationResult.IsSuccess)
            return Result<OrganizationResponse>.Failure(organizationResult.Error!);

        var organization = organizationResult.Value!;
        var owner = OrganizationMember.CreateOwner(
            organization.Id,
            accountId,
            OrgClaimPresets.OwnerPresetLabel,
            OrgClaimPresets.OwnerAdmin);

        dbContext.Organizations.Add(organization);
        dbContext.OrganizationMembers.Add(owner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<OrganizationResponse>.Success(
            OrganizationDtoMapper.ToResponse(organization, isOwner: true));
    }
}
