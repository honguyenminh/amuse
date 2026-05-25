# B2C frontend foundation — implementation & verification

## What was implemented

### Design system (Tailwind-first, Material-like tokens)

- Semantic color roles on `--amuse-*` CSS variables, mapped to Tailwind (`bg-primary`, `text-on-surface`, …).
- Typography utilities: `text-display-large` through `text-label-medium`.
- Runtime theme engine: OKLCH seed → palette, paused variant, page/playing seed priority.
- `ThemeProvider` applies palette to `document.documentElement`.

### UI primitives

- `AppShell`, `TopBar`, `BottomNav`, `Button`, `Card`, `Text` — token-only styling.

### Auth (web)

- `identityClient` + `AuthProvider` with in-memory access token.
- `credentials: include`, `X-Amuse-Client: web`.
- Refresh single-flight lock (`withRefreshLock`).
- Login, logout (revoke), session restore via refresh cookie.

### Listener bootstrap

- `POST /api/v1/listener/profile/ensure` (backend VSA slice added).
- `bootstrapListener()` ensures profile then verifies `listener` in `/personas`.

### Routes

| Route | Description |
|-------|-------------|
| `/login` | Email/password sign-in |
| `/home` | Shell + playback seed demo |
| `/artist/[artistId]` | Page seed override (demo seeds) |
| `/album/[albumId]` | Page seed override (demo seeds) |

### Quality gates

- `pnpm test` — Vitest (theme + refresh lock).
- `pnpm check:colors` — fails on literal colors outside token sources.
- Dev CORS on API for `http://localhost:3000`.

## Manual verification runbook

### Prerequisites

```bash
# Terminal 1 — backend
cd backend
docker compose up -d postgres
./scripts/migrate-all.sh
dotnet run --project src/Amuse.Api

# Terminal 2 — frontend
cd frontend/consumer
cp .env.example .env.local   # NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
pnpm install
pnpm dev
```

Use HTTP URLs consistently (API launchSettings HTTP port, typically 5000 or 8080).

### 1. Token foundation

- Open `http://localhost:3000/login`.
- Expect brutalist borders, semantic background (not raw zinc/gray page).
- Inspect `<html>` in devtools: `--amuse-primary`, `--amuse-surface`, etc. present.

### 2. Dynamic theme

- Sign in → `/home`.
- Click **Play demo track** — chrome shifts to blue-ish seed.
- **Pause** — palette fades (lower chroma).
- **Resume** — returns to playing seed.
- Bottom nav → **Artist** / **Album** — distinct hue overrides.
- Back to **Home** — override clears.

### 3. Auth

- Login with `root@amuse.local` / `ChangeMe_Root123!` (after migrations/seed).
- DevTools → Application → Cookies: `amuse_refresh` set; no refresh token in localStorage.
- **Log out** → cookie cleared; `/home` redirects to login.
- Sign in again; refresh cookie restores session without re-entering password.

### 4. Listener bootstrap

- After login, home shows **Listener ID** (GUID).
- If bootstrap fails, retry screen appears (not a blank crash).

### 5. Automated checks

```bash
cd frontend/consumer
pnpm test
pnpm check:colors
pnpm lint
pnpm build
```

## Blocker checklist

| Symptom | Check |
|---------|--------|
| CORS errors | API running; `UseCors("DevFrontend")` in Development; `.env.local` API URL matches |
| Cookie not set | Same-site: use `localhost` for both; API `Secure` cookie only outside Development |
| Login 400 | Migrations + root user seeded |
| Ensure 404 | `MapListenerModule()` in `Program.cs`; rebuild API |
| No listener persona | `bootstrapListener` + Tenancy/Listener DB seeded |
| Colors check fails | Remove literal `zinc-*` / hex from components; use semantic classes |

## Related docs

- [b2c-foundation-spec.md](./b2c-foundation-spec.md)
- [../backend/identity-auth.md](../backend/identity-auth.md)
