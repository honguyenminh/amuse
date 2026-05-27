using System.Collections.Concurrent;
using Amuse.Modules.Catalog.Processing;

namespace Amuse.Api.IntegrationTests;

public sealed class InMemoryAudioTranscodeJobQueue : IAudioTranscodeJobQueue
{
    public ConcurrentQueue<AudioTranscodeJobMessage> Messages { get; } = new();

    public Task PublishAsync(AudioTranscodeJobMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
