using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Ingestion.Contracts;

/// <summary>
/// Cross-BC ingestion command port. Catalog handlers will depend on this contract when
/// audio ingest tables move from the catalog schema into ingestion.
/// </summary>
public interface IIngestionCommands
{
    Task<Result> EnqueueTranscodeAsync(
        TrackId trackId,
        string masterKey,
        string streamKey,
        CancellationToken cancellationToken);
}

internal sealed class IngestionCommandsStub : IIngestionCommands
{
    public Task<Result> EnqueueTranscodeAsync(
        TrackId trackId,
        string masterKey,
        string streamKey,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException(
            "Ingestion BC schema migration is not complete. Use catalog-scoped ingest handlers until Phase 4b lands.");
}
