using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManageTrackAudio;

public sealed record CompleteAudioMasterUploadRequest(string Key);

public sealed record CompleteAudioMasterUploadResponse(
    Guid TrackId,
    string MasterKey,
    string StreamKey,
    Guid JobId);

internal sealed class CompleteAudioMasterUploadRequestValidator : AbstractValidator<CompleteAudioMasterUploadRequest>
{
    public CompleteAudioMasterUploadRequestValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(Track.MaxKeyLength);
    }
}

internal sealed class CompleteAudioMasterUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IAudioTranscodeJobQueue jobQueue,
    IClock clock)
{
    public async Task<Result<CompleteAudioMasterUploadResponse>> HandleAsync(
        Guid trackId,
        CompleteAudioMasterUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrWhiteSpace(request.Key))
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var typedId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedId, cancellationToken);

        if (track is null)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var masterExists = await storage.ObjectExistsAsync(MediaBucket.Audio, request.Key, cancellationToken);
        if (!masterExists)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.AudioMasterObjectMissing);

        track.SetAudioMaster(request.Key);

        // DASH-first: worker will package into an MPD + segments under this key prefix.
        var derivedId = Guid.CreateVersion7();
        var streamKey = $"dash/{trackId}/{derivedId}/manifest.mpd";

        var now = clock.UtcNow;
        var job = AudioTranscodeJob.Enqueue(track.Id, request.Key, streamKey, now);
        db.AudioTranscodeJobs.Add(job);

        await db.SaveChangesAsync(cancellationToken);

        await jobQueue.PublishAsync(
            new AudioTranscodeJobMessage(job.Id, track.Id.Value, job.MasterKey, job.StreamKey),
            cancellationToken);

        return Result<CompleteAudioMasterUploadResponse>.Success(
            new CompleteAudioMasterUploadResponse(trackId, request.Key, streamKey, job.Id));
    }
}

