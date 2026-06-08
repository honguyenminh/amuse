using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Billing.Features.CheckOwnership;

internal sealed class CheckOwnershipHandler(
    IEntitlementReadModel entitlements,
    IListenerPersonaReadModel personaReadModel)
{
    public async Task<Result<OwnershipCheckResponse>> HandleAsync(
        Guid? trackId,
        Guid? releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId is null && releaseId is null)
            return Result<OwnershipCheckResponse>.Failure(BillingErrors.InvalidAcquisitionTarget);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<OwnershipCheckResponse>.Failure(listenerResult.Error!);

        var accountId = listenerResult.Value!.AccountId;

        if (trackId is { } resolvedTrackId)
        {
            if (resolvedTrackId == Guid.Empty)
                return Result<OwnershipCheckResponse>.Failure(BillingErrors.TrackNotFound);

            if (releaseId is null)
                return Result<OwnershipCheckResponse>.Failure(BillingErrors.InvalidAcquisitionTarget);

            if (releaseId == Guid.Empty)
                return Result<OwnershipCheckResponse>.Failure(BillingErrors.ReleaseNotFound);

            var ownsTrack = await entitlements.OwnsTrackAsync(
                accountId,
                resolvedTrackId,
                releaseId.Value,
                cancellationToken);
            var ownsRelease = await entitlements.OwnsReleaseAsync(
                accountId,
                releaseId.Value,
                cancellationToken);

            return Result<OwnershipCheckResponse>.Success(
                new OwnershipCheckResponse(ownsTrack, ownsRelease));
        }

        if (releaseId is { } resolvedReleaseId)
        {
            if (resolvedReleaseId == Guid.Empty)
                return Result<OwnershipCheckResponse>.Failure(BillingErrors.ReleaseNotFound);

            var ownsRelease = await entitlements.OwnsReleaseAsync(
                accountId,
                resolvedReleaseId,
                cancellationToken);

            return Result<OwnershipCheckResponse>.Success(
                new OwnershipCheckResponse(OwnsTrack: false, ownsRelease));
        }

        return Result<OwnershipCheckResponse>.Failure(BillingErrors.InvalidAcquisitionTarget);
    }
}
