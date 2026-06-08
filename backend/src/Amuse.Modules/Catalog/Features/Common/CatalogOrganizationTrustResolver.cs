using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogOrganizationTrustResolver
{
    internal static async Task<string> ResolveTrustTierAsync(
        ITenancyOrganizationReadModel organizationReadModel,
        OrganizationId? organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId is null)
            return OrganizationTrustTierJson.ToApiValue(OrganizationTrustTierJson.DefaultWhenMissing);

        var trustTier = await organizationReadModel.GetTrustTierAsync(organizationId.Value, cancellationToken);
        return OrganizationTrustTierJson.ToApiValue(
            trustTier ?? OrganizationTrustTierJson.DefaultWhenMissing);
    }

    internal static string ResolveTrustTier(
        OrganizationId? organizationId,
        IReadOnlyDictionary<Guid, OrganizationTrustTier> trustTiers)
    {
        if (organizationId is null)
            return OrganizationTrustTierJson.ToApiValue(OrganizationTrustTierJson.DefaultWhenMissing);

        if (!trustTiers.TryGetValue(organizationId.Value.Value, out var trustTier))
            return OrganizationTrustTierJson.ToApiValue(OrganizationTrustTierJson.DefaultWhenMissing);

        return OrganizationTrustTierJson.ToApiValue(trustTier);
    }

    internal static bool IsPlatformVerified(string trustTierApiValue) =>
        OrganizationTrustTierJson.IsPlatformVerified(
            Enum.TryParse<OrganizationTrustTier>(trustTierApiValue, ignoreCase: true, out var parsed)
                ? parsed
                : OrganizationTrustTierJson.DefaultWhenMissing);
}
