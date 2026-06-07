using Amuse.Domain.Catalog;
using Amuse.Modules.Common.Persistence;

namespace Amuse.Modules.Catalog.Tests;

public sealed class AuditEntityRegistryTests
{
    [Fact]
    public void Register_resolves_strongly_typed_id()
    {
        var registry = new AuditEntityRegistry();
        registry.Register<Artist>("catalog.artist");

        var artist = Artist.Create(
            ArtistId.From(Guid.Parse("019e7000-0000-7000-8000-000000000010")),
            "Test Artist",
            Slug.From("test-artist"),
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            visibilityTier: ArtistVisibilityTier.Unverified).Value!;

        Assert.True(registry.TryGetRegistration(typeof(Artist), out var registration));
        Assert.Equal("catalog.artist", registration.TableName);
        Assert.Equal(artist.Id.Value, registration.TargetIdResolver(artist));
    }
}
