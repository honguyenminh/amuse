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

            options.AddPolicy(OrgPolicies.ReadCatalog, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("read", "catalog")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.WriteDraftCatalog, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("write_draft", "catalog")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.UploadCatalog, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("upload", "catalog")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.PublishCatalog, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("publish_public", "catalog")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManageCatalogPricing, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeSubClaim("manage", "catalog", "pricing")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManagePurchaseRefund, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeSubClaim("manage", "purchase", "refund")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ReadPayout, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeWideClaim("read", "payout")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManagePayoutProfile, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeSubClaim("manage", "payout", "profile")],
                    OrgClaimMatchMode.Any));
            });

            options.AddPolicy(OrgPolicies.ManagePayoutWithdraw, policy =>
            {
                policy.RequireClaim("ctx", "org");
                policy.RequireClaim("org_id");
                policy.Requirements.Add(new OrgClaimRequirement(
                    [OrgClaim.ScopeSubClaim("manage", "payout", "withdraw")],
                    OrgClaimMatchMode.Any));
            });
        });

        return services;
    }
}
