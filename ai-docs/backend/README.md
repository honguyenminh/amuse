# Amuse backend — AI documentation

This folder documents **current backend behavior and conventions** as implemented in the repo. It is meant for humans and AI agents working on the codebase.

**Authoritative product/architecture docs** remain in [`ads/`](../../ads/) (especially [`ads/auth/`](../../ads/auth/) and [`ads/backend-structure.md`](../../ads/backend-structure.md)). **`design/` is not source of truth.**

## Contents

| Document | Description |
|----------|-------------|
| [identity-auth.md](./identity-auth.md) | Identity BC: endpoints, token model, auth flows, errors, JTI blacklist |
| [local-development.md](./local-development.md) | Postgres, migrations policy, run API, worker, MinIO/CORS, manual verification |
| [architecture-conventions.md](./architecture-conventions.md) | VSA, DDD, cross-BC boundaries, validation, migrations |
| [testing.md](./testing.md) | Unit and integration tests, Docker requirement |
| [ci-cd.md](./ci-cd.md) | GitHub Actions, branch model, GHCR publish, deploy stub, migrations in deploy |
| [catalog.md](./catalog.md) | Catalog BC: stream-info (DASH), dev seed + transcode job enqueue |
| [media.md](./media.md) | Ingest, presign, worker, DASH output, client read path |

## Quick reference

- **API base (Identity):** `/api/v1/identity`
- **Local DB:** `Host=localhost;Port=5432;Database=amuse_development` (see `appsettings.Development.json`)
- **Migrations:** never on app startup — run [`backend/scripts/migrate-all.sh`](../../backend/scripts/migrate-all.sh)
- **Dev root user:** `root@amuse.local` / `ChangeMe_Root123!` (platform persona)

## What was built (Identity slice summary)

- Modular monolith with **Identity**, **Tenancy**, **Listener**, **Platform**, **Audit** modules and separate EF schemas.
- **Persona-aware JWT access tokens**; **opaque refresh sessions** (permission-agnostic; persona chosen at login/refresh).
- **Vertical slice features** under `Amuse.Modules/Identity/Features/` with FluentValidation and RFC 7807-style problem mapping.
- **JTI blacklist** on revoke via Redis (source of truth) + per-pod local cache; JWT bearer checks memory only.
- **Integration tests** (Testcontainers + Postgres + Redis) and **local compose** stack.

## Not yet implemented (follow-ups)

- Listener `ensure-profile` HTTP feature
- Tenancy org/membership CRUD features
- OpenAPI/Scalar polish
- Frontend auth client
