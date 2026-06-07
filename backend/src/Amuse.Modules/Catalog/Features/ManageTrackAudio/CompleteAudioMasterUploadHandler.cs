using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Messaging;
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

        var expectedPrefix = $"masters/{trackId}/";
        if (!request.Key.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var typedId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedId, cancellationToken);

        if (track is null)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var scope = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scope.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(scope.Error!);

        var now = clock.UtcNow;

        var intent = await db.AudioMasterUploadIntents
            .FirstOrDefaultAsync(i => i.TrackId == typedId && i.ObjectKey == request.Key, cancellationToken);

        if (intent is null || !intent.IsConsumable(now))
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        if (!await storage.ObjectExistsAsync(MediaBucket.Audio, request.Key, cancellationToken))
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.AudioMasterObjectMissing);

        var inflight = await db.AudioTranscodeJobs.AnyAsync(
            j => j.TrackId == typedId
                && (j.Status == AudioTranscodeJobStatus.Queued || j.Status == AudioTranscodeJobStatus.Processing),
            cancellationToken);
        if (inflight)
            return Result<CompleteAudioMasterUploadResponse>.Failure(CatalogErrors.TranscodeAlreadyInProgress);

        intent.Consume(now);

        if (!string.IsNullOrWhiteSpace(track.AudioStreamKey))
        {
            await CatalogMediaCleanup.DeleteDashPrefixAsync(storage, track.AudioStreamKey, cancellationToken);

            var clearStream = track.ClearAudioStream();
            if (!clearStream.IsSuccess)
                return Result<CompleteAudioMasterUploadResponse>.Failure(clearStream.Error!);

            var clearLoudness = track.ClearLoudnessProfile();
            if (!clearLoudness.IsSuccess)
                return Result<CompleteAudioMasterUploadResponse>.Failure(clearLoudness.Error!);
        }

        track.SetAudioMaster(request.Key);
        var processing = track.MarkProcessing();
        if (!processing.IsSuccess)
            return Result<CompleteAudioMasterUploadResponse>.Failure(processing.Error!);

        var derivedId = Guid.CreateVersion7();
        var streamKey = $"dash/{trackId}/{derivedId}/manifest.mpd";

        var job = AudioTranscodeJob.Enqueue(track.Id, request.Key, streamKey, now);
        db.AudioTranscodeJobs.Add(job);
        db.CatalogOutboxMessages.Add(
            CatalogOutboxMessage.EnqueueAudioTranscode(
                new AudioTranscodeJobMessage(job.Id, track.Id.Value, job.MasterKey, job.StreamKey),
                now));

        await db.SaveChangesAsync(cancellationToken);

        return Result<CompleteAudioMasterUploadResponse>.Success(
            new CompleteAudioMasterUploadResponse(trackId, request.Key, streamKey, job.Id));
    }
}
