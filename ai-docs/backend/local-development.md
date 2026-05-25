# Local development & verification

## Prerequisites

- .NET 10 SDK
- Docker (for Postgres via Compose; required for integration tests)
- `dotnet-ef` tool (repo: [`backend/dotnet-tools.json`](../../backend/dotnet-tools.json))

## Start Postgres

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

HTTPS redirection is enabled; for local HTTP testing use the HTTP port from `launchSettings.json` or disable redirection in dev if needed.

## Seeded dev data

Seeding is **not** part of `migrate-all.sh`. The dotnet-ef CLI uses the design-time `DbContextFactory` and does not invoke `UseAsyncSeeding`. Two idempotent seeds run at **API startup in Development only**:

1. `PlatformRootSeeding.SeedAsync` — creates the root account, identity user, and platform operator.
2. `CatalogDevSeeding.SeedAsync` — populates a small fixture of artists/albums/tracks so the listener app has something to render.

Flow for a fresh DB:

1. `./scripts/migrate-all.sh` — applies schema for every bounded context.
2. `dotnet run --project src/Amuse.Api` — in Development environment, seeds the rows listed above.

Production/staging never auto-seed — operators provision the root account through a separate ops procedure and catalog data through normal write paths.

| Field | Value |
|-------|--------|
| Email | `root@amuse.local` |
| Password | `ChangeMe_Root123!` |
| Account ID | `00000000-0000-7000-8000-000000000001` |
| Persona | `platform` (platform operator id `1`) |

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
| Relation does not exist | Run `./scripts/migrate-all.sh` |
| Login 400 invalid persona | Root seed failed; check Platform migration/seed logs |
| Cookie refresh fails on HTTP | `ASPNETCORE_ENVIRONMENT=Development` (non-Secure cookies) |
| 401 not `token_revoked` | Ensure `Authorization` header sent on revoke |
