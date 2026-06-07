import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  clearPersistedQueue,
  loadPersistedQueue,
  savePersistedQueue,
  snapshotFromPlaybackState,
  type PersistedQueueSnapshot,
} from "../queuePersistence";
import type { PlaybackTrack } from "../types";

const T = (id: string): PlaybackTrack => ({
  id,
  title: `t-${id}`,
  trackNumber: 1,
  durationMs: 180_000,
  artistId: "a1",
  artistName: "Artist",
  artistSlug: "artist",
  releaseId: "r1",
  releaseTitle: "Release",
  releaseSlug: "release",
  coverArtUrl: null,
});

const baseSnapshot = (): PersistedQueueSnapshot => ({
  version: 1,
  queue: [T("a"), T("b")],
  playOrder: [0, 1],
  playOrderIndex: 0,
  currentIndex: 0,
  positionMs: 12_000,
  durationMs: 180_000,
  shuffle: false,
  repeat: "off",
  updatedAt: 1,
  activeTabId: null,
  isPlaying: false,
});

function installStorageMock() {
  const store = new Map<string, string>();
  vi.stubGlobal("window", {
    localStorage: {
      getItem: (key: string) => store.get(key) ?? null,
      setItem: (key: string, value: string) => {
        store.set(key, value);
      },
      removeItem: (key: string) => {
        store.delete(key);
      },
    },
  });
  return store;
}

describe("queuePersistence", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
    installStorageMock();
  });

  it("round-trips a valid snapshot through localStorage", () => {
    const snapshot = baseSnapshot();
    savePersistedQueue(snapshot);
    expect(loadPersistedQueue()).toEqual(snapshot);
    clearPersistedQueue();
    expect(loadPersistedQueue()).toBeNull();
  });

  it("rejects corrupt snapshots", () => {
    window.localStorage.setItem("amuse.consumer.playbackQueue.v1", JSON.stringify({ version: 2 }));
    expect(loadPersistedQueue()).toBeNull();
    clearPersistedQueue();
  });

  it("builds snapshots from playback state", () => {
    const snapshot = snapshotFromPlaybackState(
      {
        queue: [T("a")],
        playOrder: [0],
        playOrderIndex: 0,
        currentIndex: 0,
        positionMs: 0,
        durationMs: 180_000,
        shuffle: false,
        repeat: "off",
        isPlaying: true,
      },
      { activeTabId: "tab-1", isPlaying: true },
      42,
    );
    expect(snapshot?.updatedAt).toBe(42);
    expect(snapshot?.activeTabId).toBe("tab-1");
  });

  it("accepts discovery tracks with empty artistId", () => {
    const track = { ...T("a"), artistId: "", artistSlug: "" };
    const snapshot = {
      ...baseSnapshot(),
      queue: [track],
      playOrder: [0],
    };
    savePersistedQueue(snapshot);
    expect(loadPersistedQueue()?.queue[0]?.artistId).toBe("");
  });
});
