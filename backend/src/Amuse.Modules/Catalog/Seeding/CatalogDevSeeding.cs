using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Seeding;

/// <summary>
/// Idempotent dev-only seed. Populates a small fixture of artists, albums and tracks the first
/// time it runs against an empty catalog, and uploads matching cover/audio assets to MinIO.
/// Safe to call repeatedly: each step is skipped when the data already exists.
/// </summary>
public static class CatalogDevSeeding
{
    public static async Task SeedAsync(
        CatalogDbContext db,
        IObjectStorage storage,
        CancellationToken cancellationToken)
    {
        await EnsureMediaAsync(storage, cancellationToken);

        if (await db.Artists.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;

        var aurora = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000001")),
            name: "Aurora Lights",
            slug: Slug.From("aurora-lights"),
            createdAt: now,
            bio: "Nordic synth-folk duo built around dawn-themed compositions.",
            avatarKey: CoverKey.ForArtistAvatar("aurora-lights"),
            coverKey: CoverKey.ForArtistCover("aurora-lights"));

        var ironPalms = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000002")),
            name: "Iron Palms",
            slug: Slug.From("iron-palms"),
            createdAt: now,
            bio: "Brutalist post-rock outfit recording almost entirely live to tape.",
            avatarKey: CoverKey.ForArtistAvatar("iron-palms"),
            coverKey: CoverKey.ForArtistCover("iron-palms"));

        var velvetMonsoon = Artist.Create(
            id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000003")),
            name: "Velvet Monsoon",
            slug: Slug.From("velvet-monsoon"),
            createdAt: now,
            bio: "Chamber-pop trio splitting time between Hanoi and Lisbon.",
            avatarKey: CoverKey.ForArtistAvatar("velvet-monsoon"),
            coverKey: CoverKey.ForArtistCover("velvet-monsoon"));

        db.Artists.AddRange(aurora, ironPalms, velvetMonsoon);

        var dawnAlbum = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000001")),
            artistId: aurora.Id,
            title: "Dawn Anatomy",
            slug: Slug.From("dawn-anatomy"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2025, 11, 14, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtKey: CoverKey.ForAlbumCover("dawn-anatomy"));

        AddTrack(dawnAlbum, "019e6020-0000-7000-8000-000000000001", "Threshold",      1, 214_000);
        AddTrack(dawnAlbum, "019e6020-0000-7000-8000-000000000002", "First Wave",     2, 187_500);
        AddTrack(dawnAlbum, "019e6020-0000-7000-8000-000000000003", "Halfway House",  3, 243_000);
        AddTrack(dawnAlbum, "019e6020-0000-7000-8000-000000000004", "Slow Dawn",      4, 305_000);

        var auroraEp = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000002")),
            artistId: aurora.Id,
            title: "Ribbons & Frost",
            slug: Slug.From("ribbons-and-frost"),
            releaseType: ReleaseType.Ep,
            releaseDate: new DateTimeOffset(2024, 3, 22, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtKey: CoverKey.ForAlbumCover("ribbons-and-frost"));

        AddTrack(auroraEp, "019e6020-0000-7000-8000-000000000010", "Ribbons", 1, 168_000);
        AddTrack(auroraEp, "019e6020-0000-7000-8000-000000000011", "Frost",   2, 202_400);
        AddTrack(auroraEp, "019e6020-0000-7000-8000-000000000012", "Margins", 3, 220_000);

        var concreteWaves = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000003")),
            artistId: ironPalms.Id,
            title: "Concrete Waves",
            slug: Slug.From("concrete-waves"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2025, 6, 6, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtKey: CoverKey.ForAlbumCover("concrete-waves"));

        AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000020", "Rebar",      1, 254_000);
        AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000021", "Spillway",   2, 312_500);
        AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000022", "Half-Tide",  3, 289_000);
        AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000023", "Pour",       4, 401_750);

        var pylonsSingle = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000004")),
            artistId: ironPalms.Id,
            title: "Pylons",
            slug: Slug.From("pylons"),
            releaseType: ReleaseType.Single,
            releaseDate: new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtKey: CoverKey.ForAlbumCover("pylons"));

        AddTrack(pylonsSingle, "019e6020-0000-7000-8000-000000000030", "Pylons", 1, 276_000);

        var velvetAlbum = Album.Create(
            id: AlbumId.From(Guid.Parse("019e6010-0000-7000-8000-000000000005")),
            artistId: velvetMonsoon.Id,
            title: "Weather Reports",
            slug: Slug.From("weather-reports"),
            releaseType: ReleaseType.Album,
            releaseDate: new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero),
            createdAt: now,
            coverArtKey: CoverKey.ForAlbumCover("weather-reports"));

        AddTrack(velvetAlbum, "019e6020-0000-7000-8000-000000000040", "Static Bloom",           1, 232_000);
        AddTrack(velvetAlbum, "019e6020-0000-7000-8000-000000000041", "Monsoon Hours",          2, 258_500);
        AddTrack(velvetAlbum, "019e6020-0000-7000-8000-000000000042", "Soft Power Cuts",        3, 204_000);
        AddTrack(velvetAlbum, "019e6020-0000-7000-8000-000000000043", "Hanoi Through Curtains", 4, 297_000);
        AddTrack(velvetAlbum, "019e6020-0000-7000-8000-000000000044", "Lisbon, Drying",         5, 341_500);

        db.Albums.AddRange(dawnAlbum, auroraEp, concreteWaves, pylonsSingle, velvetAlbum);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void AddTrack(Album album, string idString, string title, int trackNumber, int durationMs) =>
        album.AddTrack(
            TrackId.From(Guid.Parse(idString)),
            title,
            trackNumber,
            TrackDuration.FromMilliseconds(durationMs),
            audioMasterKey: AudioKey.ForTrack(album.Slug.Value, trackNumber, title));

    /// <summary>
    /// Generates and uploads the BMP cover + WAV audio set for each artist/album/track combo.
    /// Each upload is gated by ObjectExistsAsync so re-runs only push missing keys.
    /// </summary>
    private static async Task EnsureMediaAsync(IObjectStorage storage, CancellationToken cancellationToken)
    {
        await UploadCoverIfMissingAsync(storage, "aurora-lights", isArtist: true, cancellationToken);
        await UploadCoverIfMissingAsync(storage, "iron-palms", isArtist: true, cancellationToken);
        await UploadCoverIfMissingAsync(storage, "velvet-monsoon", isArtist: true, cancellationToken);

        foreach (var (albumSlug, tracks) in TrackPlan)
        {
            await UploadCoverIfMissingAsync(storage, albumSlug, isArtist: false, cancellationToken);
            foreach (var (trackNumber, title) in tracks)
            {
                var key = AudioKey.ForTrack(albumSlug, trackNumber, title);
                if (await storage.ObjectExistsAsync(MediaBucket.Audio, key, cancellationToken))
                    continue;

                // Pentatonic-ish tone, distinct per track within an album.
                var frequency = 220.0 * Math.Pow(2, (trackNumber - 1) / 12.0);
                var bytes = SeedMediaGenerators.GenerateSineWaveWav(frequency, durationSeconds: 5.0);
                await storage.PutAsync(MediaBucket.Audio, key, bytes, "audio/wav", cancellationToken);
            }
        }
    }

    private static async Task UploadCoverIfMissingAsync(
        IObjectStorage storage,
        string slug,
        bool isArtist,
        CancellationToken cancellationToken)
    {
        // Avatars (artist square), full cover (artist hero), and album covers all share the
        // same generator. Each gets its own deterministic seed so they look distinct.
        if (isArtist)
        {
            await UploadIfMissing(CoverKey.ForArtistAvatar(slug), $"{slug}-avatar");
            await UploadIfMissing(CoverKey.ForArtistCover(slug), $"{slug}-cover");
        }
        else
        {
            await UploadIfMissing(CoverKey.ForAlbumCover(slug), $"{slug}-album");
        }

        async Task UploadIfMissing(string key, string seed)
        {
            if (await storage.ObjectExistsAsync(MediaBucket.Covers, key, cancellationToken))
                return;
            var bmp = SeedMediaGenerators.GenerateGradientBmp(seed);
            await storage.PutAsync(MediaBucket.Covers, key, bmp, "image/bmp", cancellationToken);
        }
    }

    private static readonly (string AlbumSlug, (int TrackNumber, string Title)[] Tracks)[] TrackPlan =
    [
        ("dawn-anatomy", [
            (1, "Threshold"), (2, "First Wave"), (3, "Halfway House"), (4, "Slow Dawn"),
        ]),
        ("ribbons-and-frost", [
            (1, "Ribbons"), (2, "Frost"), (3, "Margins"),
        ]),
        ("concrete-waves", [
            (1, "Rebar"), (2, "Spillway"), (3, "Half-Tide"), (4, "Pour"),
        ]),
        ("pylons", [
            (1, "Pylons"),
        ]),
        ("weather-reports", [
            (1, "Static Bloom"), (2, "Monsoon Hours"), (3, "Soft Power Cuts"),
            (4, "Hanoi Through Curtains"), (5, "Lisbon, Drying"),
        ]),
    ];
}

/// <summary>Conventions for object keys in the public covers bucket.</summary>
internal static class CoverKey
{
    public static string ForArtistAvatar(string slug) => $"artists/{slug}/avatar.bmp";
    public static string ForArtistCover(string slug) => $"artists/{slug}/cover.bmp";
    public static string ForAlbumCover(string slug) => $"albums/{slug}/cover.bmp";
}

/// <summary>Conventions for object keys in the private audio bucket.</summary>
internal static class AudioKey
{
    public static string ForTrack(string albumSlug, int trackNumber, string title) =>
        $"albums/{albumSlug}/{trackNumber:00}-{Slugify(title)}.wav";

    private static string Slugify(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length);
        var lastDash = false;
        foreach (var rawCh in value.ToLowerInvariant())
        {
            if (rawCh is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(rawCh);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }
        if (lastDash) sb.Length--;
        return sb.ToString();
    }
}
