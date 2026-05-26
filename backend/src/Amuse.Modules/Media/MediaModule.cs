using Amazon.Runtime;
using Amazon.S3;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Media;

public static class MediaModule
{
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MediaOptions>()
            .Bind(configuration.GetSection(MediaOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<MediaOptions>>().Value;
            var credentials = new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = opts.Endpoint,
                ForcePathStyle = opts.ForcePathStyle,
                // MinIO accepts arbitrary region; pick a benign default so the SDK doesn't probe.
                AuthenticationRegion = "us-east-1",
                // Honour the scheme from Endpoint (plain HTTP for local MinIO).
                // Without this, presigned URLs default to https://, which a local MinIO
                // running on plain HTTP cannot serve.
                UseHttp = opts.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            };
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        return services;
    }
}
