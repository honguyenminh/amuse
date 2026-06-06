namespace Amuse.Domain.Catalog;

public sealed class TrackAudioRendition
{
    public Guid Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public AudioCodec Codec { get; private set; }
    public int? BitrateKbps { get; private set; }
    public int SampleRateHz { get; private set; }
    public int Bandwidth { get; private set; }
    public string RepresentationId { get; private set; } = string.Empty;
    public string AdaptationSetId { get; private set; } = string.Empty;
    public Guid ManifestId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TrackAudioRendition()
    {
    }

    public static TrackAudioRendition Create(
        TrackId trackId,
        AudioCodec codec,
        int? bitrateKbps,
        int sampleRateHz,
        int bandwidth,
        string representationId,
        string adaptationSetId,
        Guid manifestId,
        DateTimeOffset createdAt)
    {
        return new TrackAudioRendition
        {
            Id = Guid.CreateVersion7(),
            TrackId = trackId,
            Codec = codec,
            BitrateKbps = bitrateKbps,
            SampleRateHz = sampleRateHz,
            Bandwidth = bandwidth,
            RepresentationId = representationId,
            AdaptationSetId = adaptationSetId,
            ManifestId = manifestId,
            CreatedAt = createdAt,
        };
    }
}
