using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManageTrackAudio;

public sealed record CompleteAudioMasterUploadRequest(string Key, int DurationMs);

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
        RuleFor(x => x.DurationMs).GreaterThan(0);
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
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(orgResult.Error!);

        if (string.IsNullOrWhiteSpace(request.Key))
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var typedId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedId, cancellationToken);

        if (track is null)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var scope = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scope.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(scope.Error!);

        var masterExists = await storage.ObjectExistsAsync(MediaBucket.Audio, request.Key, cancellationToken);
        if (!masterExists)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.AudioMasterObjectMissing);

        TrackDuration duration;
        try
        {
            duration = TrackDuration.FromMilliseconds(request.DurationMs);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidTrack);
        }

        var setDurationResult = track.SetDurationFromUploadedAudio(duration);
        if (!setDurationResult.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(setDurationResult.Error!);

        track.SetAudioMaster(request.Key);
        var processing = track.MarkProcessing();
        if (!processing.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(processing.Error!);

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
