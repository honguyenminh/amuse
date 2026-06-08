using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace Amuse.Modules.Common.Authorization;

public sealed class RefundPurchaseRequirement : IAuthorizationRequirement;

public sealed class RefundPurchaseAuthorizationHandler : AuthorizationHandler<RefundPurchaseRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RefundPurchaseRequirement requirement)
    {
        var ctx = context.User.FindFirst("ctx")?.Value;
        var claims = context.User.FindAll("claims").Select(claim => claim.Value).ToList();

        if (string.Equals(ctx, "platform", StringComparison.OrdinalIgnoreCase)
            && PlatformClaims.CanManagePurchases(claims))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(context.User.FindFirst("org_id")?.Value)
            && OrgClaim.MatchesAny(
                [OrgClaim.ScopeSubClaim("manage", "purchase", "refund")],
                claims.ToHashSet(StringComparer.Ordinal)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
