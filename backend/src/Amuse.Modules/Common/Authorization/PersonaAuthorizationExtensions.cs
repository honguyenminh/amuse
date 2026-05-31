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
        });

        services.AddSingleton<IAuthorizationHandler, PlatformOrganizationReviewHandler>();
        services.AddSingleton<IAuthorizationHandler, PlatformOrganizationManageHandler>();
        services.AddOrgClaimAuthorization();
        return services;
    }
}
