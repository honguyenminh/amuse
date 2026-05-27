# Catalog module

The Catalog bounded context owns artists, releases, and tracks. It exposes a read-only HTTP surface used by the listener consumer app to populate the home feed and detail pages.

A **release** is the umbrella concept: it can be a single, an EP, an album, or a compilation, distinguished by the `release_type` discriminator. We collapse the MusicBrainz `release_group → release` two-level model down to a single `Release` aggregate for now; introducing a `ReleaseGroup` over `Release` is a future enhancement (e.g. for grouping remasters/regional editions of the same record).

## Domain model

| Aggregate  | Children          | Identifying fields                                          |
|------------|-------------------|-------------------------------------------------------------|
| `Artist`   | –                 | `id` (Guid v7), unique `slug`                               |
| `Release`  | `Track[]`         | `id`, unique (`artist_id`, `slug`), `release_type` (PG enum)|
| `Track`    | child of `Release`| `id`, unique (`release_id`, `track_number`)                 |

Value objects (`record struct` or `partial record struct`):

- `ArtistId`, `ReleaseId`, `TrackId` — V7 GUID wrappers with `From(Guid)` factories.
- `Slug` — lowercase `[a-z0-9](-[a-z0-9]+)*`, max 96.
- `TrackDuration` — positive int milliseconds.
- `ReleaseType` — `single | ep | album | compilation` mapped to Postgres enum `catalog.release_type` via `npgsql.MapEnum<ReleaseType>("release_type", "catalog")` + `modelBuilder.HasPostgresEnum<ReleaseType>(schema: "catalog", name: "release_type")`. Note that `album` here is one of four possible `release_type` values — not the entity name.

Errors live in `Amuse.Domain.Catalog.CatalogErrors`:

- `catalog.artist_not_found`
- `catalog.release_not_found`
- `catalog.track_not_found`
- `catalog.track_has_no_audio`
- `catalog.invalid_audio_upload_request`
- `catalog.audio_master_object_missing`
- `catalog.track_stream_not_ready`
- `catalog.stream_asset_not_found`
- `catalog.invalid_slug`, `catalog.invalid_artist`, `catalog.invalid_release`, `catalog.invalid_track`

## Endpoints

The three browse endpoints are **public** (`.AllowAnonymous()`); a Spotify/YouTube-Music–style flow expects anonymous visitors to be able to look at any artist, release, or the home feed without an account. Only `stream-info` requires a bearer token, because that's where actual playback begins.

| Method | Path | Auth | Handler | Notes |
|--------|------|------|---------|-------|
| `GET`  | `/api/v1/catalog/home`                          | Anonymous     | `BrowseHomeHandler`         | Returns 8 most-recent releases + 6 featured artists. |
| `GET`  | `/api/v1/catalog/artists/{artistId:guid}`       | Anonymous     | `GetArtistDetailHandler`    | Returns artist + discography. `400 catalog.artist_not_found` if missing. |
| `GET`  | `/api/v1/catalog/releases/{releaseId:guid}`     | Anonymous     | `GetReleaseDetailHandler`   | Returns release + ordered tracks. `400 catalog.release_not_found` if missing. |
| `GET`  | `/api/v1/catalog/tracks/{trackId:guid}/stream-info` | Bearer JWT | `GetTrackStreamInfoHandler` | Returns playback URL. If DASH is ready, URL points to authenticated catalog DASH endpoint; otherwise falls back to signed master URL. |
| `POST` | `/api/v1/catalog/tracks/{trackId:guid}/audio-master/presign-upload` | Bearer JWT | `PresignAudioMasterUploadHandler` | Returns short-lived presigned PUT URL + key for direct upload from uploader UI. |
| `POST` | `/api/v1/catalog/tracks/{trackId:guid}/audio-master/complete` | Bearer JWT | `CompleteAudioMasterUploadHandler` | Validates uploaded object, assigns `audio_master_key`, persists transcode job, publishes RabbitMQ message. |
| `GET`  | `/api/v1/catalog/tracks/{trackId:guid}/dash/{manifestId}/{assetName}` | Bearer JWT | `GetTrackDashAssetHandler` | Authenticated DASH gateway. Serves `manifest.mpd` from API and issues signed redirects for segment files. |

Response DTOs are defined in `Catalog/Features/Shared/CatalogDtos.cs` and per-feature handler files. Track durations are returned as `durationMs` (int milliseconds). `ReleaseSummary` is the canonical card shape (used both in `BrowseHomeResponse.recentReleases` and `GetArtistDetailResponse.releases`).

## Persistence

- Schema: `catalog`
- Migrations now include:
  - `InitialCatalog` for `artist` / `release` / `track`
  - `AddAudioTranscodeJobs` for `track.audio_stream_key` and `audio_transcode_job` queue/status table
- FK from track to release is `ON DELETE CASCADE`.
- `audio_transcode_job` is the durable status record for ingestion/transcoding lifecycle (`queued`/`processing`/`succeeded`/`failed`). RabbitMQ carries work dispatch; DB carries job state/audit.
- `Release.Tracks` is a private list exposed as `IReadOnlyList<Track>`. EF Core access mode is configured to `Field` in `ReleaseConfiguration`.

## Dev seed

`CatalogDevSeeding.SeedAsync` is invoked once at API startup **only when `app.Environment.IsDevelopment()`**. It is idempotent: returns early if any artist already exists, and each media upload to MinIO is gated by `IObjectStorage.ObjectExistsAsync` so re-runs only push missing keys. The fixture (`AmuseApiFixture`) calls the same method for integration tests so endpoint tests can rely on a populated catalog plus matching MinIO objects (via the in-memory adapter).

Sample data: 3 artists (Aurora Lights, Iron Palms, Velvet Monsoon), 5 releases spanning every `ReleaseType` value, each with a procedurally generated BMP gradient cover (`releases/<slug>/cover.bmp`) and per-track WAV sine waves (`releases/<slug>/NN-<track-slug>.wav`).

## Frontend wiring

`frontend/consumer/src/lib/api/catalogClient.ts` exposes `browseCatalogHome()`, `getCatalogArtist(id)`, `getCatalogRelease(id)` (browse — routed through `publicFetch`, anonymous-friendly), and `getTrackStreamInfo(id)` (auth required — routed through `authFetch` with transparent token refresh).

The release page uses `useCoverArtSeed(coverArtUrl)` (see `frontend/consumer/src/theme/useCoverArtSeed.ts`) to:

1. Resolve a deterministic hash seed immediately (fast first paint).
2. Asynchronously load the image with `crossOrigin = "anonymous"`, sample it on a 16×16 canvas, convert to OKLCH, and update the page seed.

That seed is fed into the theme system through `usePageSeed`, which keeps the existing precedence order intact: page seed > playing seed > default.

## Adding new catalog features

1. Domain changes in `backend/src/Amuse.Domain/Catalog`. Value objects must remain `record struct` or sealed with private constructors.
2. New endpoint in `backend/src/Amuse.Modules/Catalog/Features/<Feature>/` (handler + endpoint + DTOs).
3. Register handler in `CatalogModule.AddCatalogModule`. Map endpoint in `CatalogModule.MapCatalogModule`.
4. Decide auth posture explicitly — `.AllowAnonymous()` for browse, `.RequireAuthorization()` for anything that touches a user identity or signed media.
5. Add OpenAPI metadata on the endpoint via `.WithSummary`, `.Produces<T>()`, and `.ProducesProblem(StatusCodes.Status400BadRequest)` referencing the relevant `CatalogErrors.*` code in the summary.
6. Update the integration tests fixture or add a new `Catalog*Tests` file under `tests/Amuse.Api.IntegrationTests`.
