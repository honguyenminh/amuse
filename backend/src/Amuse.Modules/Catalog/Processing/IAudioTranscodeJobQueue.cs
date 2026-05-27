using System.Threading;

namespace Amuse.Modules.Catalog.Processing;

public interface IAudioTranscodeJobQueue
{
    Task PublishAsync(AudioTranscodeJobMessage message, CancellationToken cancellationToken = default);
}

