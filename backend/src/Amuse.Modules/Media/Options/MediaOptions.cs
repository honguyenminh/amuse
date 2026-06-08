namespace Amuse.Modules.Media.Options;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>S3 API endpoint (e.g. <c>http://localhost:9000</c> for local MinIO).</summary>
    public string Endpoint { get; set; } = "http://localhost:9000";

    /// <summary>
    /// Base URL used to compose anonymous-read URLs for public covers.
    /// Often identical to <see cref="Endpoint"/> in dev, but may be a CDN/edge host in prod.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:9000";

    /// <summary>
    /// Host used to sign browser-facing presigned GET/PUT URLs (DASH segments, uploads).
    /// When empty, <see cref="PublicBaseUrl"/> is used. Set to the R2 S3 API endpoint when
    /// <see cref="PublicBaseUrl"/> is a CDN/custom domain for public covers only.
    /// </summary>
    public string PresignBaseUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Required for MinIO and most non-AWS S3-compatible stores.</summary>
    public bool ForcePathStyle { get; set; } = true;

    public string CoversBucket { get; set; } = "amuse-covers";
    public string AudioBucket { get; set; } = "amuse-audio";

    /// <summary>TTL of presigned URLs handed out by the stream endpoint.</summary>
    public int SignedUrlMinutes { get; set; } = 30;

    public string ResolvePresignBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(PresignBaseUrl))
            return PresignBaseUrl.Trim();

        return PublicBaseUrl.Trim();
    }
}
