using Amuse.Domain.Platform;
using Microsoft.AspNetCore.Authorization;

namespace Amuse.Modules.Common.Authorization;

public sealed class PlatformAccountingReadRequirement : IAuthorizationRequirement;

public sealed class PlatformAccountingReadHandler : AuthorizationHandler<PlatformAccountingReadRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAccountingReadRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanReadAccounting(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class PlatformAccountingManageRequirement : IAuthorizationRequirement;

public sealed class PlatformAccountingManageHandler : AuthorizationHandler<PlatformAccountingManageRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAccountingManageRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanManageAccounting(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class PlatformPayoutManageRequirement : IAuthorizationRequirement;

public sealed class PlatformPayoutManageHandler : AuthorizationHandler<PlatformPayoutManageRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformPayoutManageRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanManagePayouts(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class PlatformPurchaseManageRequirement : IAuthorizationRequirement;

public sealed class PlatformPurchaseManageHandler : AuthorizationHandler<PlatformPurchaseManageRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformPurchaseManageRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "platform", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var claims = context.User.FindAll("claims").Select(c => c.Value).ToList();
        if (PlatformClaims.CanManagePurchases(claims))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
