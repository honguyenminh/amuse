using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Seeding;

/// <summary>
/// Idempotent dev-only seed. Populates a small fixture of artists, albums and tracks the first
/// time it runs against an empty catalog. Safe to call repeatedly: returns early when the
/// catalog already has data.
/// </summary>
public static class CatalogDevSeeding
{
    public static async Task SeedAsync(CatalogDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Artists.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var aurora = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000001")),
            name: "Aurora Lights",
            slug: Slug.From("aurora-lights"),
            createdAt: now,
            bio: "Nordic synth-folk duo built around dawn-themed compositions.",
            avatarUrl: "https://picsum.photos/seed/aurora-avatar/512/512",
            coverUrl: "https://picsum.photos/seed/aurora-cover/1600/900");

        var ironPalms = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000002")),
            name: "Iron Palms",
            slug: Slug.From("iron-palms"),
            createdAt: now,
            bio: "Brutalist post-rock outfit recording almost entirely live to tape.",
            avatarUrl: "https://picsum.photos/seed/iron-palms-avatar/512/512",
            coverUrl: "https://picsum.photos/seed/iron-palms-cover/1600/900");

        var velvetMonsoon = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000003")),
            name: "Velvet Monsoon",
            slug: Slug.From("velvet-monsoon"),
            createdAt: now,
            bio: "Chamber-pop trio splitting time between Hanoi and Lisbon.",
            avatarUrl: "https://picsum.photos/seed/velvet-monsoon-avatar/512/512",
            coverUrl: "https://picsum.photos/seed/velvet-monsoon-cover/1600/900");

        db.Artists.AddRange(aurora, ironPalms, velvetMonsoon);

        var dawnAlbum = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000001")),
            artistId: aurora.Id,
            title: "Dawn Anatomy",
            slug: Slug.From("dawn-anatomy"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2025, 11, 14, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtUrl: "https://picsum.photos/seed/aurora-dawn-anatomy/800/800");

        dawnAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000001")), "Threshold", 1, TrackDuration.FromMilliseconds(214_000));
        dawnAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000002")), "First Wave", 2, TrackDuration.FromMilliseconds(187_500));
        dawnAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000003")), "Halfway House", 3, TrackDuration.FromMilliseconds(243_000));
        dawnAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000004")), "Slow Dawn", 4, TrackDuration.FromMilliseconds(305_000));

        var auroraEp = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000002")),
            artistId: aurora.Id,
            title: "Ribbons & Frost",
            slug: Slug.From("ribbons-and-frost"),
            releaseType: ReleaseType.Ep,
            releaseDate: new DateTimeOffset(2024, 3, 22, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtUrl: "https://picsum.photos/seed/aurora-ribbons/800/800");

        auroraEp.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000010")), "Ribbons", 1, TrackDuration.FromMilliseconds(168_000));
        auroraEp.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000011")), "Frost", 2, TrackDuration.FromMilliseconds(202_400));
        auroraEp.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000012")), "Margins", 3, TrackDuration.FromMilliseconds(220_000));

        var concreteWaves = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000003")),
            artistId: ironPalms.Id,
            title: "Concrete Waves",
            slug: Slug.From("concrete-waves"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2025, 6, 6, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtUrl: "https://picsum.photos/seed/iron-palms-concrete-waves/800/800");

        concreteWaves.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000020")), "Rebar", 1, TrackDuration.FromMilliseconds(254_000));
        concreteWaves.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000021")), "Spillway", 2, TrackDuration.FromMilliseconds(312_500));
        concreteWaves.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000022")), "Half-Tide", 3, TrackDuration.FromMilliseconds(289_000));
        concreteWaves.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000023")), "Pour", 4, TrackDuration.FromMilliseconds(401_750));

        var pylonsSingle = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000004")),
            artistId: ironPalms.Id,
            title: "Pylons",
            slug: Slug.From("pylons"),
            releaseType: ReleaseType.Single,
            releaseDate: new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtUrl: "https://picsum.photos/seed/iron-palms-pylons/800/800");

        pylonsSingle.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000030")), "Pylons", 1, TrackDuration.FromMilliseconds(276_000));

        var velvetAlbum = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000005")),
            artistId: velvetMonsoon.Id,
            title: "Weather Reports",
            slug: Slug.From("weather-reports"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtUrl: "https://picsum.photos/seed/velvet-weather-reports/800/800");

        velvetAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000040")), "Static Bloom", 1, TrackDuration.FromMilliseconds(232_000));
        velvetAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000041")), "Monsoon Hours", 2, TrackDuration.FromMilliseconds(258_500));
        velvetAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000042")), "Soft Power Cuts", 3, TrackDuration.FromMilliseconds(204_000));
        velvetAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000043")), "Hanoi Through Curtains", 4, TrackDuration.FromMilliseconds(297_000));
        velvetAlbum.AddTrack(TrackId.From(Guid.Parse("019e6020-0000-7000-8000-000000000044")), "Lisbon, Drying", 5, TrackDuration.FromMilliseconds(341_500));

        db.Albums.AddRange(dawnAlbum, auroraEp, concreteWaves, pylonsSingle, velvetAlbum);

        await db.SaveChangesAsync(cancellationToken);
    }
}
