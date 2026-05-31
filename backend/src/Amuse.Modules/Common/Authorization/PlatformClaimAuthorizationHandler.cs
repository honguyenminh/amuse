using Amuse.Domain.Platform;
using Microsoft.AspNetCore.Authorization;

namespace Amuse.Modules.Common.Authorization;

public sealed class PlatformOrganizationReviewRequirement : IAuthorizationRequirement;

public sealed class PlatformOrganizationReviewHandler : AuthorizationHandler<PlatformOrganizationReviewRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformOrganizationReviewRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanReviewOrganizations(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
