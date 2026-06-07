using Amuse.Modules.Discovery.Features.AddTrackToPlaylist;
using Amuse.Modules.Discovery.Features.CreatePlaylist;
using Amuse.Modules.Discovery.Features.DeletePlaylist;
using Amuse.Modules.Discovery.Features.FollowPlaylist;
using Amuse.Modules.Discovery.Features.ForkPlaylist;
using Amuse.Modules.Discovery.Features.GetLikedPlaylist;
using Amuse.Modules.Discovery.Features.GetLikedPlayableTracks;
using Amuse.Modules.Discovery.Features.GetPlaylist;
using Amuse.Modules.Discovery.Features.GetPlaylistPlayableTracks;
using Amuse.Modules.Discovery.Features.GetReleasePlayableTracks;
using Amuse.Modules.Discovery.Features.LikeTrack;
using Amuse.Modules.Discovery.Features.ListLibraryLiked;
using Amuse.Modules.Discovery.Features.ListLibraryPlaylists;
using Amuse.Modules.Discovery.Features.ListLibraryReleases;
using Amuse.Modules.Discovery.Features.ListMyPlaylists;
using Amuse.Modules.Discovery.Features.RemoveTrackFromPlaylist;
using Amuse.Modules.Discovery.Features.ReorderPlaylistItems;
using Amuse.Modules.Discovery.Features.ReplacePlaylistShares;
using Amuse.Modules.Discovery.Features.SavePlaylist;
using Amuse.Modules.Discovery.Features.SaveRelease;
using Amuse.Modules.Discovery.Features.Search;
using Amuse.Modules.Discovery.Features.UnfollowPlaylist;
using Amuse.Modules.Discovery.Features.UnlikeTrack;
using Amuse.Modules.Discovery.Features.UnsavePlaylist;
using Amuse.Modules.Discovery.Features.UnsaveRelease;
using Amuse.Modules.Discovery.Features.UpdatePlaylist;
using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Discovery;

public static class DiscoveryModule
{
    public static IServiceCollection AddDiscoveryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddModulePersistenceInfrastructure();
        services.TryAddSingleton(_ => new AuditEntityRegistry());

        services.AddDbContext<DiscoveryDbContext>((sp, options) =>
        {
            DiscoveryDbContextOptions.Configure(
                (DbContextOptionsBuilder<DiscoveryDbContext>)options,
                connectionString);
            options.AddModuleInterceptors(sp);
        });

        services.AddScoped<PlaylistViewContextBuilder>();
        services.AddScoped<PlaylistLoader>();
        services.AddScoped<LikedPlaylistLoader>();
        services.AddScoped<PlayableCollectionResolver>();

        services.AddScoped<CreatePlaylistHandler>();
        services.AddScoped<ListMyPlaylistsHandler>();
        services.AddScoped<GetLikedPlaylistHandler>();
        services.AddScoped<GetLikedPlayableTracksHandler>();
        services.AddScoped<GetPlaylistHandler>();
        services.AddScoped<UpdatePlaylistHandler>();
        services.AddScoped<DeletePlaylistHandler>();
        services.AddScoped<AddTrackToPlaylistHandler>();
        services.AddScoped<RemoveTrackFromPlaylistHandler>();
        services.AddScoped<ReorderPlaylistItemsHandler>();
        services.AddScoped<ReplacePlaylistSharesHandler>();
        services.AddScoped<ForkPlaylistHandler>();
        services.AddScoped<FollowPlaylistHandler>();
        services.AddScoped<UnfollowPlaylistHandler>();
        services.AddScoped<SavePlaylistHandler>();
        services.AddScoped<UnsavePlaylistHandler>();
        services.AddScoped<ListLibraryPlaylistsHandler>();
        services.AddScoped<ListLibraryLikedHandler>();
        services.AddScoped<ListLibraryReleasesHandler>();
        services.AddScoped<LikeTrackHandler>();
        services.AddScoped<UnlikeTrackHandler>();
        services.AddScoped<SaveReleaseHandler>();
        services.AddScoped<UnsaveReleaseHandler>();
        services.AddScoped<SearchHandler>();
        services.AddScoped<GetPlaylistPlayableTracksHandler>();
        services.AddScoped<GetReleasePlayableTracksHandler>();

        services.AddValidatorsFromAssemblyContaining<CreatePlaylistRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapDiscoveryModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCreatePlaylistEndpoint();
        endpoints.MapListMyPlaylistsEndpoint();
        endpoints.MapGetLikedPlaylistEndpoint();
        endpoints.MapGetLikedPlayableTracksEndpoint();
        endpoints.MapGetPlaylistEndpoint();
        endpoints.MapUpdatePlaylistEndpoint();
        endpoints.MapDeletePlaylistEndpoint();
        endpoints.MapAddTrackToPlaylistEndpoint();
        endpoints.MapRemoveTrackFromPlaylistEndpoint();
        endpoints.MapReorderPlaylistItemsEndpoint();
        endpoints.MapReplacePlaylistSharesEndpoint();
        endpoints.MapForkPlaylistEndpoint();
        endpoints.MapFollowPlaylistEndpoint();
        endpoints.MapUnfollowPlaylistEndpoint();
        endpoints.MapSavePlaylistEndpoint();
        endpoints.MapUnsavePlaylistEndpoint();
        endpoints.MapListLibraryPlaylistsEndpoint();
        endpoints.MapListLibraryLikedEndpoint();
        endpoints.MapListLibraryReleasesEndpoint();
        endpoints.MapLikeTrackEndpoint();
        endpoints.MapUnlikeTrackEndpoint();
        endpoints.MapSaveReleaseEndpoint();
        endpoints.MapUnsaveReleaseEndpoint();
        endpoints.MapSearchEndpoint();
        endpoints.MapGetPlaylistPlayableTracksEndpoint();
        endpoints.MapGetReleasePlayableTracksEndpoint();
        return endpoints;
    }
}
