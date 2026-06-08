using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogPricingGuard
{
    internal static async Task<Result> EnsurePricingChangesAllowedAsync(
        ITenancyOrganizationReadModel organizationReadModel,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var lifecycleStatus = await organizationReadModel.GetLifecycleStatusAsync(
            organizationId,
            cancellationToken);

        if (lifecycleStatus is OrganizationLifecycleStatus.Suspended
            or OrganizationLifecycleStatus.Closed)
        {
            return Result.Failure(CatalogErrors.PricingChangesBlocked);
        }

        return Result.Success();
    }
}
