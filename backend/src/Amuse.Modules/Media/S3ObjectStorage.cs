using Amazon.S3;
using Amazon.S3.Model;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Media;

internal sealed class S3ObjectStorage(
    IAmazonS3 internalClient,
    IAmazonS3 presignClient,
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
            await internalClient.GetObjectMetadataAsync(
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
        await internalClient.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = BucketName(bucket),
                Key = key,
                InputStream = stream,
                ContentType = contentType,
            },
            cancellationToken);
    }

    public async Task<StoredObject?> GetAsync(
        MediaBucket bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await internalClient.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = BucketName(bucket),
                    Key = key
                },
                cancellationToken);

            await using var stream = response.ResponseStream;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;

            return new StoredObject(ms.ToArray(), contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
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

    public string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) =>
        GetPreSignedGetUrl(presignClient, bucket, key, ttl, _options.ResolvePresignBaseUrl());

    public string GetInternalSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) =>
        GetPreSignedGetUrl(internalClient, bucket, key, ttl, _options.Endpoint);

    private string GetPreSignedGetUrl(
        IAmazonS3 client,
        MediaBucket bucket,
        string key,
        TimeSpan ttl,
        string serviceBaseUrl)
    {
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketName(bucket),
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        });

        return RewriteToHttpIfNeeded(url, serviceBaseUrl);
    }

    public string GetSignedUploadUrl(MediaBucket bucket, string key, TimeSpan ttl, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required for upload URLs.", nameof(contentType));

        var url = presignClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketName(bucket),
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType,
        });

        return RewriteToHttpIfNeeded(url, _options.ResolvePresignBaseUrl());
    }

    public async Task DeleteAsync(
        MediaBucket bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            await internalClient.DeleteObjectAsync(
                new DeleteObjectRequest
                {
                    BucketName = BucketName(bucket),
                    Key = key,
                },
                cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
    }

    public async Task DeleteByPrefixAsync(
        MediaBucket bucket,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var keys = await ListByPrefixAsync(bucket, prefix, cancellationToken);
        if (keys.Count == 0)
            return;

        var bucketName = BucketName(bucket);
        await internalClient.DeleteObjectsAsync(
            new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = keys.Select(key => new KeyVersion { Key = key }).ToList(),
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListByPrefixAsync(
        MediaBucket bucket,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return [];

        var bucketName = BucketName(bucket);
        var results = new List<string>();
        string? continuationToken = null;

        do
        {
            var listing = await internalClient.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    ContinuationToken = continuationToken,
                },
                cancellationToken);

            results.AddRange(
                (listing.S3Objects ?? [])
                    .Select(obj => obj.Key)
                    .Where(key => !string.IsNullOrWhiteSpace(key))!);

            continuationToken = listing.IsTruncated == true ? listing.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return results;
    }

    private string BucketName(MediaBucket bucket) => bucket switch
    {
        MediaBucket.Covers => _options.CoversBucket,
        MediaBucket.Audio => _options.AudioBucket,
        _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
    };

    private static string RewriteToHttpIfNeeded(string url, string serviceBaseUrl)
    {
        var presignBase = serviceBaseUrl.TrimEnd('/');
        if (presignBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat("http://", url.AsSpan("https://".Length));
        }

        return url;
    }
}
