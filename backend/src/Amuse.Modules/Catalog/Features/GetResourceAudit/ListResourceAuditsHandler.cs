using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Audit;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.GetResourceAudit;

public sealed record CatalogAuditEntryResponse(
    Guid Id,
    string Action,
    string TableName,
    Guid TargetId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset ChangedAt,
    Guid? ActorAccountId);

public sealed record CatalogAuditListResponse(
    IReadOnlyList<CatalogAuditEntryResponse> Items);

internal sealed class ListResourceAuditsHandler(CatalogDbContext catalogDb, IAuditLogReadModel auditLog)
{
    public async Task<Result<CatalogAuditListResponse>> HandleAsync(
        string tableName,
        Guid targetId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<CatalogAuditListResponse>.Failure(orgResult.Error!);

        if (string.IsNullOrWhiteSpace(tableName))
            return Result<CatalogAuditListResponse>.Failure(CatalogErrors.Forbidden);

        var normalizedTable = tableName.Trim();
        if (normalizedTable is not (
            CatalogAuditTables.Artist
            or CatalogAuditTables.Release
            or CatalogAuditTables.Track
            or CatalogAuditTables.ReleaseGroup))
        {
            return Result<CatalogAuditListResponse>.Failure(CatalogErrors.Forbidden);
        }

        var scopeResult = await CatalogAuditScopeGuard.EnsureResourceAccessibleAsync(
            catalogDb,
            normalizedTable,
            targetId,
            orgResult.Value!,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<CatalogAuditListResponse>.Failure(scopeResult.Error!);

        var entries = await auditLog.QueryByTargetAsync(
            normalizedTable,
            targetId,
            take: 100,
            cancellationToken);

        var items = entries
            .Select(entry => new CatalogAuditEntryResponse(
                entry.Id,
                entry.Action,
                entry.TableName,
                entry.TargetId,
                entry.BeforeJson,
                entry.AfterJson,
                entry.ChangedAt,
                entry.ActorAccountId))
            .ToList();

        return Result<CatalogAuditListResponse>.Success(new CatalogAuditListResponse(items));
    }
}
