using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class TranscodeJobQueries
{
    public static Task<bool> HasInflightForTrackAsync(
        CatalogDbContext db,
        TrackId trackId,
        CancellationToken cancellationToken) =>
        db.AudioTranscodeJobs.AnyAsync(
            j => j.TrackId == trackId
                 && (j.Status == AudioTranscodeJobStatus.Queued
                     || j.Status == AudioTranscodeJobStatus.Processing),
            cancellationToken);

    public static Task<AudioTranscodeJob?> GetLatestInflightForTrackAsync(
        CatalogDbContext db,
        TrackId trackId,
        CancellationToken cancellationToken) =>
        db.AudioTranscodeJobs
            .Where(j => j.TrackId == trackId
                        && (j.Status == AudioTranscodeJobStatus.Queued
                            || j.Status == AudioTranscodeJobStatus.Processing))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<AudioTranscodeJob?> GetLatestForTrackAsync(
        CatalogDbContext db,
        TrackId trackId,
        CancellationToken cancellationToken) =>
        db.AudioTranscodeJobs
            .Where(j => j.TrackId == trackId)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<List<AudioTranscodeJob>> GetStaleProcessingJobsAsync(
        CatalogDbContext db,
        DateTimeOffset staleCutoff,
        CancellationToken cancellationToken) =>
        db.AudioTranscodeJobs
            .Where(j => j.Status == AudioTranscodeJobStatus.Processing && j.UpdatedAt < staleCutoff)
            .ToListAsync(cancellationToken);

    internal static TranscodeJobSnapshot ToSnapshot(this AudioTranscodeJob job) =>
        new(
            job.Id,
            (TranscodeJobStatus)(int)job.Status,
            job.UpdatedAt,
            job.MasterKey,
            job.StreamKey,
            job.AttemptCount);
}
