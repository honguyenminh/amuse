using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Seeding;

/// <summary>
/// Idempotent dev-only seed. Populates a small fixture of artists, releases and tracks the first
/// time it runs against an empty catalog, and uploads matching cover/audio assets to MinIO.
/// Safe to call repeatedly: each step is skipped when the data already exists.
/// </summary>
public static class CatalogDevSeeding
{
    /// <summary>Synthetic org id for dev fixture catalog rows (not a real tenancy org).</summary>
    public static readonly OrganizationId DevFixtureOrganizationId =
        OrganizationId.From(Guid.Parse("019e5000-0000-7000-8000-000000000099"));

    public static async Task SeedAsync(
        CatalogDbContext db,
        IObjectStorage storage,
        IAudioTranscodeJobQueue jobQueue,
        IClock clock,
        CancellationToken cancellationToken)
    {
        await EnsureMediaAsync(storage, cancellationToken);

        if (!await db.Artists.AnyAsync(cancellationToken))
        {
            var now = clock.UtcNow;
            var fixtureOrg = DevFixtureOrganizationId;

            var aurora = Artist.Create(
                id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000001")),
                name: "Aurora Lights",
                slug: Slug.From("aurora-lights"),
                createdAt: now,
                visibilityTier: ArtistVisibilityTier.PlatformVerified,
                bio: "Nordic synth-folk duo built around dawn-themed compositions.",
                avatarKey: CoverKey.ForArtistAvatar("aurora-lights"),
                coverKey: CoverKey.ForArtistCover("aurora-lights")).Value!;

            var ironPalms = Artist.Create(
                id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000002")),
                name: "Iron Palms",
                slug: Slug.From("iron-palms"),
                createdAt: now,
                visibilityTier: ArtistVisibilityTier.PlatformVerified,
                bio: "Brutalist post-rock outfit recording almost entirely live to tape.",
                avatarKey: CoverKey.ForArtistAvatar("iron-palms"),
                coverKey: CoverKey.ForArtistCover("iron-palms")).Value!;

            var velvetMonsoon = Artist.Create(
                id: ArtistId.From(Guid.Parse("019e6000-0000-7000-8000-000000000003")),
                name: "Velvet Monsoon",
                slug: Slug.From("velvet-monsoon"),
                createdAt: now,
                visibilityTier: ArtistVisibilityTier.PlatformVerified,
                bio: "Chamber-pop trio splitting time between Hanoi and Lisbon.",
                avatarKey: CoverKey.ForArtistAvatar("velvet-monsoon"),
                coverKey: CoverKey.ForArtistCover("velvet-monsoon")).Value!;

            db.Artists.AddRange(aurora, ironPalms, velvetMonsoon);

            var dawn = Release.Create(
                id: ReleaseId.From(Guid.Parse("019e6010-0000-7000-8000-000000000001")),
                organizationId: fixtureOrg,
                artistId: aurora.Id,
                title: "Dawn Anatomy",
                slug: Slug.From("dawn-anatomy"),
                releaseType: ReleaseType.Album,
                releaseDate: new DateTimeOffset(2025, 11, 14, 0, 0, 0, TimeSpan.Zero),
                createdAt: now,
                coverArtKey: CoverKey.ForReleaseCover("dawn-anatomy")).Value!;

            AddTrack(dawn, "019e6020-0000-7000-8000-000000000001", "Threshold", 1, 214_000);
            AddTrack(dawn, "019e6020-0000-7000-8000-000000000002", "First Wave", 2, 187_500);
            AddTrack(dawn, "019e6020-0000-7000-8000-000000000003", "Halfway House", 3, 243_000);
            AddTrack(dawn, "019e6020-0000-7000-8000-000000000004", "Slow Dawn", 4, 305_000);
            dawn.MarkPublishedForDevelopment(now);

            var ribbons = Release.Create(
                id: ReleaseId.From(Guid.Parse("019e6010-0000-7000-8000-000000000002")),
                organizationId: fixtureOrg,
                artistId: aurora.Id,
                title: "Ribbons & Frost",
                slug: Slug.From("ribbons-and-frost"),
                releaseType: ReleaseType.Ep,
                releaseDate: new DateTimeOffset(2024, 3, 22, 0, 0, 0, TimeSpan.Zero),
                createdAt: now,
                coverArtKey: CoverKey.ForReleaseCover("ribbons-and-frost")).Value!;

            AddTrack(ribbons, "019e6020-0000-7000-8000-000000000010", "Ribbons", 1, 168_000);
            AddTrack(ribbons, "019e6020-0000-7000-8000-000000000011", "Frost", 2, 202_400);
            AddTrack(ribbons, "019e6020-0000-7000-8000-000000000012", "Margins", 3, 220_000);
            ribbons.MarkPublishedForDevelopment(now);

            var concreteWaves = Release.Create(
                id: ReleaseId.From(Guid.Parse("019e6010-0000-7000-8000-000000000003")),
                organizationId: fixtureOrg,
                artistId: ironPalms.Id,
                title: "Concrete Waves",
                slug: Slug.From("concrete-waves"),
                releaseType: ReleaseType.Album,
                releaseDate: new DateTimeOffset(2025, 6, 6, 0, 0, 0, TimeSpan.Zero),
                createdAt: now,
                coverArtKey: CoverKey.ForReleaseCover("concrete-waves")).Value!;

            AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000020", "Rebar", 1, 254_000);
            AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000021", "Spillway", 2, 312_500);
            AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000022", "Half-Tide", 3, 289_000);
            AddTrack(concreteWaves, "019e6020-0000-7000-8000-000000000023", "Pour", 4, 401_750);
            concreteWaves.MarkPublishedForDevelopment(now);

            var pylons = Release.Create(
                id: ReleaseId.From(Guid.Parse("019e6010-0000-7000-8000-000000000004")),
                organizationId: fixtureOrg,
                artistId: ironPalms.Id,
                title: "Pylons",
                slug: Slug.From("pylons"),
                releaseType: ReleaseType.Single,
                releaseDate: new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero),
                createdAt: now,
                coverArtKey: CoverKey.ForReleaseCover("pylons")).Value!;

            AddTrack(pylons, "019e6020-0000-7000-8000-000000000030", "Pylons", 1, 276_000);
            pylons.MarkPublishedForDevelopment(now);

            var weather = Release.Create(
                id: ReleaseId.From(Guid.Parse("019e6010-0000-7000-8000-000000000005")),
                organizationId: fixtureOrg,
                artistId: velvetMonsoon.Id,
                title: "Weather Reports",
                slug: Slug.From("weather-reports"),
                releaseType: ReleaseType.Album,
                releaseDate: new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero),
                createdAt: now,
                coverArtKey: CoverKey.ForReleaseCover("weather-reports")).Value!;

            AddTrack(weather, "019e6020-0000-7000-8000-000000000040", "Static Bloom", 1, 232_000);
            AddTrack(weather, "019e6020-0000-7000-8000-000000000041", "Monsoon Hours", 2, 258_500);
            AddTrack(weather, "019e6020-0000-7000-8000-000000000042", "Soft Power Cuts", 3, 204_000);
            AddTrack(weather, "019e6020-0000-7000-8000-000000000043", "Hanoi Through Curtains", 4, 297_000);
            AddTrack(weather, "019e6020-0000-7000-8000-000000000044", "Lisbon, Drying", 5, 341_500);
            weather.MarkPublishedForDevelopment(now);

            db.Releases.AddRange(dawn, ribbons, concreteWaves, pylons, weather);

            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureStreamIngestJobsAsync(db, clock, cancellationToken);
    }

    private static void AddTrack(Release release, string idString, string title, int trackNumber, int durationMs)
    {
        var result = release.AddTrack(
            TrackId.From(Guid.Parse(idString)),
            title,
            trackNumber,
            TrackDuration.FromMilliseconds(durationMs),
            audioMasterKey: AudioKey.ForTrack(release.Slug.Value, trackNumber, title));

        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error!.Message);
    }

    /// <summary>
    /// Generates and uploads the BMP cover + WAV audio set for each artist/release/track combo.
    /// Each upload is gated by ObjectExistsAsync so re-runs only push missing keys.
    /// </summary>
    private static async Task EnsureMediaAsync(IObjectStorage storage, CancellationToken cancellationToken)
    {
        await UploadCoverIfMissingAsync(storage, "aurora-lights", isArtist: true, cancellationToken);
        await UploadCoverIfMissingAsync(storage, "iron-palms", isArtist: true, cancellationToken);
        await UploadCoverIfMissingAsync(storage, "velvet-monsoon", isArtist: true, cancellationToken);

        foreach (var (releaseSlug, tracks) in TrackPlan)
        {
            await UploadCoverIfMissingAsync(storage, releaseSlug, isArtist: false, cancellationToken);
            foreach (var (trackNumber, title) in tracks)
            {
                var key = AudioKey.ForTrack(releaseSlug, trackNumber, title);
                if (await storage.ObjectExistsAsync(MediaBucket.Audio, key, cancellationToken))
                    continue;

                // Pentatonic-ish tone, distinct per track within a release.
                var frequency = 220.0 * Math.Pow(2, (trackNumber - 1) / 12.0);
                var bytes = SeedMediaGenerators.GenerateSineWaveWav(frequency, durationSeconds: 5.0);
                await storage.PutAsync(MediaBucket.Audio, key, bytes, "audio/wav", cancellationToken);
            }
        }
    }

    /// <summary>
    /// Ensures every track with an audio master has a queued ingest job for a DASH stream.
    /// This is what upgrades the legacy dev WAV fixtures into the encoded/streamed flow.
    /// Idempotent: does not create duplicate in-flight jobs.
    /// </summary>
    private static async Task EnsureStreamIngestJobsAsync(
        CatalogDbContext db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var candidates = await db.Tracks
            .Where(t => t.AudioMasterKey != null && t.AudioStreamKey == null)
            .Select(t => new { t.Id, t.AudioMasterKey })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return;

        foreach (var t in candidates)
        {
            var hasInflight = await db.AudioTranscodeJobs
                .AnyAsync(
                    j => j.TrackId == t.Id && j.Status != AudioTranscodeJobStatus.Failed,
                    cancellationToken);
            if (hasInflight)
                continue;

            var derivedId = Guid.CreateVersion7();
            var streamKey = $"dash/{t.Id.Value}/{derivedId}/manifest.mpd";
            var now = clock.UtcNow;

            var job = AudioTranscodeJob.Enqueue(t.Id, t.AudioMasterKey!, streamKey, now);
            db.AudioTranscodeJobs.Add(job);
            db.CatalogOutboxMessages.Add(
                CatalogOutboxMessage.EnqueueAudioTranscode(
                    new AudioTranscodeJobMessage(job.Id, t.Id.Value, job.MasterKey, job.StreamKey),
                    now));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task UploadCoverIfMissingAsync(
        IObjectStorage storage,
        string slug,
        bool isArtist,
        CancellationToken cancellationToken)
    {
        // Avatars (artist square), full cover (artist hero), and release covers all share the
        // same generator. Each gets its own deterministic seed so they look distinct.
        if (isArtist)
        {
            await UploadIfMissing(CoverKey.ForArtistAvatar(slug), $"{slug}-avatar");
            await UploadIfMissing(CoverKey.ForArtistCover(slug), $"{slug}-cover");
        }
        else
        {
            await UploadIfMissing(CoverKey.ForReleaseCover(slug), $"{slug}-release");
        }

        async Task UploadIfMissing(string key, string seed)
        {
            if (await storage.ObjectExistsAsync(MediaBucket.Covers, key, cancellationToken))
                return;
            var bmp = SeedMediaGenerators.GenerateGradientBmp(seed);
            await storage.PutAsync(MediaBucket.Covers, key, bmp, "image/bmp", cancellationToken);
        }
    }

    private static readonly (string ReleaseSlug, (int TrackNumber, string Title)[] Tracks)[] TrackPlan =
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
    public static string ForReleaseCover(string slug) => $"releases/{slug}/cover.bmp";
}

/// <summary>Conventions for object keys in the private audio bucket.</summary>
internal static class AudioKey
{
    public static string ForTrack(string releaseSlug, int trackNumber, string title) =>
        $"releases/{releaseSlug}/{trackNumber:00}-{Slugify(title)}.wav";

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
