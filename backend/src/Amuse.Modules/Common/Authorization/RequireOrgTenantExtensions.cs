using Microsoft.AspNetCore.Builder;

namespace Amuse.Modules.Common.Authorization;

public static class RequireOrgTenantExtensions
{
    public static RouteHandlerBuilder RequireOrgTenant(this RouteHandlerBuilder builder) =>
        builder.WithMetadata(new RequireOrgTenantAttribute());
}
