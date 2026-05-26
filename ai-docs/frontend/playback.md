# Playback

Frontend playback is owned by a single `PlaybackProvider` sibling to `ThemeProvider` and
`AuthProvider`. It encapsulates:

- A single `HTMLAudioElement` instance.
- A pure reducer over a `PlaybackState` (queue, position, volume, repeat, shuffle).
- The fetch for short-lived signed stream URLs (`/api/v1/catalog/tracks/{id}/stream-info`).
- The bridge into `ThemeProvider`'s `playingSeed` / `isPaused` precedence.

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

## Side effects in the provider

The provider runs five effects, each driven by a single piece of state:

1. **Once**: create the `<audio>` element on the client (guarded against SSR). Wire up
   `timeupdate`, `ended`, and `loadedmetadata` listeners that dispatch reducer actions.
2. **On `currentTrack.id` change**: fetch `stream-info`, set `audio.src`, call `play()`
   if `isPlaying`. A failed fetch dispatches `pause`.
3. **On `isPlaying` change**: call `audio.play()` or `audio.pause()` to match state.
4. **On `volume` change**: write through to `audio.volume`.
5. **On `currentTrack` change**: seed the theme with the cover art. First applies a
   deterministic fallback (instant), then asynchronously refines with
   `extractSeedFromImage` if CORS permits.
6. **On `isPlaying` + `currentIndex` change**: set `theme.isPaused` to fade the palette
   when a track is loaded but paused.
7. **On `positionMs` change**: write through to `audio.currentTime` when the delta is
   greater than 0.5s (so we don't fight the natural `timeupdate` tick).

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

- `MiniPlayer` (`src/components/player/MiniPlayer.tsx`) — docked above `BottomNav`,
  hidden when queue is empty. Tap body → `/playing`. Tap play/next → reducer actions.
- `/playing` (`src/app/(b2c)/playing/page.tsx`) — full-screen view with seek slider,
  prev/play/pause/next, repeat, shuffle, and an "Up next" list.
- Album page tracks (`src/app/(b2c)/album/[albumId]/page.tsx`) — clicking a track row
  builds the queue from `album.tracks.filter(hasAudio)` and starts at that index.

UI primitives introduced for this slice live in `src/components/ui/`:

- `IconButton` — accessible square button for icon-only actions (variants: filled,
  tonal, outlined, ghost; sizes: sm/md/lg).
- `Slider` — themed range input for seek + future volume.
- `Skeleton` — pulsing placeholder for loading states (replaces plain "Loading…").
- `PlaybackIcons` — small set of inline SVGs (Play, Pause, Prev, Next, Repeat, Shuffle,
  ChevronDown).

Plus new `Text` variants: `headline-small`, `title-small`, `body-small`, `label-small`.

## Testing

- `src/lib/playback/__tests__/reducer.test.ts` — 14 cases covering queue ops, transport,
  boundary conditions, repeat modes, and misc helpers.
- `src/lib/playback/__tests__/formatDuration.test.ts` — 4 cases covering edge values,
  minute / hour formatting, and `0:00` for invalid input.
- Run with `pnpm test` from `frontend/consumer`. All 23 frontend tests pass.

Backend integration coverage for the stream-info endpoint lives in
`CatalogEndpointsTests` (`Stream_info_returns_signed_url_for_seeded_track`,
`Stream_info_unknown_track_returns_problem`).

## Known follow-ups

- Auto-refresh stream URLs when the audio element emits an `error` after URL expiry.
- Wire `usePlayback` into a global keyboard shortcut layer (space = toggle, ← / → =
  seek, ↑ / ↓ = volume).
- Persist queue + position to `localStorage` so a refresh keeps you where you were.
- When real cover art replaces the dev BMP gradients, ensure the MinIO bucket sends
  appropriate CORS headers so `extractSeedFromImage` can use the real canvas-sampled
  seed instead of falling back to the deterministic hash.
- Replace `<img>` with `next/image` + `images.remotePatterns` for the configured media
  host once the CORS story is firmed up.
