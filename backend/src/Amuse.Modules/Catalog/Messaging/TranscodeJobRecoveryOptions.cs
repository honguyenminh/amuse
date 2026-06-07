namespace Amuse.Modules.Catalog.Messaging;

public sealed class TranscodeJobRecoveryOptions
{
    public TimeSpan StaleProcessingTimeout { get; set; } = TimeSpan.FromMinutes(45);
}
