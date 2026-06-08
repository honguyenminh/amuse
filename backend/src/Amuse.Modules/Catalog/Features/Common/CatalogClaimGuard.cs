using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Features.Common;

internal readonly record struct CatalogReadContext(
    string ResourceKind,
    Guid ResourceId,
    Guid? ArtistId = null,
    Guid? ReleaseId = null,
    Guid? ReleaseGroupId = null);

internal static class CatalogClaimGuard
{
    public static bool HasCatalogReadAll(ClaimsPrincipal principal) =>
        OrgClaim.Matches(
            OrgClaim.ScopeWideClaim("read", "catalog"),
            CatalogPersonaAccessor.GetGrantedClaims(principal));

    public static bool CanReadCatalogResource(ClaimsPrincipal principal, string resourceKind, Guid resourceId) =>
        OrgClaim.Matches(
            $"read:catalog:{resourceKind}:{resourceId:D}",
            CatalogPersonaAccessor.GetGrantedClaims(principal));

    public static bool CanRead(ClaimsPrincipal principal, CatalogReadContext context)
    {
        if (HasCatalogReadAll(principal))
            return true;

        if (CanReadCatalogResource(principal, context.ResourceKind, context.ResourceId))
            return true;

        if (context.ArtistId is { } artistId &&
            CanReadCatalogResource(principal, "artist", artistId) &&
            context.ResourceKind is "release_group" or "release" or "track")
        {
            return true;
        }

        if (context.ReleaseGroupId is { } releaseGroupId &&
            CanReadCatalogResource(principal, "release_group", releaseGroupId) &&
            context.ResourceKind is "release")
        {
            return true;
        }

        if (context.ReleaseId is { } releaseId &&
            CanReadCatalogResource(principal, "release", releaseId) &&
            context.ResourceKind is "track")
        {
            return true;
        }

        return false;
    }

    public static Result RequireRead(ClaimsPrincipal principal, string resourceKind, Guid resourceId) =>
        RequireRead(principal, new CatalogReadContext(resourceKind, resourceId));

    public static Result RequireRead(ClaimsPrincipal principal, CatalogReadContext context)
    {
        if (CanRead(principal, context))
            return Result.Success();

        return Result.Failure(CatalogErrors.Forbidden);
    }

    public static IReadOnlyList<T> FilterReadable<T>(
        ClaimsPrincipal principal,
        IEnumerable<T> items,
        Func<T, string> resourceKind,
        Func<T, Guid> resourceId)
    {
        return FilterReadable(
            principal,
            items,
            item => new CatalogReadContext(resourceKind(item), resourceId(item)));
    }

    public static IReadOnlyList<T> FilterReadable<T>(
        ClaimsPrincipal principal,
        IEnumerable<T> items,
        Func<T, CatalogReadContext> contextFactory)
    {
        if (HasCatalogReadAll(principal))
            return items.ToArray();

        return items
            .Where(item => CanRead(principal, contextFactory(item)))
            .ToArray();
    }
}
