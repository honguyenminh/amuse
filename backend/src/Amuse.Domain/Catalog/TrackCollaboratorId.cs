namespace Amuse.Domain.Catalog;

public readonly record struct TrackCollaboratorId(Guid Value)
{
    public static TrackCollaboratorId New() => new(Guid.CreateVersion7());

    public static TrackCollaboratorId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Track collaborator id cannot be empty.", nameof(value));

        return new TrackCollaboratorId(value);
    }
}
