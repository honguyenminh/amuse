# Media (object storage)

The `Amuse.Modules.Media` module owns the abstraction over object storage. It does not have
its own bounded context or `DbContext` — media metadata that belongs to a domain entity
(cover art on `Artist`/`Album`, audio master on `Track`) is stored as object **keys** on
those entities and resolved to URLs at read time.

## Buckets

Two S3-compatible buckets, sourced from `appsettings:Media`:

| Bucket             | Visibility       | Purpose                          | URL shape                                              |
| ------------------ | ---------------- | -------------------------------- | ------------------------------------------------------ |
| `amuse-covers`     | anonymous read   | Album covers, artist art         | `GetPublicUrl` → `<PublicBaseUrl>/amuse-covers/<key>`  |
| `amuse-audio`      | private          | Track masters                    | `GetSignedUrl` → presigned GET with TTL                |

In dev, both back onto a single MinIO instance launched via `backend/compose.yaml`. In
prod they can point at AWS S3, Backblaze B2, R2, etc. — anywhere the AWS SDK v4 protocol
works.

## Local infra

`backend/compose.yaml` brings up:

- `minio` — MinIO server on `:9000` (S3 API) and `:9001` (console).
- `minio-init` — one-shot `mc` job that creates both buckets and sets `download` policy on
  `amuse-covers`. Idempotent (`mb --ignore-existing`).

```bash
docker compose up -d minio minio-init
# console: http://localhost:9001  (amuse / amuse_dev_secret)
```

## Configuration

`appsettings.Development.json` → `Media`:

```json
{
  "Endpoint":       "http://localhost:9000",
  "PublicBaseUrl":  "http://localhost:9000",
  "AccessKey":      "amuse",
  "SecretKey":      "amuse_dev_secret",
  "ForcePathStyle": true,
  "CoversBucket":   "amuse-covers",
  "AudioBucket":    "amuse-audio",
  "SignedUrlMinutes": 30
}
```

`Endpoint` is the S3 API URL the SDK talks to (uploads, presigned URL generation).
`PublicBaseUrl` is the host used to compose anonymous-read URLs handed to clients; this
is usually the same host in dev but may be a CDN in prod.

## Abstraction

```csharp
public interface IObjectStorage
{
    Task<bool> ObjectExistsAsync(MediaBucket bucket, string key, CancellationToken ct = default);
    Task PutAsync(MediaBucket bucket, string key, ReadOnlyMemory<byte> data, string contentType, CancellationToken ct = default);
    string GetPublicUrl(MediaBucket bucket, string key);
    string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl);
}
```

Implementation: `S3ObjectStorage` (uses `AWSSDK.S3` 4.0.x). Singleton, configured via
`MediaModule.AddMediaModule(configuration)` in `Program.cs`.

`GetPublicUrl` throws if called on a non-public bucket. `GetSignedUrl` returns a
presigned URL valid for the requested TTL.

### MinIO quirks the impl handles

- `ForcePathStyle = true` is required for any non-AWS S3-compatible store.
- The AWS SDK signs payloads on plain HTTP endpoints — `DisablePayloadSigning` cannot
  be enabled when the endpoint is not HTTPS.
- The SDK emits `https://` in presigned URLs even when `UseHttp = true` and the endpoint
  scheme is `http://`. `S3ObjectStorage.GetSignedUrl` rewrites the scheme back to `http://`
  when the configured endpoint is HTTP, so MinIO over plain HTTP works in dev.
- The signature is computed for the GET verb; `HEAD` requests on the signed URL fail with
  `403 Forbidden` and that is expected (browsers GET audio).

## Key conventions

Catalog seeding follows these conventions (see `CatalogDevSeeding.CoverKey` / `AudioKey`):

```
amuse-covers/
  artists/{slug}/avatar.bmp
  artists/{slug}/cover.bmp
  albums/{slug}/cover.bmp

amuse-audio/
  albums/{album-slug}/{track-number:00}-{title-slug}.wav
```

Keys are stored on the domain entities (`Artist.AvatarKey`, `Artist.CoverKey`,
`Album.CoverArtKey`, `Track.AudioMasterKey`) as plain strings with a 512-char max. The
catalog migration emitted these as `*_key` columns (e.g. `cover_art_key`).

## Catalog read flow

1. Catalog handler reads the entity (just keys, no URLs).
2. Cover URLs are composed via `IObjectStorage.GetPublicUrl(MediaBucket.Covers, key)` and
   included in the DTO as `coverArtUrl`/`avatarUrl`/`coverUrl`.
3. Track audio is **not** included in the album DTO — the client must call the dedicated
   `GET /api/v1/catalog/tracks/{id}/stream-info` endpoint to obtain a signed URL.
   The track DTO exposes a boolean `hasAudio` instead, so the UI can grey out
   un-uploaded tracks.

## Dev-only media seeding

`CatalogDevSeeding.SeedAsync(db, storage, ct)` does two things, idempotently:

1. **Uploads media** — for each artist slug it generates a 256x256 BMP gradient (hue
   derived from the slug hash) and uploads it as the avatar/cover. For each track it
   generates a 5-second sine wave WAV at a frequency derived from the track number and
   uploads it as the audio master. `ObjectExistsAsync` gates every upload so re-runs
   are no-ops.
2. **Writes catalog rows** — only when the catalog table is empty (idempotent guard).

Both BMP and WAV encoders live in `SeedMediaGenerators` and are hand-rolled (no extra
image/audio dependencies). Total data uploaded: ~28 objects, ~5.7 MB.

The seed runs at API startup in Development only — see `Program.cs`.

## Integration tests

`AmuseApiFixture` replaces `IAmazonS3` + `IObjectStorage` with an in-process
`InMemoryObjectStorage` that stores blobs in a `ConcurrentDictionary` and returns
deterministic URLs. This keeps tests hermetic — no MinIO container required.

Tests use the same `CatalogDevSeeding.SeedAsync` flow, exercising the upload + DB
path against the in-memory fake.

## Production checklist (when we get there)

- Move `amuse-covers` behind a CDN with image transforms; `PublicBaseUrl` becomes the
  CDN host.
- Add a bucket policy to `amuse-audio` so signed URL TTL is the only access path.
- Add a transcoding pipeline that drops `audio_master_key` and emits multiple bitrate
  derivatives; `stream-info` then picks based on network conditions.
- Server-side encryption (SSE-KMS) for `amuse-audio`.
- Consider replacing the dev BMP gradients with actual artwork uploaded by org users
  through the catalog management slice (future).
