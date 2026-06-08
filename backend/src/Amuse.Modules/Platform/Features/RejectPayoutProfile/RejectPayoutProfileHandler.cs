using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Platform.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.RejectPayoutProfile;

internal sealed class RejectPayoutProfileHandler(
    BillingDbContext billingDb,
    IPlatformOperatorLookup operatorLookup,
    IAuditWriter auditWriter,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        RejectPayoutProfileRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = PlatformAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        var operatorId = await operatorLookup.GetOperatorIdForAccountAsync(
            accountResult.Value!,
            cancellationToken);

        if (operatorId is null)
            return Result.Failure(IdentityErrors.InvalidPersonaContext);

        if (organizationId == Guid.Empty)
            return Result.Failure(BillingErrors.PayoutProfileNotFound);

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(
                p => p.OrganizationId == OrganizationId.From(organizationId),
                cancellationToken);

        if (profile is null)
            return Result.Failure(BillingErrors.PayoutProfileNotFound);

        var now = clock.UtcNow;
        var rejectResult = profile.Reject(request.Reason, now);
        if (!rejectResult.IsSuccess)
            return rejectResult;

        await billingDb.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "payout_profile_rejected",
            TableName = "billing.payout_profile",
            TargetId = profile.Id.Value,
            ChangedAt = now,
            ActorAccountId = accountResult.Value!.Value,
            Reason = request.Reason,
        }, cancellationToken);

        return Result.Success();
    }
}
