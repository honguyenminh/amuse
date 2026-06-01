# Local development & verification

## Prerequisites

- Docker or Podman with Compose (for the local backend stack)
- .NET 10 SDK (only if you run the API/worker on the host instead of Compose)
- `dotnet-ef` tool (repo: [`backend/dotnet-tools.json`](../../backend/dotnet-tools.json))

## Quick start (recommended)

From [`backend/`](../../backend/), start infra + migrations + API + transcoder worker in one shot:

```bash
cd backend
./scripts/dev-up.sh
# or: docker compose up -d --build
```

Then run a frontend **on the host** (Compose bind mounts are awkward with Podman/SELinux):

```bash
cd frontend/business   # port 3001
pnpm dev

# or consumer app on port 3000
cd frontend/consumer
pnpm dev
```

| Service | URL |
|---------|-----|
| API | http://localhost:5000 |
| OpenAPI | http://localhost:5000/openapi/v1.json |
| MinIO console | http://localhost:9001 (`amuse` / `amuse_dev_secret`) |
| RabbitMQ UI | http://localhost:15672 (`amuse` / `amuse_dev_secret`) |
| Mailpit (dev email) | http://localhost:8025 (SMTP `localhost:1025`) |

The API listens on **HTTP port 5000** with CORS enabled for local Next.js dev servers
(`localhost` and `127.0.0.1` on ports 3000/3001). Override with `NEXT_PUBLIC_API_BASE_URL`
if needed (defaults to `http://localhost:5000` in both frontends).

Infra-only (no API/worker containers):

```bash
docker compose up -d postgres minio minio-init rabbitmq mailpit
```

Registration confirmation emails use **Mailpit** when `Identity:Email:Smtp:Enabled` is true (default in `appsettings.Development.json`). After sign-up, open Mailpit and click the confirm link. With `dotnet run` on the host, SMTP targets `localhost:1025`; API-in-compose uses hostname `mailpit`. Set `Smtp.Enabled` to `false` to log links only.

## Start Postgres only

From [`backend/`](../../backend/):

```bash
docker compose up -d postgres
```

Compose service: **Postgres 18**, DB `amuse_development`, user/password `postgres`, port **5432**.

Connection string (default in Development):

```
Host=localhost;Port=5432;Database=amuse_development;Username=postgres;Password=postgres
```

Override with `ConnectionStrings__DefaultConnection` if needed.

## Migrations policy

**The API never applies migrations on startup** (not Development, not Production).

In Compose, a one-shot **`migrate`** service runs [`scripts/migrate-all.sh`](../../backend/scripts/migrate-all.sh)
before `amuse.api` starts. On the host, run the same script manually.

Schema changes are:

| Environment | Who applies migrations |
|-------------|----------------------|
| Local dev | Developer runs script (below) |
| Staging / prod | Deploy pipeline **before** new API rollout |
| Integration tests | Test fixture only (`ModuleDatabaseInitializer`) |

### Apply all bounded-context migrations

```bash
cd backend
./scripts/migrate-all.sh
```

Order: Identity → Tenancy → Listener → Platform → Catalog → Audit.

## Object storage + queue infra (MinIO + RabbitMQ)

Catalog cover art/audio masters use MinIO; transcode dispatch uses RabbitMQ. The local
stack ships both under `backend/compose.yaml`:

```bash
cd backend
docker compose up -d minio minio-init rabbitmq   # also starts postgres if needed
```

`minio-init` is a one-shot job that creates the `amuse-covers` (public-read) and
`amuse-audio` (private) buckets and configures **global MinIO API CORS** (required for
cross-origin audio playback + presigned PUTs in some MinIO Community setups). Web console: <http://localhost:9001> (creds:
`amuse` / `amuse_dev_secret`).

The dev API seeds both buckets at startup with deterministic content (BMP gradient
covers + sine-wave WAV audio). See `ai-docs/backend/media.md`.

RabbitMQ management UI: <http://localhost:15672> (`amuse` / `amuse_dev_secret`).

**First run on an empty database:** EF Core may log `Failed executing DbCommand` when selecting from `__EFMigrationsHistory_*` because those tables do not exist yet. That probe is normal. Treat the run as successful when each context ends with `Done.` and the script exits `0`. Re-runs are idempotent (only pending migrations apply).

Equivalent manual command per context:

```bash
dotnet ef database update \
  --project src/Amuse.Modules \
  --startup-project src/Amuse.Api \
  --context IdentityDbContext
```

## Run the API

```bash
cd backend
dotnet run --project src/Amuse.Api
```

Development opens OpenAPI at `/openapi/v1.json` when `ASPNETCORE_ENVIRONMENT=Development`.

HTTPS redirection is **disabled in Development** so browser clients can call `http://localhost:5000`
without redirect/CORS preflight issues. Production still redirects to HTTPS.

## Run the transcoder worker

The worker consumes RabbitMQ transcode jobs and writes DASH assets (`manifest.mpd` +
`*.m4s`) into the private audio bucket.

```bash
cd backend
dotnet run --project src/Amuse.Worker.Transcoder
```

Run from repo **`backend/`** root (as above) or set `ConnectionStrings__DefaultConnection` / RabbitMQ / Media env vars explicitly. The worker loads `appsettings.json` from the **content root**; if you `dotnet run` from another directory without overrides, `DefaultConnection` may be missing.

`TranscodingWorker` emits **structured logs** (Information) for: RabbitMQ connect settings, message received (`JobId`, `TrackId`, delivery tag), job processing / skip-if-already-packaged, ffmpeg start/end timing, artifact upload counts, success/failure with elapsed ms. On ffmpeg failure, stderr is included in the error log. For verbose per-object upload lines in dev, `src/Amuse.Worker.Transcoder/appsettings.Development.json` sets `Amuse.Worker.Transcoder.TranscodingWorker` to Debug.

For containerized local stack, `./scripts/dev-up.sh` (or `docker compose up -d --build`) starts the worker with the API.

## Seeded dev data

Seeding is **not** part of `migrate-all.sh`. The dotnet-ef CLI uses the design-time `DbContextFactory` and does not invoke `UseAsyncSeeding`. Two idempotent seeds run at **API startup in Development only**:

1. `PlatformRootSeeding.SeedAsync` — creates the root account, identity user, and platform operator.
2. `CatalogDevSeeding.SeedAsync` — populates a small fixture of artists/releases/tracks, uploads dev media to MinIO, and **enqueues transcode jobs** for tracks that have a master but no DASH stream yet (requires RabbitMQ + worker for playback to succeed via `stream-info`).

Flow for a fresh DB (Compose):

1. `docker compose up -d --build` — runs `migrate`, then starts API + worker + infra.
2. API startup (Development) runs platform + catalog seeds and enqueues transcode jobs.
3. Worker consumes jobs; DASH playback works once packaging finishes.

Flow for a fresh DB (host API):

1. `./scripts/migrate-all.sh` — applies schema for every bounded context.
2. `docker compose up -d postgres minio minio-init rabbitmq` — infra for storage + queue.
3. `dotnet run --project src/Amuse.Worker.Transcoder` — consumes jobs (optional for catalog browse; **required** for DASH `stream-info` on seeded tracks).
4. `dotnet run --project src/Amuse.Api` — in Development environment, runs seeds above.

Production/staging never auto-seed — operators provision the root account through a separate ops procedure and catalog data through normal write paths.

| Field | Value |
|-------|--------|
| Email | `root@amuse.local` |
| Password | `ChangeMe_Root123!` |
| Account ID | `00000000-0000-7000-8000-000000000001` |
| Persona | `platform` (platform operator id `1`) |

## Integration tests (`Amuse.Api.IntegrationTests`)

Requires **Docker** (Testcontainers spins up an ephemeral Postgres 16 container with database `amuse_test`).

```bash
cd backend
dotnet test tests/Amuse.Api.IntegrationTests
```

Tests use environment **`Testing`**, not `Development`, so they do **not** read `appsettings.Development.json` or write to `amuse_development`. The fixture refuses to start if the connection string targets `amuse_development`.

If you see leftover rows such as `Member Flow Org` or `Integration Indie` in your local dev database, they were almost certainly created before this isolation was enforced (when the fixture used `Development` and could fall through to the host Postgres). Safe cleanup: delete those org rows from `tenancy.organization` in `amuse_development`, or reset the dev DB and re-run migrations + API seed.

## Manual verification (curl)

### 1. Login (mobile-style, tokens in body)

```bash
BASE=http://localhost:5000   # adjust port

curl -s -X POST "$BASE/api/v1/identity/login/password" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "root@amuse.local",
    "password": "ChangeMe_Root123!",
    "context": { "type": "platform", "orgId": null, "listenerId": null }
  }'
```

Save `accessToken` and `refreshToken` from the JSON response.

### 2. Get current account

```bash
curl -s "$BASE/api/v1/identity/me" \
  -H "Authorization: Bearer <accessToken>"
```

### 3. List personas

```bash
curl -s "$BASE/api/v1/identity/personas" \
  -H "Authorization: Bearer <accessToken>"
```

Expect at least a `platform` entry for the root user.

### 4. Refresh (persona switch / renew access)

```bash
curl -s -X POST "$BASE/api/v1/identity/refresh" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<refreshToken>",
    "context": { "type": "platform", "orgId": null, "listenerId": null }
  }'
```

### 5. Revoke (invalidate refresh + access)

```bash
curl -s -X POST "$BASE/api/v1/identity/revoke" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <accessToken>" \
  -d '{ "refreshToken": "<refreshToken>" }'
```

Expect `204 No Content`.

### 6. Confirm access revoked

```bash
curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/identity/me" \
  -H "Authorization: Bearer <accessToken>"
```

Expect **401**; body should reference `identity.token_revoked`.

### Web client cookie flow

```bash
# Login with cookie transport
curl -s -c cookies.txt -X POST "$BASE/api/v1/identity/login/password" \
  -H "Content-Type: application/json" \
  -H "X-Amuse-Client: web" \
  -d '{
    "email": "root@amuse.local",
    "password": "ChangeMe_Root123!",
    "context": { "type": "platform", "orgId": null, "listenerId": null }
  }'

# Refresh using cookie only (no refreshToken in body)
curl -s -b cookies.txt -c cookies.txt -X POST "$BASE/api/v1/identity/refresh" \
  -H "Content-Type: application/json" \
  -H "X-Amuse-Client: web" \
  -d '{ "context": { "type": "platform", "orgId": null, "listenerId": null } }'
```

## Troubleshooting

| Issue | Check |
|-------|--------|
| Connection refused to DB | `docker compose ps`, port 5432 free |
| Relation does not exist | Re-run migrate: `docker compose run --rm migrate` or `./scripts/migrate-all.sh` |
| Login 400 invalid persona | Root seed failed; check Platform migration/seed logs |
| Cookie refresh fails on HTTP | `ASPNETCORE_ENVIRONMENT=Development` (non-Secure cookies) |
| Browser CORS errors to API | Frontend origin must be `localhost` or `127.0.0.1` on port 3000/3001; API must be `http://localhost:5000` |
| Cover/audio URLs broken in browser | Compose `Media__PublicBaseUrl` must be `http://localhost:9000`, not `http://minio:9000` |
| Transcoder ffmpeg `Connection refused` to `localhost:9000` | Worker must presign masters with `Media__Endpoint` (`http://minio:9000` in compose), not `PublicBaseUrl`; see `GetInternalSignedUrl` |
| 401 not `token_revoked` | Ensure `Authorization` header sent on revoke |
