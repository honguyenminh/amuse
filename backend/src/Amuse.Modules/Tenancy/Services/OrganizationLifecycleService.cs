using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Services;

public sealed class OrganizationLifecycleService(
    TenancyDbContext dbContext,
    IOrganizationCreatorContactLookup creatorContacts,
    ICatalogManagedArtistVisibility catalogArtistVisibility,
    IClock clock) : IOrganizationLifecycleCommands
{
    public async Task<IReadOnlyList<OrganizationApplicationSummary>> ListPendingBackingApplicationsAsync(
        CancellationToken cancellationToken)
    {
        var organizations = await dbContext.Organizations
            .AsNoTracking()
            .Where(o =>
                o.OrgClass == OrganizationClass.BackingOrg
                && o.OnboardingStatus == OrganizationOnboardingStatus.PendingReview
                && o.LifecycleStatus == OrganizationLifecycleStatus.Active)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        if (organizations.Count == 0)
            return [];

        var accountIds = organizations
            .Select(o => o.CreatedByAccountId)
            .Distinct()
            .ToArray();

        var contacts = await creatorContacts.GetByAccountIdsAsync(accountIds, cancellationToken);

        return organizations
            .Select(o => new OrganizationApplicationSummary(
                o.Id.Value,
                o.DisplayName,
                o.OrgClass,
                o.OnboardingStatus,
                o.TrustTier,
                o.CreatedAt,
                o.UpdatedAt,
                ResolveOwner(o.CreatedByAccountId.Value, contacts)))
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizationApplicationSummary>> ListClosedOrganizationsAsync(
        CancellationToken cancellationToken)
    {
        var organizations = await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.LifecycleStatus == OrganizationLifecycleStatus.Closed)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync(cancellationToken);

        if (organizations.Count == 0)
            return [];

        var accountIds = organizations
            .Select(o => o.CreatedByAccountId)
            .Distinct()
            .ToArray();

        var contacts = await creatorContacts.GetByAccountIdsAsync(accountIds, cancellationToken);

        return organizations
            .Select(o => new OrganizationApplicationSummary(
                o.Id.Value,
                o.DisplayName,
                o.OrgClass,
                o.OnboardingStatus,
                o.TrustTier,
                o.CreatedAt,
                o.UpdatedAt,
                ResolveOwner(o.CreatedByAccountId.Value, contacts)))
            .ToList();
    }

    private static OrganizationApplicationOwner ResolveOwner(
        Guid accountId,
        IReadOnlyDictionary<Guid, OrganizationApplicationOwner> contacts) =>
        contacts.TryGetValue(accountId, out var owner)
            ? owner
            : new OrganizationApplicationOwner(
                accountId,
                null,
                "unknown",
                accountId.ToString(),
                "unknown");

    public async Task<Result> ApproveBackingOrganizationAsync(
        OrganizationId organizationId,
        PlatformOperatorId operatorId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization is null)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var approve = organization.Approve(operatorId, clock.UtcNow);
        if (!approve.IsSuccess)
            return approve;

        await dbContext.SaveChangesAsync(cancellationToken);

        await catalogArtistVisibility.SyncManagedArtistsForOrganizationAsync(
            organizationId,
            organization.TrustTier,
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RejectBackingOrganizationAsync(
        OrganizationId organizationId,
        string reason,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization is null)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var reject = organization.Reject(reason, clock.UtcNow);
        if (!reject.IsSuccess)
            return reject;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ForceTransferOwnershipAsync(
        OrganizationId organizationId,
        Guid targetMemberId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
        if (organization is null || organization.IsClosed)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var currentOwner = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == organizationId && m.IsOwner && m.Status == MembershipStatus.Active,
                cancellationToken);

        var target = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.Id == targetMemberId
                     && m.OrganizationId == organizationId
                     && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (target is null)
            return Result.Failure(TenancyErrors.MemberNotFound);

        var transfer = target.ForceOwnershipFrom(currentOwner);
        if (!transfer.IsSuccess)
            return transfer;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RecoverClosedOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
        if (organization is null || !organization.IsClosed)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var recover = organization.RecoverFromClosed(clock.UtcNow);
        if (!recover.IsSuccess)
            return recover;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
