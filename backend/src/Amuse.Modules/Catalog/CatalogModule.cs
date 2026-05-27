using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.GetArtistDetail;
using Amuse.Modules.Catalog.Features.GetTrackDashAsset;
using Amuse.Modules.Catalog.Features.GetReleaseDetail;
using Amuse.Modules.Catalog.Features.GetTrackStreamInfo;
using Amuse.Modules.Catalog.Features.ManageTrackAudio;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_catalog", "catalog");
                    npgsql.MapEnum<ReleaseType>("release_type", "catalog");
                }));

        services.AddScoped<BrowseHomeHandler>();
        services.AddScoped<GetArtistDetailHandler>();
        services.AddScoped<GetReleaseDetailHandler>();
        services.AddScoped<GetTrackStreamInfoHandler>();
        services.AddScoped<GetTrackDashAssetHandler>();
        services.AddScoped<PresignAudioMasterUploadHandler>();
        services.AddScoped<CompleteAudioMasterUploadHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapBrowseHomeEndpoint();
        endpoints.MapGetArtistDetailEndpoint();
        endpoints.MapGetReleaseDetailEndpoint();
        endpoints.MapGetTrackStreamInfoEndpoint();
        endpoints.MapGetTrackDashAssetEndpoint();
        endpoints.MapPresignAudioMasterUploadEndpoint();
        endpoints.MapCompleteAudioMasterUploadEndpoint();
        return endpoints;
    }
}
