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
using Amuse.Modules.Tenancy.Features.ListClaimPresets;
using Amuse.Modules.Tenancy.Features.ListInvites;
using Amuse.Modules.Tenancy.Features.ListMembers;
using Amuse.Modules.Tenancy.Features.ListMyOrganizations;
using Amuse.Modules.Tenancy.Features.RemoveMember;
using Amuse.Modules.Tenancy.Features.RevokeInvite;
using Amuse.Modules.Tenancy.Features.TransferOwnership;
using Amuse.Modules.Tenancy.Features.UpdateMember;
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

        services.AddDbContext<TenancyDbContext>(options =>
            TenancyDbContextOptions.Configure(options, connectionString));

        services.TryAddSingleton<IClock, SystemClock>();

        services.AddScoped<ITenancyPersonaReadModel, TenancyPersonaReadModel>();
        services.AddScoped<IOrganizationLifecycleCommands, OrganizationLifecycleService>();

        services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
        services.AddScoped<CreateOrganizationHandler>();
        services.AddScoped<ListMyOrganizationsHandler>();
        services.AddScoped<GetOrganizationHandler>();
        services.AddScoped<ListMembersHandler>();
        services.AddScoped<CreateInviteHandler>();
        services.AddScoped<ListInvitesHandler>();
        services.AddScoped<RevokeInviteHandler>();
        services.AddScoped<UpdateMemberHandler>();
        services.AddScoped<RemoveMemberHandler>();
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
        group.MapListMembersEndpoint();
        group.MapCreateInviteEndpoint();
        group.MapListInvitesEndpoint();
        group.MapRevokeInviteEndpoint();
        group.MapUpdateMemberEndpoint();
        group.MapRemoveMemberEndpoint();
        group.MapTransferOwnershipEndpoint();
        group.MapDeleteOrganizationEndpoint();
        group.MapGetInvitePreviewEndpoint();
        group.MapAcceptInviteEndpoint();
        group.MapDeclineInviteEndpoint();
        return endpoints;
    }
}
