# Playback

Frontend playback is owned by a single `PlaybackProvider` sibling to `ThemeProvider` and
`AuthProvider`. It encapsulates:

- A single `HTMLAudioElement` instance.
- A pure reducer over a `PlaybackState` (queue, position, volume, repeat, shuffle).
- The fetch for short-lived signed stream URLs (`/api/v1/catalog/tracks/{id}/stream-info`).
- The bridge into `ThemeProvider`'s `playingSeed` / `isPaused` precedence.
- A login redirect guard: anonymous visitors can browse, but attempting to play
  bounces them through `/login?next=<current path>`.

## Provider tree

```
ThemeProvider             (palette + seed precedence)
└─ AuthProvider           (session + persona)
   └─ PlaybackProvider    (queue + audio element + theme bridge)
      └─ <app>
```

Order is intentional: playback updates the theme, which means the theme provider must be
established first so the playback effects can read/write its setters.

## State + actions

`src/lib/playback/types.ts`:

```ts
type PlaybackTrack = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  artistId: string;
  artistName: string;
  releaseId: string;
  releaseTitle: string;
  coverArtUrl: string | null;
};

type PlaybackState = {
  queue: PlaybackTrack[];
  currentIndex: number;   // -1 when empty
  isPlaying: boolean;
  positionMs: number;
  durationMs: number;
  volume: number;         // 0..1
  repeat: "off" | "queue" | "one";
  shuffle: boolean;
};
```

The reducer (`src/lib/playback/reducer.ts`) is **pure**. All DOM side effects (audio
element src, play/pause, theme bridge) live in the provider and react to state changes
via `useEffect`. This keeps the reducer fully unit-testable with Vitest, see
`__tests__/reducer.test.ts`.

### Notable behaviours

- `previous` restarts the current track when more than 3 seconds in (Spotify/Apple-style),
  otherwise moves to the previous queue position.
- `trackEnded` with `repeat = "one"` restarts the same track; with `repeat = "queue"` it
  wraps at the end; otherwise it stops at the end of the queue.
- `toggle` is a no-op when no track is loaded (queue empty).
- `setVolume` clamps to [0, 1].

## Auth gating

The browse surface (`/home`, `/release/[id]`, `/artist/[id]`) is fully anonymous; the
public catalog endpoints don't require a bearer token. Playback is the wall:

- `playQueue(...)` checks for an access token before dispatching anything. If absent it
  pushes `/login?next=<encoded current path>` and returns without mutating the queue.
- The `currentTrack.id`-change effect that fetches `stream-info` catches `401` /
  `auth.not_authenticated` from `authFetch` and does the same redirect, additionally
  dispatching `{ type: "clear" }` so the MiniPlayer hides immediately rather than showing
  a track stuck in an indeterminate state.

A consequence: the visitor's intended queue is lost across the round-trip. Recovering it
is a follow-up once we wire session persistence to `localStorage`.

## Smooth scrubbing

Two visible bugs in the previous implementation both came from the same root cause —
the slider component was bound to `state.positionMs`, which only ticks at the audio
element's ~4 Hz `timeupdate` cadence — combined with a 100 ms CSS transition on the
slider fill that visibly trailed the native thumb during drags.

The fix has three parts:

1. **`step={1}` on the Slider** (`Slider.tsx` + `/playing` + MiniPlayer). The full slider
   reports millisecond-precision values so any pointer position is a valid commit.
2. **No CSS transition on the fill** (`Slider.tsx`). The colored bar updates synchronously
   with the input's `value` change, so it can never trail the thumb.
3. **`usePlaybackPosition()` hook** (`PlaybackContext.tsx`). While `isPlaying` is true a
   `requestAnimationFrame` loop reads `audio.currentTime` directly into a local `useState`,
   re-rendering only the components that subscribe (currently `MiniPlayer` and `/playing`)
   at ~60 Hz. The reducer's `state.positionMs` continues to tick at the slower cadence and
   is used for non-visual concerns (e.g. `previous` boundary detection).

While the user is dragging the thumb, the parent component overrides the displayed value
with a local `scrubMs` state populated from `onScrubStart`/`onChange`. Audio element
push-back (rAF or `timeupdate`) can't fight the user because the slider isn't reading
either source during scrubbing. On `onScrubEnd` the final value is committed via
`seek(positionMs)`, `scrubMs` is cleared, and the audio sync effect (>0.5 s delta) seeks
the audio element to the chosen position.

## Side effects in the provider

The provider runs these effects, each driven by a single piece of state:

1. **Once**: create the `<audio>` element on the client (guarded against SSR). Wire up
   `timeupdate`, `ended`, and `loadedmetadata` listeners that dispatch reducer actions.
2. **On `currentTrack.id` change**: fetch `stream-info`, set `audio.src`, call `play()`
   if `isPlaying`. A failed fetch dispatches `pause`; a 401 additionally dispatches
   `clear` and redirects to `/login?next=...`.
3. **On `isPlaying` change**: call `audio.play()` or `audio.pause()` to match state.
4. **On `volume` change**: write through to `audio.volume`.
5. **On `currentTrack` change**: seed the theme with the cover art. First applies a
   deterministic fallback (instant), then asynchronously refines with
   `extractSeedFromImage` if CORS permits.
6. **On `isPlaying` + `currentIndex` change**: set `theme.isPaused` to fade the palette
   when a track is loaded but paused.
7. **On `positionMs` change**: write through to `audio.currentTime` when the delta is
   greater than 0.5 s (so we don't fight the natural `timeupdate` tick).

## Stream URL lifecycle

```text
PlaybackProvider              Catalog API                      MinIO
       │                           │                              │
       │  GET /tracks/{id}/stream  │                              │
       ├──────────────────────────▶│                              │
       │                           │  GetSignedUrl (30 min TTL)   │
       │                           ├─────────────────────────────▶│
       │                           │◀── signed URL                │
       │◀── { url, contentType, durationMs, expiresAt }            │
       │                                                          │
audio.src = url                                                   │
audio.play()                                                      │
       ├──────────────────────────── GET (Range)  ───────────────▶│
       │◀──── audio bytes ────────────────────────────────────────│
```

Signed URLs expire (30 minutes by default). If a track is paused at minute 29 and
resumed at minute 31 the audio element will start emitting errors; the current strategy
is "tolerate the failure, user reloads the track". A future improvement: re-fetch
`stream-info` automatically when an `error` event fires.

## Theme bridge

`ThemeProvider` resolves the active palette from:

```
pageSeed > playingSeed > defaultSeed   then optionally → makePausedVariant if paused
```

`PlaybackProvider` writes to `playingSeed` and `isPaused`. The `/playing` route writes
to `pageSeed` (which trumps `playingSeed`) so the full-screen player gets the cover
applied directly without indirection.

## UI surfaces

- `MiniPlayer` (`src/components/player/MiniPlayer.tsx`) — full-width docked player at
  the bottom of the viewport. Hidden when queue is empty. Tap body → `/playing`.
  Prev / play-pause / next inline. Interactive seek slider, with time-elapsed /
  total-duration text on `sm+` widths.
- `/playing` (`src/app/(b2c)/playing/page.tsx`) — full-screen view with hero cover art,
  seek slider, prev/play/pause/next, repeat, shuffle, and an "Up next" list. The layout
  shifts from stacked (mobile) to two-column (md+) so the cover doesn't dominate larger
  viewports.
- Release page tracks (`src/app/(b2c)/release/[releaseId]/page.tsx`) — clicking a track
  row calls `playQueue(release.tracks.filter(hasAudio), index)`. On an anonymous session
  this triggers the login redirect documented above.

UI primitives introduced earlier and still in use:

- `IconButton` — accessible square button for icon-only actions (variants: filled,
  tonal, outlined, ghost; sizes: sm/md/lg).
- `Slider` — themed range input with `onScrubStart`/`onScrubEnd` hooks for the playback
  scrubbing pattern.
- `Skeleton` — pulsing placeholder for loading states.
- `PlaybackIcons` — small set of inline SVGs (Play, Pause, Prev, Next, Repeat, Shuffle,
  ChevronDown).

## Testing

- `src/lib/playback/__tests__/reducer.test.ts` — 14 cases covering queue ops, transport,
  boundary conditions, repeat modes, and misc helpers.
- `src/lib/playback/__tests__/formatDuration.test.ts` — 4 cases covering edge values,
  minute / hour formatting, and `0:00` for invalid input.
- Run with `pnpm test` from `frontend/consumer`. All 23 frontend tests pass.

Backend integration coverage for the stream-info endpoint lives in
`CatalogEndpointsTests` (`Stream_info_returns_signed_url_for_seeded_track`,
`Stream_info_requires_authentication`, `Stream_info_unknown_track_returns_problem`).

## Known follow-ups

- Auto-refresh stream URLs when the audio element emits an `error` after URL expiry.
- Wire `usePlayback` into a global keyboard shortcut layer (space = toggle, ← / → =
  seek, ↑ / ↓ = volume).
- Persist queue + position to `localStorage` so a refresh keeps you where you were —
  this also unlocks restoring the visitor's intended queue after the auth redirect.
- When real cover art replaces the dev BMP gradients, ensure the MinIO bucket sends
  appropriate CORS headers so `extractSeedFromImage` can use the real canvas-sampled
  seed instead of falling back to the deterministic hash.
- Replace `<img>` with `next/image` + `images.remotePatterns` for the configured media
  host once the CORS story is firmed up.
