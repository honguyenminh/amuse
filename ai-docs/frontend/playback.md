# Playback

Frontend playback is owned by a single `PlaybackProvider` sibling to `ThemeProvider` and
`AuthProvider`. It encapsulates:

- A single `HTMLAudioElement` wrapped by `createPlaybackOutput()` (gain / volume fades).
- A pure reducer over a `PlaybackState` (queue, position, volume, repeat, shuffle).
- The fetch for playback discovery (`/api/v1/catalog/tracks/{id}/stream-info`) — returns a **relative** DASH manifest path when ready, or `catalog.track_stream_not_ready` until the worker sets `audio_stream_key`.
- **DASH playback** via `dashjs` (`src/lib/playback/dashPlayer.ts`): manifest + catalog-backed segment URLs are resolved against `NEXT_PUBLIC_API_BASE_URL` (`resolveApiUrl`); dash.js **v5** uses `addRequestInterceptor` to attach `Authorization` + `X-Amuse-Client` (the old `RequestModifier` extension is a no-op in v5).
- **Output path** (`src/lib/playback/playbackOutput.ts`): optional Web Audio `GainNode` for short fade-in on play and fade-out before pause (reduces clicks); pause restores `currentTime` after fade so the seekbar does not drift. **Volume normalization** multiplies user volume by per-track gain from `stream-info.loudness.linearGainLu` when enabled in settings (`normalizationGain.ts`, `refreshPlaybackSettings()` on toggle).
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
   `requestAnimationFrame` loop reads `audio.currentTime` into local `useState` for the
   slider (~60 Hz). On **resume after pause**, the first frame seeds from reducer
   `positionMs` so the bar does not jump ahead of the frozen pause position. While paused,
   the slider follows `state.positionMs`. `timeupdate` reducer ticks are suppressed while
   `!isPlaying` so the pause fade cannot advance the stored position.

While the user is dragging the thumb, the parent component overrides the displayed value
with a local `scrubMs` state populated from `onScrubStart`/`onChange`. Audio element
push-back (rAF or `timeupdate`) can't fight the user because the slider isn't reading
either source during scrubbing. On `onScrubEnd` the final value is committed via
`seek(positionMs)`, `scrubMs` is cleared, and the audio sync effect seeks the element via
`syncAudioTime` (see `syncAudioTime.ts`).

## Side effects in the provider

The provider runs these effects, each driven by a single piece of state:

1. **Once**: create playback output + `HTMLAudioElement` on the client. Wire `timeupdate`
   (dispatches `tick` only while playing), `ended`, `loadedmetadata`.
2. **On `currentTrack.id` change**: fetch `stream-info`, attach DASH (`attachDashToAudio`) or set `audio.src` for non-DASH; reset DASH session when switching tracks.
3. **On `isPlaying` change**: `output.playSmooth()` / `output.pauseSmooth()` — not raw
   `audio.play`/`pause` alone (gain ramps).
4. **On `volume` change**: `output.setVolume`.
5. **On `currentTrack` change**: theme `playingSeed` bridge (unchanged).
6. **On `isPlaying` + `currentIndex`**: theme paused palette.
7. **Seek / scrub**: `syncAudioTime` + `pauseImmediate` on the output path when jumping position.

## Stream URL lifecycle (DASH)

```text
PlaybackProvider          Catalog API (auth)              Object storage (MinIO / R2)
       │                         │                                    │
       │  GET .../stream-info    │                                    │
       ├────────────────────────▶│  returns { url: "/api/.../manifest.mpd", contentType } │
       │                         │                                    │
       │  dashjs GET manifest    │  reads MPD from bucket (GetAsync)  │
       ├────────────────────────▶│◀──────────────────────────────────│
       │                         │                                    │
       │  dashjs GET segment     │  302 → presigned GET URL           │
       ├────────────────────────▶├───────────────────────────────────▶│
       │  browser follows redirect (bytes from storage / CDN)         │
```

Configure `NEXT_PUBLIC_API_BASE_URL` in `frontend/consumer` so relative `/api/...` paths resolve to the real API host (not the Next.js origin).

Signed **segment** URLs expire (see `Media:SignedUrlMinutes`). Re-fetch `stream-info` / restart track if playback errors after long pause — same class of problem as expiring a single signed file URL.

## Legacy direct file URL (non-DASH)

If the API ever returned a fully qualified presigned URL to a single file, the client would set `audio.crossOrigin = "anonymous"` and `audio.src = url`. The **catalog path today is DASH-only** once a stream exists; masters are not exposed for listener playback via `stream-info`.

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

## Volume normalization

Settings (`/settings`, `playbackSettings.volumeNormalization`, default **on**):

| Toggle | Playback |
|--------|----------|
| **On** | `computeNormalizationGain(info.loudness, true)` → `playbackOutput.setNormalizationGain` (Web Audio can exceed 1.0; fallback `audio.volume` is clamped to 1.0) |
| **Off** | Gain multiplier `1` — original DASH levels (worker no longer bakes loudnorm into renditions) |

`stream-info` must include `loudness.linearGainLu` (populated after worker analyze pass).
Toggling normalization calls `refreshPlaybackSettings()` without reloading the track.

Verify after a fresh dev seed + worker run: compare the same track with normalization on vs off.

## Testing

- `src/lib/playback/__tests__/reducer.test.ts` — queue ops, transport, repeat, etc.
- `src/lib/playback/__tests__/formatDuration.test.ts` — duration formatting.
- `src/lib/playback/__tests__/normalizationGain.test.ts` — per-track gain helper.
- Run with `pnpm test` from `frontend/consumer`.

Backend integration: `CatalogEndpointsTests` includes `stream-info` expectations for **DASH-only** behavior (`Stream_info_returns_track_stream_not_ready_until_ingested`, auth, unknown track).

## Known follow-ups

- Auto-refresh / re-attach when `stream-info` URLs or signed segment redirects expire (listen for `error` on the media element).
- Wire `usePlayback` into a global keyboard shortcut layer (space = toggle, ← / → =
  seek, ↑ / ↓ = volume).
- Persist queue + position to `localStorage` so a refresh keeps you where you were —
  this also unlocks restoring the visitor's intended queue after the auth redirect.
- When real cover art replaces the dev BMP gradients, ensure the MinIO bucket sends
  appropriate CORS headers so `extractSeedFromImage` can use the real canvas-sampled
  seed instead of falling back to the deterministic hash.
- Replace `<img>` with `next/image` + `images.remotePatterns` for the configured media
  host once the CORS story is firmed up.
- Token refresh on **long** DASH sessions: today interceptors read the token at request time; if access JWT expires mid-playback, consider refreshing inside the interceptor or re-attaching the source after refresh.
