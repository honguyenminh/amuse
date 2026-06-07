using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.CancelScheduleRelease;
using Amuse.Modules.Catalog.Features.GetArtistDetail;
using Amuse.Modules.Catalog.Features.GetReleaseDetail;
using Amuse.Modules.Catalog.Features.GetReleaseGroupDetail;
using Amuse.Modules.Catalog.Features.GetResourceAudit;
using Amuse.Modules.Catalog.Features.GetTrackDashAsset;
using Amuse.Modules.Catalog.Features.GetTrackIngestion;
using Amuse.Modules.Catalog.Features.GetTrackStreamInfo;
using Amuse.Modules.Catalog.Features.HideRelease;
using Amuse.Modules.Catalog.Features.ManageArtistAvatar;
using Amuse.Modules.Catalog.Features.ManageArtistCover;
using Amuse.Modules.Catalog.Features.ManageArtists;
using Amuse.Modules.Catalog.Features.ManageReleaseCover;
using Amuse.Modules.Catalog.Features.ManageReleaseGroups;
using Amuse.Modules.Catalog.Features.ManageReleases;
using Amuse.Modules.Catalog.Features.ManageTrackAudio;
using Amuse.Modules.Catalog.Features.ManageTracks;
using Amuse.Modules.Catalog.Features.PublishRelease;
using Amuse.Modules.Catalog.Features.RetryTrackTranscode;
using Amuse.Modules.Catalog.Features.ScheduleRelease;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Catalog.Services;
using Amuse.Modules.Common.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddModulePersistenceInfrastructure();
        services.TryAddSingleton(_ => new AuditEntityRegistry());

        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            CatalogDbContextOptions.Configure(
                (DbContextOptionsBuilder<CatalogDbContext>)options,
                connectionString);
            options.AddModuleInterceptors(sp);
        });

        services.AddScoped<ICatalogOrganizationBootstrap, CatalogOrganizationBootstrap>();
        services.AddScoped<ICatalogManagedArtistVisibility, CatalogManagedArtistVisibility>();
        services.AddScoped<ICatalogDiscoveryReadModel, CatalogDiscoveryReadModel>();
        services.AddScoped<CatalogAuditWriter>();

        services.AddScoped<BrowseHomeHandler>();
        services.AddScoped<GetArtistDetailHandler>();
        services.AddScoped<GetReleaseDetailHandler>();
        services.AddScoped<GetTrackStreamInfoHandler>();
        services.AddScoped<GetTrackDashAssetHandler>();
        services.AddScoped<PresignAudioMasterUploadHandler>();
        services.AddScoped<CompleteAudioMasterUploadHandler>();

        services.AddScoped<CreateReleaseGroupHandler>();
        services.AddScoped<ListReleaseGroupsHandler>();
        services.AddScoped<GetReleaseGroupDetailHandler>();
        services.AddScoped<GetPublicReleaseGroupDetailHandler>();
        services.AddScoped<UpdateReleaseGroupHandler>();

        services.AddScoped<CreateArtistHandler>();
        services.AddScoped<CheckArtistSlugAvailabilityHandler>();
        services.AddScoped<ListArtistsHandler>();
        services.AddScoped<GetArtistHandler>();
        services.AddScoped<UpdateArtistHandler>();

        services.AddScoped<CreateReleaseHandler>();
        services.AddScoped<CheckReleaseSlugAvailabilityHandler>();
        services.AddScoped<ListReleasesHandler>();
        services.AddScoped<GetReleaseHandler>();
        services.AddScoped<UpdateReleaseHandler>();
        services.AddScoped<DeleteReleaseHandler>();

        services.AddScoped<CreateTrackHandler>();
        services.AddScoped<UpdateTrackHandler>();
        services.AddScoped<DeleteTrackHandler>();

        services.AddScoped<ScheduledReleaseClaimService>();

        services.AddScoped<PublishReleaseHandler>();
        services.AddScoped<ScheduleReleaseHandler>();
        services.AddScoped<CancelScheduleReleaseHandler>();
        services.AddScoped<HideReleaseHandler>();
        services.AddScoped<GetTrackIngestionHandler>();
        services.AddScoped<RetryTrackTranscodeHandler>();

        services.AddScoped<PresignReleaseCoverUploadHandler>();
        services.AddScoped<CompleteReleaseCoverUploadHandler>();
        services.AddScoped<PresignArtistAvatarUploadHandler>();
        services.AddScoped<CompleteArtistAvatarUploadHandler>();
        services.AddScoped<PresignArtistCoverUploadHandler>();
        services.AddScoped<CompleteArtistCoverUploadHandler>();

        services.AddScoped<ListResourceAuditsHandler>();

        return services;
    }

    /// <summary>
    /// Outbox dispatch and stale-job recovery run in the API process only (requires
    /// <see cref="Processing.IAudioTranscodeJobQueue"/>). Worker hosts must not call this.
    /// </summary>
    public static IServiceCollection AddCatalogTranscodeRecoveryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TranscodeJobRecoveryOptions>(
            configuration.GetSection("Catalog:TranscodeRecovery"));
        services.AddHostedService<CatalogOutboxProcessor>();
        services.AddHostedService<TranscodeJobStaleSweeper>();
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

        endpoints.MapManageReleaseGroupsEndpoint();
        endpoints.MapGetPublicReleaseGroupDetailEndpoint();
        endpoints.MapManageArtistsEndpoint();
        endpoints.MapManageReleasesEndpoint();
        endpoints.MapManageTracksEndpoint();
        endpoints.MapPublishReleaseEndpoint();
        endpoints.MapScheduleReleaseEndpoint();
        endpoints.MapCancelScheduleReleaseEndpoint();
        endpoints.MapHideReleaseEndpoint();
        endpoints.MapGetTrackIngestionEndpoint();
        endpoints.MapRetryTrackTranscodeEndpoint();
        endpoints.MapManageReleaseCoverEndpoint();
        endpoints.MapManageArtistAvatarEndpoint();
        endpoints.MapManageArtistCoverEndpoint();
        endpoints.MapGetResourceAuditEndpoint();

        return endpoints;
    }
}
