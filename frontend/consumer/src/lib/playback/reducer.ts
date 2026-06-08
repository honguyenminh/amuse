import type { PlaybackAction, PlaybackState, PlaybackTrack } from "./types";

/**
 * Pure reducer for the playback state machine. Side effects (audio element src,
 * fetching stream-info, theme bridge) live in the provider; this function is
 * fully testable on its own.
 */
export function playbackReducer(state: PlaybackState, action: PlaybackAction): PlaybackState {
  switch (action.type) {
    case "playQueue": {
      if (action.tracks.length === 0) {
        return emptyQueueState(state);
      }
      const start = clampIndex(action.startIndex ?? 0, action.tracks.length);
      const playOrder = state.shuffle
        ? shuffledOrderKeepingFirst(action.tracks.length, start)
        : identityOrder(action.tracks.length);
      const playOrderIndex = state.shuffle ? 0 : start;
      const currentIndex = playOrder[playOrderIndex]!;
      const track = action.tracks[currentIndex]!;
      return {
        ...state,
        queue: action.tracks,
        playOrder,
        playOrderIndex,
        currentIndex,
        isPlaying: true,
        positionMs: 0,
        durationMs: track.durationMs,
      };
    }

    case "appendToQueue": {
      const toAdd = dedupeTracks(action.tracks, state.queue);
      if (toAdd.length === 0) return state;
      const queue = [...state.queue, ...toAdd];
      const startIdx = state.queue.length;
      const newIndices = toAdd.map((_, i) => startIdx + i);
      let playOrder = [...state.playOrder];
      if (state.shuffle) {
        playOrder = [...playOrder, ...shuffleIndices(newIndices)];
      } else {
        playOrder = [...playOrder, ...newIndices];
      }
      if (state.currentIndex < 0) {
        const track = queue[playOrder[0]!]!;
        return {
          ...state,
          queue,
          playOrder,
          playOrderIndex: 0,
          currentIndex: playOrder[0]!,
          isPlaying: true,
          positionMs: 0,
          durationMs: track.durationMs,
        };
      }
      return { ...state, queue, playOrder };
    }

    case "insertPlayNext": {
      const toAdd = dedupeTracks(action.tracks, state.queue);
      if (toAdd.length === 0) return state;
      const queue = [...state.queue, ...toAdd];
      const startIdx = state.queue.length;
      const newIndices = toAdd.map((_, i) => startIdx + i);
      let playOrder: number[];
      let playOrderIndex = state.playOrderIndex;
      let currentIndex = state.currentIndex;
      let isPlaying = state.isPlaying;
      let positionMs = state.positionMs;
      let durationMs = state.durationMs;

      if (state.currentIndex < 0) {
        playOrder = state.shuffle ? shuffleIndices(queue.map((_, i) => i)) : identityOrder(queue.length);
        playOrderIndex = 0;
        currentIndex = playOrder[0]!;
        const track = queue[currentIndex]!;
        isPlaying = true;
        positionMs = 0;
        durationMs = track.durationMs;
      } else {
        playOrder = [...state.playOrder];
        const insertAt = playOrderIndex + 1;
        if (state.shuffle) {
          playOrder.splice(insertAt, 0, ...shuffleIndices(newIndices));
        } else {
          playOrder.splice(insertAt, 0, ...newIndices);
        }
      }
      return {
        ...state,
        queue,
        playOrder,
        playOrderIndex,
        currentIndex,
        isPlaying,
        positionMs,
        durationMs,
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

    case "setVolume": {
      if (!Number.isFinite(action.volume)) return state;
      return { ...state, volume: Math.max(0, Math.min(1, action.volume)) };
    }

    case "setRepeat":
      return { ...state, repeat: action.mode };

    case "setShuffle":
      return applyShuffle(state, action.enabled);

    case "toggleShuffle":
      return applyShuffle(state, !state.shuffle);

    case "clear":
      return emptyQueueState(state);

    case "jumpToPlayOrderIndex": {
      const target = action.playOrderIndex;
      if (target < 0 || target >= state.playOrder.length) return state;
      if (target === state.playOrderIndex) {
        return { ...state, positionMs: 0, isPlaying: true };
      }
      const queueIndex = state.playOrder[target]!;
      const track = state.queue[queueIndex];
      if (!track) return state;
      return {
        ...state,
        playOrderIndex: target,
        currentIndex: queueIndex,
        positionMs: 0,
        durationMs: track.durationMs,
        isPlaying: true,
      };
    }

    case "reorderPlayOrder": {
      const { fromPlayOrderIndex, toPlayOrderIndex } = action;
      if (fromPlayOrderIndex === toPlayOrderIndex) return state;
      if (fromPlayOrderIndex < state.playOrderIndex) return state;
      if (toPlayOrderIndex < state.playOrderIndex) return state;
      if (
        fromPlayOrderIndex < 0 ||
        toPlayOrderIndex < 0 ||
        fromPlayOrderIndex >= state.playOrder.length ||
        toPlayOrderIndex >= state.playOrder.length
      ) {
        return state;
      }

      const playOrder = [...state.playOrder];
      const [moved] = playOrder.splice(fromPlayOrderIndex, 1);
      if (moved === undefined) return state;
      playOrder.splice(toPlayOrderIndex, 0, moved);

      const playOrderIndex = playOrder.indexOf(state.currentIndex);
      if (playOrderIndex < 0) return state;

      return {
        ...state,
        playOrder,
        playOrderIndex,
        shuffle: false,
      };
    }

    case "moveToPlayNext": {
      if (state.currentIndex < 0 || state.playOrder.length === 0) return state;

      let fromPlayOrderIndex = -1;
      for (let i = 0; i < state.playOrder.length; i++) {
        const queueIndex = state.playOrder[i]!;
        if (state.queue[queueIndex]?.id === action.trackId) {
          fromPlayOrderIndex = i;
          break;
        }
      }
      if (fromPlayOrderIndex < 0) return state;

      const targetPlayOrderIndex = state.playOrderIndex + 1;
      if (
        fromPlayOrderIndex === state.playOrderIndex ||
        fromPlayOrderIndex === targetPlayOrderIndex
      ) {
        return state;
      }

      const playOrder = [...state.playOrder];
      const [moved] = playOrder.splice(fromPlayOrderIndex, 1);
      if (moved === undefined) return state;

      let playOrderIndex = state.playOrderIndex;
      if (fromPlayOrderIndex < playOrderIndex) {
        playOrderIndex -= 1;
      }

      playOrder.splice(playOrderIndex + 1, 0, moved);

      return {
        ...state,
        playOrder,
        playOrderIndex,
        shuffle: false,
      };
    }

    case "removeFromQueue": {
      if (state.currentIndex < 0 || state.playOrder.length === 0) return state;

      let fromPlayOrderIndex = -1;
      for (let i = 0; i < state.playOrder.length; i++) {
        const queueIndex = state.playOrder[i]!;
        if (state.queue[queueIndex]?.id === action.trackId) {
          fromPlayOrderIndex = i;
          break;
        }
      }
      if (fromPlayOrderIndex < 0) return state;

      const queueIndexToRemove = state.playOrder[fromPlayOrderIndex]!;
      const isRemovingCurrent = fromPlayOrderIndex === state.playOrderIndex;
      const playOrder = state.playOrder.filter((_, i) => i !== fromPlayOrderIndex);
      const queue = state.queue.filter((_, i) => i !== queueIndexToRemove);
      const adjustedPlayOrder = playOrder.map((idx) =>
        idx > queueIndexToRemove ? idx - 1 : idx,
      );

      if (adjustedPlayOrder.length === 0) {
        return emptyQueueState(state);
      }

      if (isRemovingCurrent) {
        if (fromPlayOrderIndex >= adjustedPlayOrder.length) {
          const newPlayOrderIndex = adjustedPlayOrder.length - 1;
          const newCurrentIndex = adjustedPlayOrder[newPlayOrderIndex]!;
          const track = queue[newCurrentIndex]!;
          return {
            ...state,
            queue,
            playOrder: adjustedPlayOrder,
            playOrderIndex: newPlayOrderIndex,
            currentIndex: newCurrentIndex,
            positionMs: 0,
            durationMs: track.durationMs,
            isPlaying: false,
            shuffle: false,
          };
        }

        const newCurrentIndex = adjustedPlayOrder[fromPlayOrderIndex]!;
        const track = queue[newCurrentIndex]!;
        return {
          ...state,
          queue,
          playOrder: adjustedPlayOrder,
          playOrderIndex: fromPlayOrderIndex,
          currentIndex: newCurrentIndex,
          positionMs: 0,
          durationMs: track.durationMs,
          isPlaying: state.isPlaying,
          shuffle: false,
        };
      }

      let newPlayOrderIndex = state.playOrderIndex;
      if (fromPlayOrderIndex < state.playOrderIndex) {
        newPlayOrderIndex -= 1;
      }

      let newCurrentIndex = state.currentIndex;
      if (queueIndexToRemove < state.currentIndex) {
        newCurrentIndex -= 1;
      }

      return {
        ...state,
        queue,
        playOrder: adjustedPlayOrder,
        playOrderIndex: newPlayOrderIndex,
        currentIndex: newCurrentIndex,
        shuffle: false,
      };
    }

    case "restoreState": {
      const { snapshot, isPlaying } = action;
      if (snapshot.queue.length === 0) return emptyQueueState(state);
      return {
        ...state,
        queue: snapshot.queue,
        playOrder: snapshot.playOrder,
        playOrderIndex: snapshot.playOrderIndex,
        currentIndex: snapshot.currentIndex,
        positionMs: snapshot.positionMs,
        durationMs: snapshot.durationMs,
        shuffle: snapshot.shuffle,
        repeat: snapshot.repeat,
        isPlaying,
      };
    }

    default:
      return state;
  }
}

function emptyQueueState(state: PlaybackState): PlaybackState {
  return {
    ...state,
    queue: [],
    currentIndex: -1,
    playOrder: [],
    playOrderIndex: -1,
    isPlaying: false,
    positionMs: 0,
    durationMs: 0,
  };
}

function clampIndex(index: number, length: number): number {
  if (length === 0) return -1;
  if (index < 0) return 0;
  if (index >= length) return length - 1;
  return index;
}

function identityOrder(length: number): number[] {
  return Array.from({ length }, (_, i) => i);
}

function shuffleIndices(indices: number[]): number[] {
  const order = [...indices];
  for (let i = order.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [order[i], order[j]] = [order[j]!, order[i]!];
  }
  return order;
}

/** Shuffle all indices but keep `startIndex` at play-order position 0. */
function shuffledOrderKeepingFirst(length: number, startIndex: number): number[] {
  const rest = identityOrder(length).filter((i) => i !== startIndex);
  return [startIndex, ...shuffleIndices(rest)];
}

function dedupeTracks(incoming: PlaybackTrack[], queue: PlaybackTrack[]): PlaybackTrack[] {
  const existing = new Set(queue.map((t) => t.id));
  return incoming.filter((t) => !existing.has(t.id));
}

function applyShuffle(state: PlaybackState, enabled: boolean): PlaybackState {
  if (state.queue.length === 0) {
    return { ...state, shuffle: enabled };
  }
  if (enabled === state.shuffle) return state;

  if (!enabled) {
    const currentIndex = state.currentIndex;
    return {
      ...state,
      shuffle: false,
      playOrder: identityOrder(state.queue.length),
      playOrderIndex: currentIndex,
      currentIndex,
    };
  }

  const playOrder = shuffleIndices(identityOrder(state.queue.length));
  const currentQueueIndex = state.currentIndex;
  let playOrderIndex = playOrder.indexOf(currentQueueIndex);
  if (playOrderIndex < 0) playOrderIndex = 0;

  return {
    ...state,
    shuffle: true,
    playOrder,
    playOrderIndex,
    currentIndex: playOrder[playOrderIndex]!,
  };
}

type AdvanceOptions = { onEnded?: boolean };

function advance(state: PlaybackState, delta: number, opts: AdvanceOptions = {}): PlaybackState {
  if (state.currentIndex < 0 || state.queue.length === 0 || state.playOrder.length === 0) {
    return state;
  }

  if (opts.onEnded && state.repeat === "one") {
    return { ...state, positionMs: 0, isPlaying: true };
  }

  const nextPlayOrderIndex = state.playOrderIndex + delta;

  if (nextPlayOrderIndex < 0) {
    return { ...state, positionMs: 0 };
  }

  if (nextPlayOrderIndex >= state.playOrder.length) {
    if (state.repeat === "queue") {
      const queueIndex = state.playOrder[0]!;
      const track = state.queue[queueIndex]!;
      return {
        ...state,
        playOrderIndex: 0,
        currentIndex: queueIndex,
        positionMs: 0,
        durationMs: track.durationMs,
        isPlaying: true,
      };
    }
    return { ...state, isPlaying: false, positionMs: 0 };
  }

  const queueIndex = state.playOrder[nextPlayOrderIndex]!;
  const track = state.queue[queueIndex]!;
  return {
    ...state,
    playOrderIndex: nextPlayOrderIndex,
    currentIndex: queueIndex,
    positionMs: 0,
    durationMs: track.durationMs,
    isPlaying: true,
  };
}
