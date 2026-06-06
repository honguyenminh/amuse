using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Listener.Features.EnsureListenerProfile;
using Amuse.Modules.Listener.Features.GetListenerProfile;
using Amuse.Modules.Listener.Features.ManageAvatar;
using Amuse.Modules.Listener.Features.UpdateListenerProfile;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Listener.Services;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Listener;

public static class ListenerModule
{
    public static IServiceCollection AddListenerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ListenerDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_listener", "listener")));

        services.AddScoped<IListenerPersonaReadModel, ListenerPersonaReadModel>();
        services.AddScoped<IListenerOnboardingStatusReadModel, ListenerOnboardingStatusReadModel>();
        services.AddScoped<IListenerPreferenceReadModel, ListenerPreferenceReadModel>();
        services.AddScoped<IListenerProfilePresentationReadModel, ListenerProfilePresentationReadModel>();
        services.AddScoped<EnsureListenerProfileService>();
        services.AddScoped<ListenerProfileService>();
        services.AddScoped<EnsureListenerProfileHandler>();
        services.AddScoped<GetListenerProfileHandler>();
        services.AddScoped<UpdateListenerProfileHandler>();
        services.AddScoped<PresignListenerAvatarUploadHandler>();
        services.AddScoped<CompleteListenerAvatarUploadHandler>();
        services.AddValidatorsFromAssemblyContaining<UpdateListenerProfileRequestValidator>();
        return services;
    }

    public static IEndpointRouteBuilder MapListenerModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapEnsureListenerProfileEndpoint();
        endpoints.MapGetListenerProfileEndpoint();
        endpoints.MapUpdateListenerProfileEndpoint();
        endpoints.MapListenerAvatarEndpoint();
        return endpoints;
    }
}
