using System.Security.Claims;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogAccountAccessor
{
    internal static Guid? TryGetAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(sub, out var accountId) ? accountId : null;
    }
}
