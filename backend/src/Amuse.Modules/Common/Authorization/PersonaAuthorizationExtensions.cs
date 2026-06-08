using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Common.Authorization;

public static class PersonaAuthorizationExtensions
{
    public static IServiceCollection AddPersonaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PersonaPolicies.RequireOrgPersona, policy =>
                policy.RequireClaim("ctx", "org").RequireClaim("org_id"));

            options.AddPolicy(PersonaPolicies.RequireListenerPersona, policy =>
                policy.RequireClaim("ctx", "listener").RequireClaim("listener_id"));

            options.AddPolicy(PersonaPolicies.RequirePlatformPersona, policy =>
                policy.RequireClaim("ctx", "platform"));

            options.AddPolicy(PersonaPolicies.RequireBusinessPortalPersona, policy =>
                policy.RequireAssertion(context =>
                {
                    var ctx = context.User.FindFirst("ctx")?.Value;
                    return string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(ctx, "platform", StringComparison.OrdinalIgnoreCase);
                }));

            options.AddPolicy(PlatformPolicies.RequireOrganizationReview, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformOrganizationReviewRequirement());
            });

            options.AddPolicy(PlatformPolicies.RequireOrganizationManage, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformOrganizationManageRequirement());
            });

            options.AddPolicy(PlatformPolicies.RequireAccountingRead, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformAccountingReadRequirement());
            });

            options.AddPolicy(PlatformPolicies.RequireAccountingManage, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformAccountingManageRequirement());
            });

            options.AddPolicy(PlatformPolicies.RequirePayoutManage, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformPayoutManageRequirement());
            });

            options.AddPolicy(PlatformPolicies.RequirePurchaseManage, policy =>
            {
                policy.RequireClaim("ctx", "platform");
                policy.Requirements.Add(new PlatformPurchaseManageRequirement());
            });

            options.AddPolicy(BillingPolicies.RefundPurchase, policy =>
                policy.Requirements.Add(new RefundPurchaseRequirement()));
        });

        services.AddSingleton<IAuthorizationHandler, PlatformOrganizationReviewHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformOrganizationManageHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformAccountingReadHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformAccountingManageHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformPayoutManageHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformPurchaseManageHandler>();
        services.AddSingleton<IAuthorizationHandler, RefundPurchaseAuthorizationHandler>();
        services.AddOrgClaimAuthorization();
        return services;
    }
}
