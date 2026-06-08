using Amazon.S3;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Media;

public static class MediaModule
{
    public const string InternalS3Client = "media.s3.internal";
    public const string PresignS3Client = "media.s3.presign";

    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MediaOptions>()
            .Bind(configuration.GetSection(MediaOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
            sp.GetRequiredKeyedService<IAmazonS3>(InternalS3Client));

        services.AddKeyedSingleton<IAmazonS3>(InternalS3Client, (sp, _) =>
        {
            var opts = sp.GetRequiredService<IOptions<MediaOptions>>().Value;
            return S3ClientFactory.Create(opts, opts.Endpoint);
        });

        services.AddKeyedSingleton<IAmazonS3>(PresignS3Client, (sp, _) =>
        {
            var opts = sp.GetRequiredService<IOptions<MediaOptions>>().Value;
            // Presigned URLs must be signed for a browser-reachable S3 host (dev API proxy,
            // R2 S3 API endpoint on stage). Covers may use a separate CDN host via PublicBaseUrl.
            return S3ClientFactory.Create(opts, opts.ResolvePresignBaseUrl());
        });

        services.AddSingleton<IObjectStorage>(sp =>
        {
            var internalClient = sp.GetRequiredKeyedService<IAmazonS3>(InternalS3Client);
            var presignClient = sp.GetRequiredKeyedService<IAmazonS3>(PresignS3Client);
            var options = sp.GetRequiredService<IOptions<MediaOptions>>();
            return new S3ObjectStorage(internalClient, presignClient, options);
        });

        services.AddSingleton<IMediaPublicUrlBuilder, MediaPublicUrlBuilder>();

        return services;
    }
}
