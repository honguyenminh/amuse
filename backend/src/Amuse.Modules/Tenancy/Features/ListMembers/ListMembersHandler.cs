using Amuse.Domain.Tenancy;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.ListMembers;

internal sealed class ListMembersHandler(
    TenancyDbContext dbContext,
    IAccountMemberActivityLookup activityLookup)
{
    public async Task<Result<OrganizationMemberListResponse>> HandleAsync(
        Guid organizationId,
        ListMembersQuery query,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return Result<OrganizationMemberListResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var orgExists = await dbContext.Organizations.AsNoTracking()
            .AnyAsync(o => o.Id == orgId, cancellationToken);
        if (!orgExists)
            return Result<OrganizationMemberListResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var members = await dbContext.OrganizationMembers.AsNoTracking()
            .Where(m => m.OrganizationId == orgId && m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

        var accountIds = members.Select(m => m.AccountId).ToArray();
        var activity = await activityLookup.GetByAccountIdsAsync(orgId, accountIds, cancellationToken);

        var rows = members.Select(member =>
        {
            activity.TryGetValue(member.AccountId.Value, out var snapshot);
            return new MemberRow(
                member,
                snapshot?.Email,
                snapshot?.JoinedAt,
                snapshot?.LastLoginAt,
                snapshot?.LastActiveAt);
        }).ToList();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLowerInvariant();
            rows = rows.Where(row =>
                    (row.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || row.Member.AccountId.Value.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (row.Member.PresetRoleLabel?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || row.Member.Claims.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        rows = query.SortBy switch
        {
            "preset" => Order(rows, r => r.Member.PresetRoleLabel ?? string.Empty, query.SortDirection),
            "lastlogin" => OrderDate(rows, r => r.LastLoginAt, query.SortDirection),
            "lastactive" => OrderDate(rows, r => r.LastActiveAt, query.SortDirection),
            "joined" => OrderDate(rows, r => r.JoinedAt, query.SortDirection),
            _ => Order(rows, r => r.Email ?? r.Member.AccountId.Value.ToString(), query.SortDirection),
        };

        var totalCount = rows.Count;
        var pageRows = rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new OrganizationMemberResponse(
                row.Member.Id,
                row.Member.AccountId.Value,
                row.Email,
                row.Member.Status.ToString(),
                row.Member.PresetRoleLabel,
                row.Member.Claims,
                row.Member.IsOwner,
                row.JoinedAt,
                row.LastLoginAt,
                row.LastActiveAt))
            .ToList();

        var pendingInviteCount = await dbContext.OrganizationInvites.AsNoTracking()
            .CountAsync(
                i => i.OrganizationId == orgId && i.Status == OrganizationInviteStatus.Pending,
                cancellationToken);

        return Result<OrganizationMemberListResponse>.Success(new OrganizationMemberListResponse(
            pageRows,
            totalCount,
            query.Page,
            query.PageSize,
            pendingInviteCount));
    }

    private static List<MemberRow> Order<TKey>(
        List<MemberRow> rows,
        Func<MemberRow, TKey> keySelector,
        string direction)
        where TKey : IComparable<TKey>
    {
        return direction == "desc"
            ? rows.OrderByDescending(keySelector).ThenByDescending(r => r.Member.IsOwner).ToList()
            : rows.OrderBy(keySelector).ThenByDescending(r => r.Member.IsOwner).ToList();
    }

    private static List<MemberRow> OrderDate(
        List<MemberRow> rows,
        Func<MemberRow, DateTimeOffset?> keySelector,
        string direction)
    {
        return direction == "desc"
            ? rows.OrderByDescending(r => keySelector(r) ?? DateTimeOffset.MinValue)
                .ThenByDescending(r => r.Member.IsOwner)
                .ToList()
            : rows.OrderByDescending(r => keySelector(r).HasValue)
                .ThenBy(r => keySelector(r) ?? DateTimeOffset.MaxValue)
                .ThenByDescending(r => r.Member.IsOwner)
                .ToList();
    }

    private sealed record MemberRow(
        OrganizationMember Member,
        string? Email,
        DateTimeOffset? JoinedAt,
        DateTimeOffset? LastLoginAt,
        DateTimeOffset? LastActiveAt);
}
