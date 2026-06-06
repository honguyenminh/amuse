using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class CatalogPersonaAccessor
{
    public static Result<OrganizationId> GetOrganizationId(ClaimsPrincipal principal)
    {
        if (!string.Equals(principal.FindFirst("ctx")?.Value, "org", StringComparison.OrdinalIgnoreCase))
            return Result<OrganizationId>.Failure(CatalogErrors.Forbidden);

        var orgIdValue = principal.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdValue, out var orgId))
            return Result<OrganizationId>.Failure(CatalogErrors.Forbidden);

        return Result<OrganizationId>.Success(OrganizationId.From(orgId));
    }

    public static IReadOnlySet<string> GetGrantedClaims(ClaimsPrincipal principal) =>
        principal.FindAll("claims")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
}

internal static class CatalogScopeGuard
{
    public static Result EnsureOrganizationScope(OrganizationId tokenOrgId, OrganizationId resourceOrgId)
    {
        if (tokenOrgId != resourceOrgId)
            return Result.Failure(CatalogErrors.NotOrganizationCatalog);

        return Result.Success();
    }

    public static Result EnsureArtistManagedBy(Artist artist, OrganizationId organizationId)
    {
        if (!artist.IsManagedBy(organizationId))
            return Result.Failure(CatalogErrors.NotOrganizationCatalog);

        return Result.Success();
    }
}
