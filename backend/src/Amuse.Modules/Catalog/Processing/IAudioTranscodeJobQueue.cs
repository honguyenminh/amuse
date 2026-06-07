using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Amuse.Modules.Catalog.Processing;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Queue suffix names the messaging abstraction.")]
public interface IAudioTranscodeJobQueue
{
    Task PublishAsync(AudioTranscodeJobMessage message, CancellationToken cancellationToken = default);
}

