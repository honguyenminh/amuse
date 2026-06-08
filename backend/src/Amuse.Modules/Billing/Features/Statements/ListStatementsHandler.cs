using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.Statements;

internal sealed class ListStatementsHandler(BillingDbContext billingDb)
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedStatementsResponse>> HandleAsync(
        int? page,
        int? pageSize,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PagedStatementsResponse>.Failure(orgResult.Error!);

        var resolvedPage = page is null or < 1 ? 1 : page.Value;
        var resolvedPageSize = pageSize is null or < 1
            ? DefaultPageSize
            : Math.Min(pageSize.Value, MaxPageSize);

        var orgId = orgResult.Value!;
        var query = billingDb.PurchaseAllocationSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.PayeeOrganizationId == orgId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(snapshot => new StatementLineRow(
                snapshot.Id.Value,
                snapshot.PurchaseId.Value,
                snapshot.TrackId,
                snapshot.ShareBps,
                snapshot.AmountMinor,
                snapshot.Currency,
                snapshot.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedStatementsResponse>.Success(new PagedStatementsResponse(
            items,
            resolvedPage,
            resolvedPageSize,
            totalCount));
    }
}
