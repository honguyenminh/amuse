using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

internal sealed class CreateOrganizationHandler(
    TenancyDbContext dbContext,
    IClock clock,
    IPlatformOperatorLookup platformOperatorLookup,
    IAuditWriter auditWriter)
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

        var instantApprove = await TryInstantApproveBackingOrgAsync(
            organization,
            accountId,
            now,
            cancellationToken);
        if (!instantApprove.IsSuccess)
            return Result<OrganizationResponse>.Failure(instantApprove.Error!);

        dbContext.Organizations.Add(organization);
        dbContext.OrganizationMembers.Add(owner);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (organization.OnboardingStatus == OrganizationOnboardingStatus.Approved)
        {
            await auditWriter.WriteAsync(new AuditEntry
            {
                Id = Guid.CreateVersion7(),
                Action = "organization_approved",
                TableName = "tenancy.organization",
                TargetId = organization.Id.Value,
                ChangedAt = now,
                ActorAccountId = accountId.Value,
            }, cancellationToken);
        }

        return Result<OrganizationResponse>.Success(
            OrganizationDtoMapper.ToResponse(organization, isOwner: true));
    }

    private async Task<Result> TryInstantApproveBackingOrgAsync(
        Organization organization,
        AccountId accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (organization.OrgClass != OrganizationClass.BackingOrg)
            return Result.Success();

        var platformClaims = await platformOperatorLookup.GetEffectiveClaimsForAccountAsync(
            accountId,
            cancellationToken);
        if (!PlatformClaims.CanInstantApproveOrganizationsOnCreate(platformClaims))
            return Result.Success();

        var operatorId = await platformOperatorLookup.GetOperatorIdForAccountAsync(
            accountId,
            cancellationToken);
        if (operatorId is null)
            return Result.Failure(IdentityErrors.InvalidPersonaContext);

        return organization.Approve(operatorId.Value, now);
    }
}
