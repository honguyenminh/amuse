using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Options;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Tenancy.Features.CreateInvite;

internal sealed class CreateInviteHandler(
    TenancyDbContext dbContext,
    IClock clock,
    ITenancyInviteEmailSender inviteEmailSender,
    IOptions<TenancyOptions> tenancyOptions)
{
    public async Task<Result<CreateOrganizationInviteResponse>> HandleAsync(
        Guid organizationId,
        CreateInviteRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<CreateOrganizationInviteResponse>.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken);
        if (organization is null || organization.IsClosed)
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var normalizedEmail = OrganizationInvite.NormalizeEmail(request.Email);
        if (normalizedEmail is null)
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.InvalidInviteEmail);

        var hasPending = await dbContext.OrganizationInvites.AnyAsync(
            i => i.OrganizationId == orgId
                 && i.Email == normalizedEmail
                 && i.Status == OrganizationInviteStatus.Pending,
            cancellationToken);
        if (hasPending)
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.DuplicatePendingInvite);

        var capabilities = organization.EvaluateCapabilities();
        if (!OrgClaimPresets.TryResolveClaims(request.PresetRoleLabel, request.Claims, out var resolvedClaims))
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.InvalidClaim);

        var assignable = OrgCapabilities.FilterAssignableClaims(resolvedClaims, capabilities);
        if (assignable.Count != resolvedClaims.Count)
            return Result<CreateOrganizationInviteResponse>.Failure(TenancyErrors.ClaimNotAllowedForOrganization);

        var now = clock.UtcNow;
        var inviteResult = OrganizationInvite.CreatePending(
            orgId,
            request.Email,
            accountResult.Value!,
            request.PresetRoleLabel,
            assignable,
            now);
        if (!inviteResult.IsSuccess)
            return Result<CreateOrganizationInviteResponse>.Failure(inviteResult.Error!);

        var (invite, rawToken) = inviteResult.Value!;
        dbContext.OrganizationInvites.Add(invite);
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = tenancyOptions.Value.BusinessPortalBaseUrl.TrimEnd('/');
        var inviteUrl = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(rawToken)}";
        await inviteEmailSender.SendOrganizationInviteAsync(
            normalizedEmail,
            organization.DisplayName,
            inviteUrl,
            cancellationToken);

        return Result<CreateOrganizationInviteResponse>.Success(
            new CreateOrganizationInviteResponse(invite.Id, invite.ExpiresAt));
    }
}
