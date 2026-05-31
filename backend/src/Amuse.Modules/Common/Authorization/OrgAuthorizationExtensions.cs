using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Common.Authorization;

public static class OrgAuthorizationExtensions
{
    public static IServiceCollection AddOrgClaimAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationHandler, OrgClaimAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(OrgPolicies.ReadMembership, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("read", "membership")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManageMembership, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("manage", "membership")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManageMemberPermissions, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.MemberPermissionsClaim],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ReadOrg, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("read", "org")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManageOrg, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("manage", "org")],
                    OrgClaimMatchMode.Any));
            });
        });

        return services;
    }
}
