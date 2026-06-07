using System.Text.Json;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Catalog.Messaging;

public sealed partial class CatalogOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogOutboxProcessor> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogBatchFailed(ex);
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var jobQueue = scope.ServiceProvider.GetRequiredService<IAudioTranscodeJobQueue>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        var pending = await db.CatalogOutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var message in pending)
        {
            try
            {
                if (string.Equals(message.MessageType, CatalogOutboxMessage.AudioTranscodeJobType, StringComparison.Ordinal))
                {
                    var payload = JsonSerializer.Deserialize<AudioTranscodeJobMessage>(message.PayloadJson, SerializerOptions);
                    if (payload is null)
                        throw new InvalidOperationException("Outbox payload deserialized to null.");

                    await jobQueue.PublishAsync(payload, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown outbox message type '{message.MessageType}'.");
                }

                message.MarkProcessed(now);
                processed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkAttemptFailed(ex.Message, now);
                LogMessageFailed(ex, message.Id, message.AttemptCount);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return processed;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Catalog outbox batch processing failed")]
    private partial void LogBatchFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Catalog outbox message {MessageId} failed on attempt {AttemptCount}")]
    private partial void LogMessageFailed(Exception ex, Guid messageId, int attemptCount);
}
