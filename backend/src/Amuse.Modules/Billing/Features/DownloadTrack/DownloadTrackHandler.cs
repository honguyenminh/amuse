using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Features.DownloadTrack;

internal sealed class DownloadTrackHandler(
    ICatalogDiscoveryReadModel catalogDiscovery,
    IEntitlementReadModel entitlements,
    IObjectStorage storage,
    IClock clock,
    IOptions<MediaOptions> mediaOptions,
    IListenerPersonaReadModel personaReadModel)
{
    public async Task<Result<TrackDownloadResponse>> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<TrackDownloadResponse>.Failure(BillingErrors.TrackNotFound);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<TrackDownloadResponse>.Failure(listenerResult.Error!);

        var accountId = listenerResult.Value!.AccountId;

        var row = await catalogDiscovery.GetTrackDownloadRowAsync(
            TrackId.From(trackId),
            cancellationToken);
        if (row is null)
            return Result<TrackDownloadResponse>.Failure(BillingErrors.DownloadNotReady);

        var ownsTrack = await entitlements.OwnsTrackAsync(
            accountId,
            trackId,
            row.ReleaseId,
            cancellationToken);
        if (!ownsTrack)
            return Result<TrackDownloadResponse>.Failure(BillingErrors.DownloadForbidden);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = clock.UtcNow.Add(ttl);
        var url = storage.GetSignedUrl(MediaBucket.Audio, row.AudioMasterKey, ttl);
        var contentType = ContentTypeForKey(row.AudioMasterKey);
        var fileName = BuildDownloadFileName(row.Title, row.AudioMasterKey);

        return Result<TrackDownloadResponse>.Success(
            new TrackDownloadResponse(trackId, url, contentType, expiresAt, fileName));
    }

    private static string BuildDownloadFileName(string title, string masterKey)
    {
        var extension = Path.GetExtension(masterKey);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".wav";

        var safeTitle = string.Concat(title.Select(ch =>
            Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
        if (safeTitle.Length == 0)
            safeTitle = "track";

        return $"{safeTitle}{extension}";
    }

    private static string ContentTypeForKey(string key)
    {
        var extension = Path.GetExtension(key).ToLowerInvariant();
        return extension switch
        {
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".mp3" => "audio/mpeg",
            ".m4a" or ".mp4" => "audio/mp4",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream",
        };
    }
}

public sealed record TrackDownloadResponse(
    Guid TrackId,
    string Url,
    string ContentType,
    DateTimeOffset ExpiresAt,
    string FileName);
