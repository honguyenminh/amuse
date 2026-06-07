using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.AcceptInvite;
using Amuse.Modules.Tenancy.Features.CreateInvite;
using Amuse.Modules.Tenancy.Features.CreateOrganization;
using Amuse.Modules.Tenancy.Features.DeclineInvite;
using Amuse.Modules.Tenancy.Features.DeleteOrganization;
using Amuse.Modules.Tenancy.Features.GetInvitePreview;
using Amuse.Modules.Tenancy.Features.GetOrganization;
using Amuse.Modules.Tenancy.Features.GetPortalProfile;
using Amuse.Modules.Tenancy.Features.LeaveOrganization;
using Amuse.Modules.Tenancy.Features.ListClaimPresets;
using Amuse.Modules.Tenancy.Features.ListInvites;
using Amuse.Modules.Tenancy.Features.ListMembers;
using Amuse.Modules.Tenancy.Features.ListMyOrganizations;
using Amuse.Modules.Tenancy.Features.ListOrganizationAudit;
using Amuse.Modules.Tenancy.Features.ManagePortalAvatar;
using Amuse.Modules.Tenancy.Features.RemoveMember;
using Amuse.Modules.Tenancy.Features.RevokeInvite;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Features.TransferOwnership;
using Amuse.Modules.Tenancy.Features.UpdateMember;
using Amuse.Modules.Tenancy.Features.UpdateOrganization;
using Amuse.Modules.Tenancy.Features.UpdatePortalProfile;
using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Tenancy.Options;
using Amuse.Modules.Tenancy.Persistence;
using Amuse.Modules.Tenancy.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Tenancy;

public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));

        services.AddModulePersistenceInfrastructure();
        services.TryAddSingleton(_ => new AuditEntityRegistry());

        services.AddDbContext<TenancyDbContext>((sp, options) =>
        {
            TenancyDbContextOptions.Configure(options, connectionString);
            options.AddModuleInterceptors(sp);
        });

        services.TryAddSingleton<IClock, SystemClock>();

        services.AddScoped<ITenancyPersonaReadModel, TenancyPersonaReadModel>();
        services.AddScoped<ITenancyOrganizationReadModel, TenancyOrganizationReadModel>();
        services.AddScoped<IOrganizationLifecycleCommands, OrganizationLifecycleService>();
        services.AddScoped<IBusinessPortalProfileLookup, BusinessPortalProfileLookup>();
        services.AddScoped<IBusinessPortalProfileOnboardingReadModel, BusinessPortalProfileOnboardingReadModel>();
        services.AddScoped<BusinessPortalProfileService>();
        services.AddScoped<GetPortalProfileHandler>();
        services.AddScoped<UpdatePortalProfileHandler>();
        services.AddScoped<PresignPortalAvatarUploadHandler>();
        services.AddScoped<CompletePortalAvatarUploadHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
        services.AddScoped<CreateOrganizationHandler>();
        services.AddScoped<ListMyOrganizationsHandler>();
        services.AddScoped<GetOrganizationHandler>();
        services.AddScoped<UpdateOrganizationHandler>();
        services.AddScoped<ListOrganizationAuditsHandler>();
        services.AddScoped<TenancyAuditWriter>();
        services.AddScoped<ListMembersHandler>();
        services.AddScoped<CreateInviteHandler>();
        services.AddScoped<ListInvitesHandler>();
        services.AddScoped<RevokeInviteHandler>();
        services.AddScoped<UpdateMemberHandler>();
        services.AddScoped<RemoveMemberHandler>();
        services.AddScoped<LeaveOrganizationHandler>();
        services.AddScoped<TransferOwnershipHandler>();
        services.AddScoped<DeleteOrganizationHandler>();
        services.AddScoped<GetInvitePreviewHandler>();
        services.AddScoped<AcceptInviteHandler>();
        services.AddScoped<DeclineInviteHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapTenancyModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenancy");
        group.MapListClaimPresetsEndpoint();
        group.MapCreateOrganizationEndpoint();
        group.MapListMyOrganizationsEndpoint();
        group.MapGetOrganizationEndpoint();
        group.MapUpdateOrganizationEndpoint();
        group.MapListOrganizationAuditsEndpoint();
        group.MapListMembersEndpoint();
        group.MapCreateInviteEndpoint();
        group.MapListInvitesEndpoint();
        group.MapRevokeInviteEndpoint();
        group.MapUpdateMemberEndpoint();
        group.MapRemoveMemberEndpoint();
        group.MapLeaveOrganizationEndpoint();
        group.MapTransferOwnershipEndpoint();
        group.MapDeleteOrganizationEndpoint();
        group.MapGetInvitePreviewEndpoint();
        group.MapAcceptInviteEndpoint();
        group.MapDeclineInviteEndpoint();
        group.MapGetPortalProfileEndpoint();
        group.MapUpdatePortalProfileEndpoint();
        group.MapPortalAvatarEndpoint();
        return endpoints;
    }
}
