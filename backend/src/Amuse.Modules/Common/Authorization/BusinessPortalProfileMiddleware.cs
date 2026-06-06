using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Common.Authorization;

public sealed class BusinessPortalProfileMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowlistedPrefixes =
    [
        "/api/v1/tenancy/portal-profile",
        "/api/v1/tenancy/organizations",
        "/api/v1/tenancy/invites/",
        "/api/v1/tenancy/claim-presets",
        "/api/v1/identity/",
        "/api/v1/platform/",
    ];

    public async Task InvokeAsync(
        HttpContext context,
        IBusinessPortalProfileOnboardingReadModel onboardingStatus)
    {
        if (!ShouldEnforce(context))
        {
            await next(context);
            return;
        }

        var sub = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var accountGuid))
        {
            await next(context);
            return;
        }

        var isComplete = await onboardingStatus.IsCompleteAsync(
            AccountId.From(accountGuid),
            context.RequestAborted);

        if (isComplete)
        {
            await next(context);
            return;
        }

        await WriteProblemAsync(context, TenancyErrors.PortalProfileIncomplete);
    }

    private static bool ShouldEnforce(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        var ctx = context.User.FindFirst("ctx")?.Value;
        if (!string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ctx, "platform", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        return !AllowlistedPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteProblemAsync(HttpContext context, Domain.SharedKernel.DomainError error)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            title = error.Code,
            status = StatusCodes.Status403Forbidden,
            detail = error.Message,
            code = error.Code,
        }, context.RequestAborted);
    }
}
