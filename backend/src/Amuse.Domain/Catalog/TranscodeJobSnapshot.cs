namespace Amuse.Domain.Catalog;

public readonly record struct TranscodeJobSnapshot(
    Guid Id,
    TranscodeJobStatus Status,
    DateTimeOffset UpdatedAt,
    string MasterKey,
    string StreamKey,
    int AttemptCount);
