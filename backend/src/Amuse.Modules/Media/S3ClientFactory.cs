using Amazon.Runtime;
using Amazon.S3;
using Amuse.Modules.Media.Options;

namespace Amuse.Modules.Media;

internal static class S3ClientFactory
{
    public static AmazonS3Client Create(MediaOptions options, string serviceUrl)
    {
        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        var useHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = "us-east-1",
            UseHttp = useHttp,
        };
        return new AmazonS3Client(credentials, config);
    }
}
