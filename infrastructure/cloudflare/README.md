# Cloudflare R2 + CDN (stage media)

Amuse stage stores media in **Cloudflare R2**. Covers are served from a **public CDN/custom domain**; audio masters and DASH segments stay private and use **presigned URLs** signed for the R2 S3 API endpoint.

Terraform writes credentials to Azure Key Vault (`infrastructure/terraform/secrets.tf`). Kubernetes pulls them via External Secrets on AKS.

## Architecture (current — DA1 API-mediated DASH)

| Asset | Bucket | Access | URL host |
|-------|--------|--------|----------|
| Cover art | `amuse-covers` | Public read | `Media__PublicBaseUrl` (CDN custom domain) |
| Audio masters + DASH | `amuse-audio` | Private | Presigned GET on `Media__PresignBaseUrl` (R2 S3 API) |
| DASH manifest | `amuse-audio` | Via API | `https://api.<domain>/api/v1/catalog/tracks/.../manifest.mpd` |

Playback flow:

1. Consumer fetches `stream-info` from API (authenticated).
2. dash.js loads manifest from API (authenticated).
3. Segment requests hit API → **302** to presigned R2 URL (no auth header; signature in query string).
4. Browser fetches segment cross-origin → **R2 CORS** must allow the app origin.

Future edge JWT playback (`design/docs/cdn-streaming-spec.md`) is not implemented yet.

## 1. Create buckets

In Cloudflare dashboard → R2:

| Bucket | Public access |
|--------|---------------|
| `amuse-covers` | Enable public access via **custom domain** (e.g. `media.staging.example.com`) |
| `amuse-audio` | **Private** (no public access) |

Or via Wrangler:

```bash
wrangler r2 bucket create amuse-covers
wrangler r2 bucket create amuse-audio
```

## 2. API token

Create an R2 token with read/write on both buckets. Store in Terraform `staging.tfvars`:

- `amuse_r2_access_key`
- `amuse_r2_secret_key`
- `amuse_r2_endpoint` = `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`

## 3. Cover CDN custom domain

Attach a custom domain to **`amuse-covers`** (Cloudflare → R2 → bucket → Settings → Custom Domains).

Set the same origin in:

- Terraform `amuse_r2_public_base_url`
- Key Vault `amuse-r2-public-base-url`
- `overlays/stage/config/cluster.env` → `MEDIA_PUBLIC_BASE_URL`

Example: `https://media.staging.example.com`

## 4. Presign endpoint

Set Terraform `amuse_r2_presign_base_url` to the **R2 S3 API endpoint** (usually identical to `amuse_r2_endpoint`):

```
https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

This becomes `Media__PresignBaseUrl` in API secrets. Do **not** use the CDN hostname here — presigned URLs must target the S3 API host.

## 5. CORS (required for playback + uploads)

Configure CORS on **both** buckets for browser origins from `cluster.env` → `MEDIA_CORS_ORIGINS`.

Example policy (Dashboard → R2 → bucket → CORS, or S3 API):

```json
[
  {
    "AllowedOrigins": [
      "https://app.staging.example.com",
      "https://business.staging.example.com"
    ],
    "AllowedMethods": ["GET", "HEAD", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": [
      "ETag",
      "Accept-Ranges",
      "Content-Length",
      "Content-Range",
      "Content-Type"
    ],
    "MaxAgeSeconds": 3600
  }
]
```

Required because:

- Consumer uses `crossOrigin="anonymous"` for cover art and Web Audio volume.
- dash.js follows 302 redirects to presigned segment URLs (cross-origin GET + Range).
- Business portal uploads masters via presigned PUT.

## 6. Align Key Vault with cluster.env

After editing `overlays/stage/config/cluster.env`:

| cluster.env | Key Vault secret |
|-------------|------------------|
| `MEDIA_PUBLIC_BASE_URL` | `amuse-r2-public-base-url` |

Run `terraform apply` when rotating R2 values; External Secrets refresh hourly.

## 7. Verification

From repo root (with `kubectl` pointed at AKS):

```bash
./infrastructure/kubernetes/scripts/verify-stage-media.sh
```

Manual playback check (logged-in consumer):

1. Upload or seed a track with DASH packaging complete.
2. `GET https://api.<domain>/api/v1/catalog/tracks/{id}/stream-info` → manifest path.
3. Play in consumer — Network tab should show manifest on API host, segments on `*.r2.cloudflarestorage.com` (302).

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Cover art blank / theme seed fails | `MEDIA_PUBLIC_BASE_URL` mismatch or CDN domain not public |
| Segment 403 after 302 | Wrong `Media__PresignBaseUrl` or expired presign |
| CORS error on segment | R2 CORS missing app origin or `Range` not allowed |
| Upload PUT fails | CORS missing `PUT` or wrong presign host |
| `track_stream_not_ready` | Transcoder not finished; check worker logs and R2 `dash/` prefix |
