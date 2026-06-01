using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class CatalogMediaCleanup
{
    internal static async Task DeleteTrackMediaAsync(
        CatalogDbContext db,
        IObjectStorage storage,
        Track track,
        CancellationToken cancellationToken)
    {
        var audioKeys = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(track.AudioMasterKey))
            audioKeys.Add(track.AudioMasterKey);

        if (!string.IsNullOrWhiteSpace(track.AudioStreamKey))
            await storage.DeleteByPrefixAsync(
                MediaBucket.Audio,
                GetDashPrefix(track.AudioStreamKey),
                cancellationToken);

        var jobs = await db.AudioTranscodeJobs
            .Where(j => j.TrackId == track.Id)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            if (!string.IsNullOrWhiteSpace(job.MasterKey))
                audioKeys.Add(job.MasterKey);

            if (!string.IsNullOrWhiteSpace(job.StreamKey))
                await storage.DeleteByPrefixAsync(
                    MediaBucket.Audio,
                    GetDashPrefix(job.StreamKey),
                    cancellationToken);
        }

        foreach (var key in audioKeys)
            await storage.DeleteAsync(MediaBucket.Audio, key, cancellationToken);

        if (jobs.Count > 0)
            db.AudioTranscodeJobs.RemoveRange(jobs);
    }

    internal static async Task DeleteReleaseCoverAsync(
        IObjectStorage storage,
        string? coverArtKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(coverArtKey))
            return;

        await storage.DeleteAsync(MediaBucket.Covers, coverArtKey, cancellationToken);
    }

    internal static async Task DeleteReleaseMediaAsync(
        CatalogDbContext db,
        IObjectStorage storage,
        Release release,
        CancellationToken cancellationToken)
    {
        foreach (var track in release.Tracks)
            await DeleteTrackMediaAsync(db, storage, track, cancellationToken);

        await DeleteReleaseCoverAsync(storage, release.CoverArtKey, cancellationToken);
    }

    private static string GetDashPrefix(string streamKey)
    {
        var lastSlash = streamKey.LastIndexOf('/');
        return lastSlash >= 0 ? streamKey[..(lastSlash + 1)] : streamKey;
    }
}
