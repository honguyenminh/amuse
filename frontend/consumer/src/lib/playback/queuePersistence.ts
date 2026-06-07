import type { PlaybackTrack, RepeatMode } from "./types";

export type PersistedQueueSnapshot = {
  version: 1;
  queue: PlaybackTrack[];
  playOrder: number[];
  playOrderIndex: number;
  currentIndex: number;
  positionMs: number;
  durationMs: number;
  shuffle: boolean;
  repeat: RepeatMode;
  updatedAt: number;
  activeTabId: string | null;
  isPlaying: boolean;
};

export const QUEUE_STORAGE_KEY = "amuse.consumer.playbackQueue.v1";

const TRACK_FIELDS: (keyof PlaybackTrack)[] = [
  "id",
  "title",
  "trackNumber",
  "durationMs",
  "artistId",
  "artistName",
  "artistSlug",
  "releaseId",
  "releaseTitle",
  "releaseSlug",
  "coverArtUrl",
];

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.length > 0;
}

function isPlaybackTrack(value: unknown): value is PlaybackTrack {
  if (!value || typeof value !== "object") return false;
  const track = value as Record<string, unknown>;
  for (const field of TRACK_FIELDS) {
    if (field === "coverArtUrl") {
      if (track[field] !== null && typeof track[field] !== "string") return false;
      continue;
    }
    if (field === "trackNumber" || field === "durationMs") {
      if (typeof track[field] !== "number" || !Number.isFinite(track[field])) return false;
      continue;
    }
    // Slugs/ids may be empty for discovery-sourced tracks; require string type only.
    if (field === "artistId" || field === "artistSlug" || field === "releaseSlug") {
      if (typeof track[field] !== "string") return false;
      continue;
    }
    if (!isNonEmptyString(track[field])) return false;
  }
  return true;
}

function isRepeatMode(value: unknown): value is RepeatMode {
  return value === "off" || value === "queue" || value === "one";
}

function validateSnapshot(raw: unknown): PersistedQueueSnapshot | null {
  if (!raw || typeof raw !== "object") return null;
  const data = raw as Partial<PersistedQueueSnapshot>;

  if (data.version !== 1) return null;
  if (!Array.isArray(data.queue) || data.queue.length === 0) return null;
  if (!data.queue.every(isPlaybackTrack)) return null;
  if (!Array.isArray(data.playOrder) || data.playOrder.length === 0) return null;

  const queueLength = data.queue.length;
  const playOrder = data.playOrder;
  if (!playOrder.every((idx) => typeof idx === "number" && idx >= 0 && idx < queueLength)) {
    return null;
  }
  if (playOrder.length !== queueLength) return null;

  if (typeof data.playOrderIndex !== "number" || !Number.isFinite(data.playOrderIndex)) {
    return null;
  }
  if (data.playOrderIndex < -1 || data.playOrderIndex >= playOrder.length) return null;

  if (typeof data.currentIndex !== "number" || !Number.isFinite(data.currentIndex)) {
    return null;
  }
  if (data.currentIndex < -1 || data.currentIndex >= queueLength) return null;

  if (data.playOrderIndex >= 0) {
    const expected = playOrder[data.playOrderIndex];
    if (expected !== data.currentIndex) return null;
  } else if (data.currentIndex !== -1) {
    return null;
  }

  if (typeof data.positionMs !== "number" || !Number.isFinite(data.positionMs) || data.positionMs < 0) {
    return null;
  }
  if (typeof data.durationMs !== "number" || !Number.isFinite(data.durationMs) || data.durationMs < 0) {
    return null;
  }
  if (typeof data.shuffle !== "boolean") return null;
  if (!isRepeatMode(data.repeat)) return null;
  if (typeof data.updatedAt !== "number" || !Number.isFinite(data.updatedAt)) return null;
  if (data.activeTabId !== null && typeof data.activeTabId !== "string") return null;
  if (typeof data.isPlaying !== "boolean") return null;

  return {
    version: 1,
    queue: data.queue,
    playOrder,
    playOrderIndex: data.playOrderIndex,
    currentIndex: data.currentIndex,
    positionMs: data.positionMs,
    durationMs: data.durationMs,
    shuffle: data.shuffle,
    repeat: data.repeat,
    updatedAt: data.updatedAt,
    activeTabId: data.activeTabId ?? null,
    isPlaying: data.isPlaying,
  };
}

export function loadPersistedQueue(): PersistedQueueSnapshot | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(QUEUE_STORAGE_KEY);
    if (!raw) return null;
    return validateSnapshot(JSON.parse(raw));
  } catch {
    return null;
  }
}

export function savePersistedQueue(snapshot: PersistedQueueSnapshot): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(QUEUE_STORAGE_KEY, JSON.stringify(snapshot));
}

export function clearPersistedQueue(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(QUEUE_STORAGE_KEY);
}

export function snapshotFromPlaybackState(
  state: {
    queue: PlaybackTrack[];
    playOrder: number[];
    playOrderIndex: number;
    currentIndex: number;
    positionMs: number;
    durationMs: number;
    shuffle: boolean;
    repeat: RepeatMode;
    isPlaying: boolean;
  },
  lease: { activeTabId: string | null; isPlaying: boolean },
  updatedAt = Date.now(),
): PersistedQueueSnapshot | null {
  if (state.queue.length === 0) return null;
  return {
    version: 1,
    queue: state.queue,
    playOrder: state.playOrder,
    playOrderIndex: state.playOrderIndex,
    currentIndex: state.currentIndex,
    positionMs: state.positionMs,
    durationMs: state.durationMs,
    shuffle: state.shuffle,
    repeat: state.repeat,
    updatedAt,
    activeTabId: lease.activeTabId,
    isPlaying: lease.isPlaying,
  };
}

export function playbackStateFromSnapshot(
  snapshot: PersistedQueueSnapshot,
  isPlaying: boolean,
  volume: number,
): {
  queue: PlaybackTrack[];
  playOrder: number[];
  playOrderIndex: number;
  currentIndex: number;
  positionMs: number;
  durationMs: number;
  shuffle: boolean;
  repeat: RepeatMode;
  isPlaying: boolean;
  volume: number;
} {
  return {
    queue: snapshot.queue,
    playOrder: snapshot.playOrder,
    playOrderIndex: snapshot.playOrderIndex,
    currentIndex: snapshot.currentIndex,
    positionMs: snapshot.positionMs,
    durationMs: snapshot.durationMs,
    shuffle: snapshot.shuffle,
    repeat: snapshot.repeat,
    isPlaying,
    volume,
  };
}
