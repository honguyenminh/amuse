# B2C frontend foundation — implementation & verification

## What was implemented

### Design system (Tailwind-first, Material-like tokens)

- Semantic color roles on `--amuse-*` CSS variables, mapped to Tailwind (`bg-primary`, `text-on-surface`, …).
- Typography utilities: `text-display-large` through `text-label-medium`.
- Runtime theme engine: OKLCH seed → palette, paused variant, page/playing seed priority.
- `ThemeProvider` applies palette to `document.documentElement`.

### UI primitives

- `AppShell`, `TopBar`, `Sidebar`, `MobileDrawer`, `Button`, `IconButton`, `Card`,
  `Text`, `Slider`, `Skeleton`, `PlaybackIcons`, `NavIcons` — token-only styling.
- `AppShell` lays out a persistent left `Sidebar` on `md+`, collapses it into a
  hamburger-triggered `MobileDrawer` below `md`, and docks a full-width `MiniPlayer`
  fixed at the bottom. The previous bottom-tabbed `BottomNav` is gone.

### Auth (web)

- `identityClient` + `AuthProvider` with in-memory access token.
- `credentials: include`, `X-Amuse-Client: web`.
- Refresh single-flight lock (`withRefreshLock`).
- Login, logout (revoke), session restore via refresh cookie.

### Listener bootstrap

- `POST /api/v1/listener/profile/ensure` (backend VSA slice added).
- `bootstrapListener()` ensures profile then verifies `listener` in `/personas`.

### Routes

| Route | Auth | Description |
|-------|------|-------------|
| `/login` | n/a | Email/password sign-in. Honours `?next=<path>` to bounce visitors back where they came from. |
| `/home` | anonymous | Curated home feed: recent releases + featured artists. |
| `/artist/[artistId]` | anonymous | Artist detail + discography. Page seed sampled from the cover. |
| `/release/[releaseId]` | anonymous | Release (album / EP / single / compilation) detail with tracklist + play button. |
| `/playing` | anonymous (auto-redirects to `/home` when queue is empty) | Full-screen now-playing view with seekbar, transport, repeat/shuffle, up-next. |

Playback (`getTrackStreamInfo`) is the only catalog operation that requires a session.
Pressing **Play** without a session redirects to `/login?next=<current path>`.

### B2cGate

`(b2c)/layout.tsx` wraps every browse route in `<B2cGate>`. Since the renaming, the gate
**does not redirect anonymous visitors** — it only blocks while the auth provider is
restoring (one-frame "Loading…" splash) and surfaces a `bootstrapError` retry screen when
a logged-in session can't load its listener profile. Anonymous users see every browse
page rendered through the normal shell with a "Log in" button in the sidebar.

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

### 2. Anonymous browse + theming

- Visit `http://localhost:3000/` without logging in. You should land on `/home` and see
  the seeded releases + featured artists, no redirect to `/login`.
- Click any release tile (`/release/{id}`) → seed shifts to the cover's hue.
- Click an artist (`/artist/{id}`) → seed shifts again.
- Press **Play** on a release → redirects to `/login?next=/release/{id}` because
  streaming requires a session.

### 3. Auth

- Login with `root@amuse.local` / `ChangeMe_Root123!` (after migrations/seed). After
  submit, you should be back on the page you came from.
- DevTools → Application → Cookies: `amuse_refresh` set; no refresh token in localStorage.
- **Log out** (account button in the top bar) → cookie cleared; you stay on the current
  page and may continue to browse anonymously.
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
