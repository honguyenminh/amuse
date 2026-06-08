using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public readonly record struct TrackCollaboratorAssignment(
    ArtistId? ArtistId,
    string? DisplayName);
