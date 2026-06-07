using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.ListOrganizationAudit;

public sealed record TenancyAuditEntryResponse(
    Guid Id,
    string Action,
    string TableName,
    Guid TargetId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset ChangedAt,
    Guid? ActorAccountId);

public sealed record TenancyAuditListResponse(
    IReadOnlyList<TenancyAuditEntryResponse> Items);

internal sealed class ListOrganizationAuditsHandler(
    TenancyDbContext tenancyDb,
    IAuditLogReadModel auditLog)
{
    public async Task<Result<TenancyAuditListResponse>> HandleAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<TenancyAuditListResponse>.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result<TenancyAuditListResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var accountId = accountResult.Value!;

        var isMember = await tenancyDb.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.AccountId == accountId
                          && member.OrganizationId == orgId
                          && member.Status == MembershipStatus.Active,
                cancellationToken);

        if (!isMember)
            return Result<TenancyAuditListResponse>.Failure(TenancyErrors.NotOrganizationMember);

        var entries = await auditLog.QueryByTargetAsync(
            TenancyAuditTables.Organization,
            organizationId,
            take: 100,
            cancellationToken);

        var items = entries
            .Select(entry => new TenancyAuditEntryResponse(
                entry.Id,
                entry.Action,
                entry.TableName,
                entry.TargetId,
                entry.BeforeJson,
                entry.AfterJson,
                entry.ChangedAt,
                entry.ActorAccountId))
            .ToList();

        return Result<TenancyAuditListResponse>.Success(new TenancyAuditListResponse(items));
    }
}
