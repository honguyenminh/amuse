import type { PlaybackAction, PlaybackState } from "./types";

/**
 * Pure reducer for the playback state machine. Side effects (audio element src,
 * fetching stream-info, theme bridge) live in the provider; this function is
 * fully testable on its own.
 *
 * Conventions:
 * - `currentIndex` is -1 when the queue is empty; the reducer never leaves a
 *   non-empty queue with an out-of-range index.
 * - `durationMs` defaults to the track's catalog duration; the provider may
 *   refine it via `tick` once the audio element reports a measured value.
 * - `previous` re-starts the current track when more than 3 seconds in, matching
 *   the convention of most music apps.
 */
export function playbackReducer(state: PlaybackState, action: PlaybackAction): PlaybackState {
  switch (action.type) {
    case "playQueue": {
      if (action.tracks.length === 0) return { ...state, queue: [], currentIndex: -1, isPlaying: false, positionMs: 0, durationMs: 0 };
      const start = clampIndex(action.startIndex ?? 0, action.tracks.length);
      const track = action.tracks[start]!;
      return {
        ...state,
        queue: action.tracks,
        currentIndex: start,
        isPlaying: true,
        positionMs: 0,
        durationMs: track.durationMs,
      };
    }

    case "play":
      return state.currentIndex < 0 ? state : { ...state, isPlaying: true };

    case "pause":
      return { ...state, isPlaying: false };

    case "toggle":
      return state.currentIndex < 0 ? state : { ...state, isPlaying: !state.isPlaying };

    case "next":
      return advance(state, +1);

    case "previous": {
      if (state.currentIndex < 0) return state;
      if (state.positionMs > 3000) return { ...state, positionMs: 0 };
      return advance(state, -1);
    }

    case "seek":
      return { ...state, positionMs: Math.max(0, action.positionMs) };

    case "tick": {
      if (state.currentIndex < 0) return state;
      const duration = action.durationMs && action.durationMs > 0 ? action.durationMs : state.durationMs;
      return { ...state, positionMs: action.positionMs, durationMs: duration };
    }

    case "trackEnded":
      return advance(state, +1, { onEnded: true });

    case "setVolume":
      return { ...state, volume: Math.max(0, Math.min(1, action.volume)) };

    case "setRepeat":
      return { ...state, repeat: action.mode };

    case "toggleShuffle":
      return { ...state, shuffle: !state.shuffle };

    case "clear":
      return { ...state, queue: [], currentIndex: -1, isPlaying: false, positionMs: 0, durationMs: 0 };

    default:
      return state;
  }
}

function clampIndex(index: number, length: number): number {
  if (length === 0) return -1;
  if (index < 0) return 0;
  if (index >= length) return length - 1;
  return index;
}

type AdvanceOptions = { onEnded?: boolean };

function advance(state: PlaybackState, delta: number, opts: AdvanceOptions = {}): PlaybackState {
  if (state.currentIndex < 0 || state.queue.length === 0) return state;

  // Repeat one when the track ends naturally — manual next/prev still moves on.
  if (opts.onEnded && state.repeat === "one") {
    return { ...state, positionMs: 0, isPlaying: true };
  }

  const next = state.currentIndex + delta;

  if (next < 0) {
    return { ...state, positionMs: 0 };
  }

  if (next >= state.queue.length) {
    if (state.repeat === "queue") {
      const track = state.queue[0]!;
      return {
        ...state,
        currentIndex: 0,
        positionMs: 0,
        durationMs: track.durationMs,
        isPlaying: true,
      };
    }
    return { ...state, isPlaying: false, positionMs: 0 };
  }

  const track = state.queue[next]!;
  return {
    ...state,
    currentIndex: next,
    positionMs: 0,
    durationMs: track.durationMs,
    isPlaying: true,
  };
}
