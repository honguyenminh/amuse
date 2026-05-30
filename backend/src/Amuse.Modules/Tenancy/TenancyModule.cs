using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.CreateOrganization;
using Amuse.Modules.Tenancy.Features.GetOrganization;
using Amuse.Modules.Tenancy.Features.ListMyOrganizations;
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

        services.AddDbContext<TenancyDbContext>(options =>
            TenancyDbContextOptions.Configure(options, connectionString));

        services.TryAddSingleton<IClock, SystemClock>();

        services.AddScoped<ITenancyPersonaReadModel, TenancyPersonaReadModel>();
        services.AddScoped<IOrganizationLifecycleCommands, OrganizationLifecycleService>();

        services.AddValidatorsFromAssemblyContaining<CreateOrganizationRequestValidator>();
        services.AddScoped<CreateOrganizationHandler>();
        services.AddScoped<ListMyOrganizationsHandler>();
        services.AddScoped<GetOrganizationHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapTenancyModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tenancy");
        group.MapCreateOrganizationEndpoint();
        group.MapListMyOrganizationsEndpoint();
        group.MapGetOrganizationEndpoint();
        return endpoints;
    }
}
