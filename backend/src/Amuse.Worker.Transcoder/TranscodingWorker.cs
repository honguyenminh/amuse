using System.Diagnostics;
using System.Text;
using System.Text.Json;
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

internal sealed class TranscodingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TranscodingWorker> logger,
    IOptions<RabbitMqOptions> rabbitOptions,
    IOptions<MediaOptions> mediaOptions) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbit = rabbitOptions.Value;

        logger.LogInformation(
            "Transcoder worker starting; RabbitMQ {RabbitHost}:{RabbitPort}, queue {QueueName}, prefetch {PrefetchCount}",
            rabbit.HostName,
            rabbit.Port,
            rabbit.QueueName,
            rabbit.PrefetchCount);

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

        logger.LogInformation(
            "Transcoder worker consuming queue {QueueName}",
            rabbit.QueueName);

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
                    logger.LogWarning(
                        "Ignoring RabbitMQ message with invalid payload on delivery {DeliveryTag}",
                        ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                logger.LogInformation(
                    "Received transcode message job {JobId} track {TrackId} delivery {DeliveryTag}",
                    msg.JobId,
                    msg.TrackId,
                    ea.DeliveryTag);

                scope = scopeFactory.CreateScope();
                db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

                job = await db.AudioTranscodeJobs
                    .FirstOrDefaultAsync(x => x.Id == msg.JobId, stoppingToken);

                if (job is null)
                {
                    logger.LogWarning(
                        "Transcode job {JobId} not found in database; acking delivery {DeliveryTag}",
                        msg.JobId,
                        ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                if (job.Status == AudioTranscodeJobStatus.Succeeded)
                {
                    logger.LogInformation(
                        "Transcode job {JobId} for track {TrackId} already succeeded; acking delivery {DeliveryTag}",
                        job.Id,
                        job.TrackId.Value,
                        ea.DeliveryTag);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                job.MarkProcessing(now);
                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "Transcode job {JobId} for track {TrackId} marked processing (attempt {AttemptCount}); master {MasterKey} -> stream {StreamKey}",
                    job.Id,
                    job.TrackId.Value,
                    job.AttemptCount,
                    job.MasterKey,
                    job.StreamKey);

                var alreadyReady = await storage.ObjectExistsAsync(MediaBucket.Audio, job.StreamKey, stoppingToken);
                if (!alreadyReady)
                {
                    await TranscodeMasterToDashAsync(
                        storage,
                        job.MasterKey,
                        job.StreamKey,
                        job.TrackId.Value,
                        job.Id,
                        stoppingToken);
                }
                else
                {
                    logger.LogInformation(
                        "DASH manifest already present at {StreamKey} for job {JobId}; skipping ffmpeg",
                        job.StreamKey,
                        job.Id);
                }

                var track = await db.Tracks
                    .FirstOrDefaultAsync(t => t.Id == job.TrackId, stoppingToken);
                if (track is not null)
                {
                    track.SetAudioStream(job.StreamKey);
                    track.MarkReady();
                }

                job.MarkSucceeded(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "Transcode job {JobId} for track {TrackId} succeeded in {ElapsedMs}ms; stream {StreamKey}",
                    job.Id,
                    job.TrackId.Value,
                    jobStopwatch.ElapsedMilliseconds,
                    job.StreamKey);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Transcode job {JobId} for track {TrackId} failed after {ElapsedMs}ms (attempt {AttemptCount})",
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
                        logger.LogWarning(
                            "Transcode job {JobId} for track {TrackId} persisted failure: {LastError}",
                            job.Id,
                            job.TrackId.Value,
                            job.LastError);
                    }
                    catch (Exception persistEx)
                    {
                        logger.LogError(
                            persistEx,
                            "Failed to persist transcode job {JobId} failure state",
                            job.Id);
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
            logger.LogInformation("Transcoder worker shutdown requested");
            try { channel.CloseAsync().GetAwaiter().GetResult(); } catch { }
            try { connection.CloseAsync().GetAwaiter().GetResult(); } catch { }
            tcs.TrySetResult();
        });

        await tcs.Task;
    }

    private async Task TranscodeMasterToDashAsync(
        IObjectStorage storage,
        string audioMasterKey,
        string dashManifestKey,
        Guid trackId,
        Guid jobId,
        CancellationToken ct)
    {
        var transcodeStopwatch = Stopwatch.StartNew();
        var manifestId = ExtractManifestId(dashManifestKey);

        logger.LogInformation(
            "Starting DASH packaging for job {JobId} track {TrackId}; master {MasterKey} -> {StreamKey}",
            jobId,
            trackId,
            audioMasterKey,
            dashManifestKey);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var inputUrl = storage.GetInternalSignedUrl(MediaBucket.Audio, audioMasterKey, ttl);

        var outDir = Path.Combine(
            Path.GetTempPath(),
            "amuse-dash",
            trackId.ToString(),
            manifestId);
        Directory.CreateDirectory(outDir);

        var manifestLocalName = Path.GetFileName(dashManifestKey);
        var manifestPath = Path.Combine(outDir, manifestLocalName);

        foreach (var file in Directory.EnumerateFiles(outDir))
            File.Delete(file);

        logger.LogDebug(
            "DASH work directory for job {JobId} track {TrackId}: {WorkDirectory}",
            jobId,
            trackId,
            outDir);

        var args =
            $"-hide_banner -loglevel error -y -nostdin -i \"{inputUrl}\" -vn " +
            "-map 0:a:0 -c:a aac -b:a 128k -ar 48000 " +
            "-f dash -seg_duration 4 -use_timeline 1 -use_template 1 " +
            "-init_seg_name init-stream$RepresentationID$.m4s " +
            "-media_seg_name chunk-stream$RepresentationID$-$Number%05d$.m4s " +
            $"-adaptation_sets \"id=0,streams=0\" \"{manifestPath}\"";

        await RunFfmpegAsync(args, trackId, jobId, ct);

        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"DASH manifest not produced for track {trackId}.");

        var prefix = dashManifestKey[..(dashManifestKey.LastIndexOf('/') + 1)];
        var artifactPaths = Directory.EnumerateFiles(outDir).ToList();

        logger.LogInformation(
            "Uploading {ArtifactCount} DASH artifacts for job {JobId} track {TrackId} under prefix {StoragePrefix}",
            artifactPaths.Count,
            jobId,
            trackId,
            prefix);

        foreach (var filePath in artifactPaths)
        {
            var fileName = Path.GetFileName(filePath);
            var key = $"{prefix}{fileName}";
            var contentType = ContentTypeForDashAsset(fileName);

            var bytes = await File.ReadAllBytesAsync(filePath, ct);
            await storage.PutAsync(MediaBucket.Audio, key, bytes, contentType, ct);

            logger.LogDebug(
                "Uploaded DASH artifact {ObjectKey} ({ByteCount} bytes) for job {JobId} track {TrackId}",
                key,
                bytes.Length,
                jobId,
                trackId);
        }

        logger.LogInformation(
            "DASH packaging completed for job {JobId} track {TrackId} in {ElapsedMs}ms",
            jobId,
            trackId,
            transcodeStopwatch.ElapsedMilliseconds);
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

    private async Task RunFfmpegAsync(string arguments, Guid trackId, Guid jobId, CancellationToken ct)
    {
        var ffmpegStopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Starting ffmpeg for job {JobId} track {TrackId}",
            jobId,
            trackId);

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
            logger.LogError(
                "ffmpeg failed for job {JobId} track {TrackId} with exit code {ExitCode} after {ElapsedMs}ms; stderr: {FfmpegStderr}",
                jobId,
                trackId,
                process.ExitCode,
                ffmpegStopwatch.ElapsedMilliseconds,
                stderr);

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                logger.LogDebug(
                    "ffmpeg stdout for job {JobId} track {TrackId}: {FfmpegStdout}",
                    jobId,
                    trackId,
                    stdout);
            }

            throw new InvalidOperationException(
                $"ffmpeg failed (exit {process.ExitCode}). stdout='{stdout}' stderr='{stderr}'");
        }

        logger.LogInformation(
            "ffmpeg completed for job {JobId} track {TrackId} in {ElapsedMs}ms",
            jobId,
            trackId,
            ffmpegStopwatch.ElapsedMilliseconds);

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            logger.LogDebug(
                "ffmpeg stderr for job {JobId} track {TrackId}: {FfmpegStderr}",
                jobId,
                trackId,
                stderr);
        }
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
                logger.LogWarning(
                    ex,
                    "RabbitMQ unreachable at {RabbitHost}:{RabbitPort}; retrying in {RetrySeconds}s",
                    rabbit.HostName,
                    rabbit.Port,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelaySeconds));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new OperationCanceledException(cancellationToken);
    }
}
