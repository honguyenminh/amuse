using System;

namespace Amuse.Modules.Catalog.Processing;

/// <summary>
/// RabbitMQ message contract for a single audio transcode packaging job.
/// </summary>
public sealed record AudioTranscodeJobMessage(
    Guid JobId,
    Guid TrackId,
    string MasterKey,
    string StreamKey);

