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

        var factory = new ConnectionFactory
        {
            HostName = rabbit.HostName,
            Port = rabbit.Port,
            UserName = rabbit.UserName,
            Password = rabbit.Password,
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: rabbit.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, rabbit.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            IServiceScope? scope = null;
            CatalogDbContext? db = null;
            AudioTranscodeJob? job = null;

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<AudioTranscodeJobMessage>(json, SerializerOptions);
                if (msg is null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                scope = scopeFactory.CreateScope();
                db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

                job = await db.AudioTranscodeJobs
                    .FirstOrDefaultAsync(x => x.Id == msg.JobId, stoppingToken);

                if (job is null || job.Status == AudioTranscodeJobStatus.Succeeded)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                job.MarkProcessing(now);
                await db.SaveChangesAsync(stoppingToken);

                var alreadyReady = await storage.ObjectExistsAsync(MediaBucket.Audio, job.StreamKey, stoppingToken);
                if (!alreadyReady)
                {
                    await TranscodeMasterToDashAsync(
                        storage,
                        job.MasterKey,
                        job.StreamKey,
                        job.TrackId.Value,
                        stoppingToken);
                }

                var track = await db.Tracks
                    .FirstOrDefaultAsync(t => t.Id == job.TrackId, stoppingToken);
                if (track is not null)
                {
                    track.SetAudioStream(job.StreamKey);
                }

                job.MarkSucceeded(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transcode worker failed processing message.");
                if (db is not null && job is not null)
                {
                    try
                    {
                        job.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch { /* best-effort */ }
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
        CancellationToken ct)
    {
        var manifestId = ExtractManifestId(dashManifestKey);
        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var inputUrl = storage.GetSignedUrl(MediaBucket.Audio, audioMasterKey, ttl);

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

        var args =
            $"-hide_banner -loglevel error -y -nostdin -i \"{inputUrl}\" -vn " +
            "-map 0:a:0 -c:a aac -b:a 128k -ar 48000 " +
            "-f dash -seg_duration 4 -use_timeline 1 -use_template 1 " +
            "-init_seg_name init-stream$RepresentationID$.m4s " +
            "-media_seg_name chunk-stream$RepresentationID$-$Number%05d$.m4s " +
            $"-adaptation_sets \"id=0,streams=0\" \"{manifestPath}\"";

        await RunFfmpegAsync(args, ct);

        if (!File.Exists(manifestPath))
            throw new InvalidOperationException($"DASH manifest not produced for track {trackId}.");

        var prefix = dashManifestKey[..(dashManifestKey.LastIndexOf('/') + 1)];

        foreach (var filePath in Directory.EnumerateFiles(outDir))
        {
            var fileName = Path.GetFileName(filePath);
            var key = $"{prefix}{fileName}";
            var contentType = ContentTypeForDashAsset(fileName);

            var bytes = await File.ReadAllBytesAsync(filePath, ct);
            await storage.PutAsync(MediaBucket.Audio, key, bytes, contentType, ct);
        }
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

    private static async Task RunFfmpegAsync(string arguments, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
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
        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            var stdout = await stdoutTask;
            throw new InvalidOperationException(
                $"ffmpeg failed (exit {process.ExitCode}). stdout='{stdout}' stderr='{stderr}'");
        }
    }
}
