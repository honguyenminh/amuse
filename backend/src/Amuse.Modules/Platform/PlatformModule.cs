using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Platform.Features.ApproveOrganization;
using Amuse.Modules.Platform.Features.ForceTransferOwnership;
using Amuse.Modules.Platform.Features.ListClosedOrganizations;
using Amuse.Modules.Platform.Features.ListOrganizationApplications;
using Amuse.Modules.Platform.Features.RecoverOrganization;
using Amuse.Modules.Platform.Features.RejectOrganization;
using Amuse.Modules.Platform.Options;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Platform;

public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformRootOptions>(configuration.GetSection(PlatformRootOptions.SectionName));

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_platform", "platform")));

        services.AddScoped<IPlatformPersonaReadModel, PlatformPersonaReadModel>();
        services.AddScoped<IPlatformOperatorLookup, PlatformOperatorLookup>();

        services.AddValidatorsFromAssemblyContaining<RejectOrganizationRequestValidator>();
        services.AddScoped<ListOrganizationApplicationsHandler>();
        services.AddScoped<ListClosedOrganizationsHandler>();
        services.AddScoped<ApproveOrganizationHandler>();
        services.AddScoped<RejectOrganizationHandler>();
        services.AddScoped<ForceTransferOwnershipHandler>();
        services.AddScoped<RecoverOrganizationHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapPlatformModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform");
        group.MapListOrganizationApplicationsEndpoint();
        group.MapListClosedOrganizationsEndpoint();
        group.MapApproveOrganizationEndpoint();
        group.MapRejectOrganizationEndpoint();
        group.MapForceTransferOwnershipEndpoint();
        group.MapRecoverOrganizationEndpoint();
        return endpoints;
    }
}
