using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

internal sealed class GetPayoutProfileHandler(BillingDbContext billingDb)
{
    public async Task<Result<PayoutProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(orgResult.Error!);

        var profile = await billingDb.PayoutProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrganizationId == orgResult.Value, cancellationToken);

        if (profile is null)
            return Result<PayoutProfileResponse>.Failure(BillingErrors.PayoutProfileNotFound);

        return Result<PayoutProfileResponse>.Success(PayoutProfileMapper.ToResponse(profile));
    }
}
