# B2C consumer foundation spec

Executable spec derived from `ads/frontend/theme/*` and `ads/auth/*`. Implementation target: `frontend/consumer`.

## Visual system

- **Aesthetic:** brutalism/retro — slim borders, solid fills, minimal shadow, bold MD3-expressive-bold feel.
- **Color math:** OKLCH preferred; runtime semantic palette from a **seed**.
- **Dynamic color:**
  - Default app seed when nothing is playing.
  - **Playing seed** from current track/album art (placeholder IDs until catalog API exists).
  - **Page seed override** on artist/album routes when entity has `colorSeed`.
  - **Paused:** same hue, lower chroma / more faded (subtle UI).
  - **Shell policy (this phase):** chrome (top bar, bottom nav, background) follows **effective seed** for the active route.

## Semantic color roles (Material-like)

Runtime CSS variables (`--amuse-*`) mapped to Tailwind `bg-primary`, `text-on-surface`, etc.:

`primary`, `on-primary`, `primary-container`, `on-primary-container`, `secondary`, `on-secondary`, `surface`, `on-surface`, `surface-variant`, `on-surface-variant`, `outline`, `error`, `on-error`, `background`, `on-background`.

Raw color literals are **only** allowed in `src/theme/defaultPalette.ts` and `src/app/globals.css` (focus ring defaults).

## Typography roles

Named utilities (no ad hoc `text-3xl` on pages):

`display-large`, `headline-large`, `headline-medium`, `title-large`, `title-medium`, `body-large`, `body-medium`, `label-large`, `label-medium`.

Font: Geist Sans (body), Geist Mono (code).

## Auth (web)

| Rule | Value |
|------|--------|
| Client header | `X-Amuse-Client: web` |
| Credentials | `include` (refresh cookie) |
| Access token | In-memory only (React context + module store) |
| Refresh token | HttpOnly cookie `amuse_refresh` — never read from JS |
| API base | `NEXT_PUBLIC_API_BASE_URL` (default `http://localhost:5000`) |

Endpoints used: login/password, refresh, revoke, me, personas.

Login/refresh requests include `context: { type: "listener", listenerId }` after bootstrap.

## Listener bootstrap

On authenticated B2C entry:

1. `POST /api/v1/listener/profile/ensure` (authorized).
2. `GET /api/v1/identity/personas` — must include `listener`.
3. If missing after ensure, show retry screen (no silent failure).

## Routes (phase 1)

| Route | Purpose |
|-------|---------|
| `/login` | Password login |
| `/home` | Home feed placeholder + playback/seed demo |
| `/artist/[artistId]` | Artist placeholder + page seed override |
| `/album/[albumId]` | Album placeholder + page seed override |

## Out of scope (this phase)

- Real catalog/playback APIs
- Mobile native client
- i18n, full a11y audit
- MUI component library
- Business portal (`frontend/business`)
