using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Amuse.Worker.Transcoder;

internal sealed partial class TranscodingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TranscodingWorker> logger,
    ILoggerFactory loggerFactory,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<MediaOptions> mediaOptions) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);
    private LoudnormAnalyzer? _loudnormAnalyzer;
    private AudioProbe? _audioProbe;

    private LoudnormAnalyzer LoudnormAnalyzer =>
        _loudnormAnalyzer ??= new LoudnormAnalyzer(
            (arguments, trackId, jobId, ct) => RunFfmpegAsync(arguments, trackId, jobId, ct),
            loggerFactory.CreateLogger<LoudnormAnalyzer>());

    private AudioProbe AudioProbe =>
        _audioProbe ??= new AudioProbe(
            (arguments, trackId, jobId, ct) => RunFfprobeAsync(arguments, trackId, jobId, ct));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbit = rabbitOptions.Value;

        LogWorkerStarting(rabbit.HostName, rabbit.Port, rabbit.QueueName, rabbit.PrefetchCount);

        var factory = new ConnectionFactory
        {
            HostName = rabbit.HostName,
            Port = rabbit.Port,
            UserName = rabbit.UserName,
            Password = rabbit.Password,
        };

        var connection = await ConnectWithRetryAsync(factory, rabbit, stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: rabbit.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, rabbit.PrefetchCount, global: false, cancellationToken: stoppingToken);

        LogWorkerConsuming(rabbit.QueueName);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            IServiceScope? scope = null;
            CatalogDbContext? db = null;
            AudioTranscodeJob? job = null;
            var jobStopwatch = Stopwatch.StartNew();

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<AudioTranscodeJobMessage>(json, SerializerOptions);
                if (msg is null)
                {
                    LogInvalidPayload(ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                LogMessageReceived(msg.JobId, msg.TrackId, ea.DeliveryTag);

                scope = scopeFactory.CreateScope();
                db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

                job = await db.AudioTranscodeJobs
                    .FirstOrDefaultAsync(x => x.Id == msg.JobId, stoppingToken);

                if (job is null)
                {
                    LogJobNotFound(msg.JobId, ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                if (job.Status == AudioTranscodeJobStatus.Succeeded)
                {
                    LogJobAlreadySucceeded(job.Id, job.TrackId.Value, ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                job.MarkProcessing(now);
                await db.SaveChangesAsync(stoppingToken);

                LogJobProcessing(
                    job.Id,
                    job.TrackId.Value,
                    job.AttemptCount,
                    job.MasterKey,
                    job.StreamKey);

                var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
                var masterInputUrl = storage.GetInternalSignedUrl(MediaBucket.Audio, job.MasterKey, ttl);

                var track = await db.Tracks
                    .FirstOrDefaultAsync(t => t.Id == job.TrackId, stoppingToken)
                    ?? throw new InvalidOperationException($"Track {job.TrackId.Value} not found for transcode job {job.Id}.");

                var probedDuration = await AudioProbe.ProbeDurationAsync(
                    masterInputUrl,
                    job.TrackId.Value,
                    job.Id,
                    stoppingToken);
                var setDurationResult = track.SetDurationFromUploadedAudio(probedDuration);
                if (!setDurationResult.IsSuccess)
                    throw new InvalidOperationException(setDurationResult.Error!.Message);

                TrackLoudnessProfile loudnessProfile;
                if (track.LoudnessProfile is not null)
                {
                    loudnessProfile = track.LoudnessProfile;
                }
                else
                {
                    loudnessProfile = await LoudnormAnalyzer.AnalyzeAsync(
                        masterInputUrl,
                        job.TrackId.Value,
                        job.Id,
                        now,
                        stoppingToken);
                }

                var alreadyReady = await DashPackageVerifier.IsCompleteAsync(storage, job.StreamKey, stoppingToken);
                if (!alreadyReady)
                {
                    if (await storage.ObjectExistsAsync(MediaBucket.Audio, job.StreamKey, stoppingToken))
                    {
                        var dashPrefix = job.StreamKey[..(job.StreamKey.LastIndexOf('/') + 1)];
                        await storage.DeleteByPrefixAsync(MediaBucket.Audio, dashPrefix, stoppingToken);
                    }

                    await TranscodeMasterToDashAsync(
                        storage,
                        db,
                        job.MasterKey,
                        masterInputUrl,
                        job.StreamKey,
                        job.TrackId,
                        job.Id,
                        stoppingToken);
                }
                else
                {
                    LogDashManifestPresent(job.StreamKey, job.Id);
                    await EnsureRenditionsFromManifestAsync(
                        storage,
                        db,
                        job.StreamKey,
                        job.TrackId,
                        stoppingToken);
                }

                var setLoudnessResult = track.SetLoudnessProfile(loudnessProfile);
                if (!setLoudnessResult.IsSuccess)
                    throw new InvalidOperationException(setLoudnessResult.Error!.Message);

                track.SetAudioStream(job.StreamKey);
                var readyResult = track.MarkReady();
                if (!readyResult.IsSuccess)
                    throw new InvalidOperationException(readyResult.Error!.Message);

                job.MarkSucceeded(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(stoppingToken);

                LogJobSucceeded(
                    job.Id,
                    job.TrackId.Value,
                    jobStopwatch.ElapsedMilliseconds,
                    job.StreamKey);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                LogJobFailed(
                    ex,
                    job?.Id,
                    job?.TrackId.Value,
                    jobStopwatch.ElapsedMilliseconds,
                    job?.AttemptCount);
                if (db is not null && job is not null)
                {
                    try
                    {
                        job.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
                        await db.SaveChangesAsync(stoppingToken);
                        LogJobFailurePersisted(job.Id, job.TrackId.Value, job.LastError);
                    }
                    catch (Exception persistEx)
                    {
                        LogJobFailurePersistError(persistEx, job.Id);
                    }
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            finally
            {
                scope?.Dispose();
            }
        };

        await channel.BasicConsumeAsync(
            queue: rabbit.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        var tcs = new TaskCompletionSource();
        stoppingToken.Register(() =>
        {
            LogWorkerShutdownRequested();
            try { channel.CloseAsync().GetAwaiter().GetResult(); } catch { }
            try { connection.CloseAsync().GetAwaiter().GetResult(); } catch { }
            tcs.TrySetResult();
        });

        await tcs.Task;
    }

    private async Task TranscodeMasterToDashAsync(
        IObjectStorage storage,
        CatalogDbContext db,
        string audioMasterKey,
        string inputUrl,
        string dashManifestKey,
        TrackId trackId,
        Guid jobId,
        CancellationToken ct)
    {
        var transcodeStopwatch = Stopwatch.StartNew();
        var manifestId = Guid.Parse(ExtractManifestId(dashManifestKey));

        LogDashPackagingStarting(jobId, trackId.Value, audioMasterKey, dashManifestKey);

        var outDir = Path.Combine(
            Path.GetTempPath(),
            "amuse-dash",
            trackId.Value.ToString(),
            manifestId.ToString());
        Directory.CreateDirectory(outDir);

        var manifestLocalName = Path.GetFileName(dashManifestKey);
        var manifestPath = Path.Combine(outDir, manifestLocalName);

        foreach (var file in Directory.EnumerateFiles(outDir))
            File.Delete(file);

        LogDashWorkDirectory(jobId, trackId.Value, outDir);

        var args =
            $"-hide_banner -loglevel error -y -nostdin -i \"{inputUrl}\" -vn " +
            "-map 0:a:0 -c:a:0 flac " +
            "-map 0:a:0 -c:a:1 libopus -b:a:1 64k -ar 48000 " +
            "-map 0:a:0 -c:a:2 libopus -b:a:2 128k -ar 48000 " +
            "-map 0:a:0 -c:a:3 libopus -b:a:3 256k -ar 48000 " +
            "-map 0:a:0 -c:a:4 aac -b:a:4 96k -ar 48000 " +
            "-map 0:a:0 -c:a:5 aac -b:a:5 128k -ar 48000 " +
            "-map 0:a:0 -c:a:6 aac -b:a:6 256k -ar 48000 " +
            "-f dash -seg_duration 4 -use_timeline 1 -use_template 1 " +
            "-init_seg_name init-stream$RepresentationID$.m4s " +
            "-media_seg_name chunk-stream$RepresentationID$-$Number%05d$.m4s " +
            "-adaptation_sets \"id=0,streams=0 id=1,streams=1,2,3 id=2,streams=4,5,6\" " +
            $"\"{manifestPath}\"";

        await RunFfmpegAsync(args, trackId.Value, jobId, ct);

        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"DASH manifest not produced for track {trackId.Value}.");

        var prefix = dashManifestKey[..(dashManifestKey.LastIndexOf('/') + 1)];
        var artifactPaths = Directory.EnumerateFiles(outDir).ToList();

        LogDashUploading(artifactPaths.Count, jobId, trackId.Value, prefix);

        await PersistRenditionsAsync(
            db,
            trackId,
            manifestId,
            manifestPath,
            ct);

        foreach (var filePath in artifactPaths)
        {
            var fileName = Path.GetFileName(filePath);
            var key = $"{prefix}{fileName}";
            var contentType = ContentTypeForDashAsset(fileName);

            var bytes = await File.ReadAllBytesAsync(filePath, ct);
            await storage.PutAsync(MediaBucket.Audio, key, bytes, contentType, ct);

            LogDashArtifactUploaded(key, bytes.Length, jobId, trackId.Value);
        }

        LogDashPackagingCompleted(jobId, trackId.Value, transcodeStopwatch.ElapsedMilliseconds);
    }

    private static async Task EnsureRenditionsFromManifestAsync(
        IObjectStorage storage,
        CatalogDbContext db,
        string dashManifestKey,
        TrackId trackId,
        CancellationToken ct)
    {
        var manifestId = Guid.Parse(ExtractManifestId(dashManifestKey));
        var hasRenditions = await db.TrackAudioRenditions
            .AnyAsync(r => r.TrackId == trackId && r.ManifestId == manifestId, ct);
        if (hasRenditions) return;

        var manifest = await storage.GetAsync(MediaBucket.Audio, dashManifestKey, ct);
        if (manifest is null) return;

        var xml = Encoding.UTF8.GetString(manifest.Data.Span);
        await ReplaceRenditionsAsync(db, trackId, manifestId, xml, ct);
    }

    private static async Task PersistRenditionsAsync(
        CatalogDbContext db,
        TrackId trackId,
        Guid manifestId,
        string manifestPath,
        CancellationToken ct)
    {
        var xml = await File.ReadAllTextAsync(manifestPath, ct);
        await ReplaceRenditionsAsync(db, trackId, manifestId, xml, ct);
    }

    private static async Task ReplaceRenditionsAsync(
        CatalogDbContext db,
        TrackId trackId,
        Guid manifestId,
        string manifestXml,
        CancellationToken ct)
    {
        var parsed = DashManifestRenditionParser.Parse(manifestXml);
        var existing = await db.TrackAudioRenditions
            .Where(r => r.TrackId == trackId && r.ManifestId == manifestId)
            .ToListAsync(ct);
        db.TrackAudioRenditions.RemoveRange(existing);

        var now = DateTimeOffset.UtcNow;
        foreach (var rendition in parsed)
        {
            db.TrackAudioRenditions.Add(
                TrackAudioRendition.Create(
                    trackId,
                    rendition.Codec,
                    rendition.BitrateKbps,
                    rendition.SampleRateHz,
                    rendition.Bandwidth,
                    rendition.RepresentationId,
                    rendition.AdaptationSetId,
                    manifestId,
                    now));
        }

        await db.SaveChangesAsync(ct);
    }

    private static string ContentTypeForDashAsset(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        if (dot < 0) return "application/octet-stream";
        return fileName[(dot + 1)..].ToLowerInvariant() switch
        {
            "mpd" => "application/dash+xml",
            "m4s" => "video/mp4",
            "mp4" => "video/mp4",
            _ => "application/octet-stream",
        };
    }

    private static string ExtractManifestId(string dashManifestKey)
    {
        var parts = dashManifestKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // dash/{trackId}/{manifestId}/manifest.mpd
        if (parts.Length >= 4)
            return parts[2];
        return Guid.CreateVersion7().ToString();
    }

    private async Task<FfmpegRunResult> RunFfmpegAsync(string arguments, Guid trackId, Guid jobId, CancellationToken ct)
    {
        var ffmpegStopwatch = Stopwatch.StartNew();

        LogFfmpegStarting(jobId, trackId);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stderr = await stderrTask;
        var stdout = await stdoutTask;

        if (process.ExitCode != 0)
        {
            LogFfmpegFailed(jobId, trackId, process.ExitCode, ffmpegStopwatch.ElapsedMilliseconds, stderr);

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                LogFfmpegStdout(jobId, trackId, stdout);
            }

            throw new InvalidOperationException(
                $"ffmpeg failed (exit {process.ExitCode}). stdout='{stdout}' stderr='{stderr}'");
        }

        LogFfmpegCompleted(jobId, trackId, ffmpegStopwatch.ElapsedMilliseconds);

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            LogFfmpegStderr(jobId, trackId, stderr);
        }

        return new FfmpegRunResult(stdout, stderr);
    }

    private async Task<FfmpegRunResult> RunFfprobeAsync(string arguments, Guid trackId, Guid jobId, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stderr = await stderrTask;
        var stdout = await stdoutTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"ffprobe failed (exit {process.ExitCode}). stdout='{stdout}' stderr='{stderr}'");
        }

        return new FfmpegRunResult(stdout, stderr);
    }

    private async Task<IConnection> ConnectWithRetryAsync(
        ConnectionFactory factory,
        RabbitMqOptions rabbit,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        const int maxDelaySeconds = 30;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (BrokerUnreachableException ex)
            {
                LogRabbitMqUnreachable(ex, rabbit.HostName, rabbit.Port, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new OperationCanceledException(cancellationToken);
    }
}
