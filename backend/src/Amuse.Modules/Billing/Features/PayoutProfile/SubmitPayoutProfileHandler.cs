using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

internal sealed class SubmitPayoutProfileHandler(
    BillingDbContext billingDb,
    IClock clock)
{
    public async Task<Result<PayoutProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(orgResult.Error!);

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == orgResult.Value, cancellationToken);

        if (profile is null)
            return Result<PayoutProfileResponse>.Failure(BillingErrors.PayoutProfileNotFound);

        var now = clock.UtcNow;
        var submitResult = profile.Submit(now);
        if (!submitResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(submitResult.Error!);

        var reviewResult = profile.EnterReview(now);
        if (!reviewResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(reviewResult.Error!);

        await billingDb.SaveChangesAsync(cancellationToken);
        return Result<PayoutProfileResponse>.Success(PayoutProfileMapper.ToResponse(profile));
    }
}
