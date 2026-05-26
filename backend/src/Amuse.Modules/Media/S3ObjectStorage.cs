using Amazon.S3;
using Amazon.S3.Model;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Media;

internal sealed class S3ObjectStorage(
    IAmazonS3 client,
    IOptions<MediaOptions> options) : IObjectStorage
{
    private readonly MediaOptions _options = options.Value;

    public async Task<bool> ObjectExistsAsync(
        MediaBucket bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = BucketName(bucket), Key = key },
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task PutAsync(
        MediaBucket bucket,
        string key,
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        await client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = BucketName(bucket),
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                // Payload signing must remain enabled when talking to plain-HTTP MinIO
                // in dev. The AWS SDK refuses to skip signing on non-HTTPS endpoints.
            },
            cancellationToken);
    }

    public string GetPublicUrl(MediaBucket bucket, string key)
    {
        if (bucket != MediaBucket.Covers)
            throw new InvalidOperationException(
                $"Bucket {bucket} is not configured for anonymous reads; use GetSignedUrl instead.");

        var bucketName = BucketName(bucket);
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return _options.ForcePathStyle
            ? $"{baseUrl}/{bucketName}/{Uri.EscapeDataString(key)}"
            : $"{baseUrl.Replace("://", $"://{bucketName}.", StringComparison.Ordinal)}/{Uri.EscapeDataString(key)}";
    }

    public string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl)
    {
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketName(bucket),
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        });

        // AWSSDK.S3 always emits https:// in presigned URLs even when the endpoint and
        // UseHttp config say otherwise. For local MinIO we need to honour the configured
        // endpoint scheme so the browser can actually fetch the signed URL.
        if (_options.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = string.Concat("http://", url.AsSpan("https://".Length));
        }
        return url;
    }

    private string BucketName(MediaBucket bucket) => bucket switch
    {
        MediaBucket.Covers => _options.CoversBucket,
        MediaBucket.Audio => _options.AudioBucket,
        _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
    };
}
