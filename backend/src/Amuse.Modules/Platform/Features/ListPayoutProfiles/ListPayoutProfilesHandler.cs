using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.ListPayoutProfiles;

internal sealed class ListPayoutProfilesHandler(BillingDbContext billingDb)
{
    public async Task<Result<IReadOnlyList<PlatformPayoutProfileRow>>> HandleAsync(
        PayoutVerificationStatus? status,
        CancellationToken cancellationToken)
    {
        var filterStatus = status ?? PayoutVerificationStatus.UnderReview;

        var rows = await billingDb.PayoutProfiles.AsNoTracking()
            .Where(p => p.VerificationStatus == filterStatus)
            .OrderBy(p => p.UpdatedAt)
            .Select(p => p)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlatformPayoutProfileRow>>.Success(
            rows.Select(PayoutProfileMapper.ToPlatformRow).ToList());
    }
}
