using Amuse.Modules.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Common.Persistence;

public static class ModuleDbContextRegistrationExtensions
{
    public static IServiceCollection AddModulePersistenceInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddScoped<IOrgScopeAccessor, OrgScopeAccessor>();
        services.TryAddScoped<AuditingSaveChangesInterceptor>();
        return services;
    }

    public static DbContextOptionsBuilder AddModuleInterceptors(
        this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        options.AddInterceptors(serviceProvider.GetRequiredService<AuditingSaveChangesInterceptor>());
        return options;
    }

    public static DbContextOptionsBuilder<TContext> AddModuleInterceptors<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        IServiceProvider serviceProvider)
        where TContext : DbContext
    {
        ((DbContextOptionsBuilder)options).AddModuleInterceptors(serviceProvider);
        return options;
    }
}
