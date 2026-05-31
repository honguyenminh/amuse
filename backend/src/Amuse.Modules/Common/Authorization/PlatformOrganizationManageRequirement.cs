using Amuse.Domain.Platform;
using Microsoft.AspNetCore.Authorization;

namespace Amuse.Modules.Common.Authorization;

public sealed class PlatformOrganizationManageRequirement : IAuthorizationRequirement;

public sealed class PlatformOrganizationManageHandler
    : AuthorizationHandler<PlatformOrganizationManageRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformOrganizationManageRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanManageOrganizations(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
