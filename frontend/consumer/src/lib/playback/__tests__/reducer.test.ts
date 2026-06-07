import { describe, expect, it } from "vitest";
import { playbackReducer } from "../reducer";
import { initialPlaybackState, type PlaybackTrack } from "../types";

const T = (id: string, trackNumber = 1, durationMs = 180_000): PlaybackTrack => ({
  id,
  title: `t-${id}`,
  trackNumber,
  durationMs,
  artistId: "a1",
  artistName: "Artist",
  artistSlug: "artist",
  releaseId: "r1",
  releaseTitle: "Release",
  releaseSlug: "release",
  coverArtUrl: null,
});

describe("playbackReducer", () => {
  describe("playQueue", () => {
    it("loads tracks and starts at the requested index", () => {
      const next = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 1,
      });
      expect(next.queue).toHaveLength(3);
      expect(next.currentIndex).toBe(1);
      expect(next.isPlaying).toBe(true);
      expect(next.positionMs).toBe(0);
      expect(next.durationMs).toBe(180_000);
    });

    it("clamps an out-of-range start index", () => {
      const next = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2)],
        startIndex: 99,
      });
      expect(next.currentIndex).toBe(1);
    });

    it("an empty queue clears state", () => {
      const next = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [],
      });
      expect(next.currentIndex).toBe(-1);
      expect(next.isPlaying).toBe(false);
    });
  });

  describe("transport", () => {
    const seeded = playbackReducer(initialPlaybackState, {
      type: "playQueue",
      tracks: [T("a"), T("b", 2), T("c", 3)],
    });

    it("toggle flips isPlaying", () => {
      const paused = playbackReducer(seeded, { type: "toggle" });
      expect(paused.isPlaying).toBe(false);
      const resumed = playbackReducer(paused, { type: "toggle" });
      expect(resumed.isPlaying).toBe(true);
    });

    it("toggle is a no-op when nothing is queued", () => {
      const next = playbackReducer(initialPlaybackState, { type: "toggle" });
      expect(next.isPlaying).toBe(false);
      expect(next.currentIndex).toBe(-1);
    });

    it("next advances to the following track", () => {
      const next = playbackReducer(seeded, { type: "next" });
      expect(next.currentIndex).toBe(1);
      expect(next.positionMs).toBe(0);
    });

    it("previous re-starts the track when more than 3 seconds in", () => {
      const withPosition = playbackReducer(
        { ...seeded, currentIndex: 1, positionMs: 5000 },
        { type: "previous" },
      );
      expect(withPosition.currentIndex).toBe(1);
      expect(withPosition.positionMs).toBe(0);
    });

    it("previous moves to the prior track when within 3 seconds", () => {
      const atSecond = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 1,
      });
      const withPosition = playbackReducer(
        { ...atSecond, positionMs: 1500 },
        { type: "previous" },
      );
      expect(withPosition.currentIndex).toBe(0);
    });
  });

  describe("queue boundaries", () => {
    const seeded = playbackReducer(initialPlaybackState, {
      type: "playQueue",
      tracks: [T("a"), T("b", 2)],
      startIndex: 1,
    });

    it("next at the end stops playback when repeat is off", () => {
      const next = playbackReducer(seeded, { type: "next" });
      expect(next.currentIndex).toBe(1);
      expect(next.isPlaying).toBe(false);
    });

    it("next at the end wraps when repeat=queue", () => {
      const withRepeat = playbackReducer(seeded, { type: "setRepeat", mode: "queue" });
      const next = playbackReducer(withRepeat, { type: "next" });
      expect(next.currentIndex).toBe(0);
      expect(next.isPlaying).toBe(true);
    });

    it("trackEnded with repeat=one restarts the same track", () => {
      const withRepeat = playbackReducer(seeded, { type: "setRepeat", mode: "one" });
      const next = playbackReducer(withRepeat, { type: "trackEnded" });
      expect(next.currentIndex).toBe(1);
      expect(next.positionMs).toBe(0);
      expect(next.isPlaying).toBe(true);
    });
  });

  describe("misc", () => {
    it("setVolume clamps to [0,1]", () => {
      const low = playbackReducer(initialPlaybackState, { type: "setVolume", volume: -5 });
      expect(low.volume).toBe(0);
      const high = playbackReducer(initialPlaybackState, { type: "setVolume", volume: 4 });
      expect(high.volume).toBe(1);
    });

    it("clear empties the queue", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a")],
      });
      const next = playbackReducer(seeded, { type: "clear" });
      expect(next.currentIndex).toBe(-1);
      expect(next.queue).toEqual([]);
    });

    it("toggleShuffle flips the flag", () => {
      const on = playbackReducer(initialPlaybackState, { type: "toggleShuffle" });
      expect(on.shuffle).toBe(true);
    });
  });

  describe("appendToQueue", () => {
    it("appends tracks and starts playback when queue was empty", () => {
      const next = playbackReducer(initialPlaybackState, {
        type: "appendToQueue",
        tracks: [T("a"), T("b", 2)],
      });
      expect(next.queue).toHaveLength(2);
      expect(next.currentIndex).toBe(0);
      expect(next.isPlaying).toBe(true);
      expect(next.playOrder).toEqual([0, 1]);
    });

    it("skips duplicate track ids", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a")],
      });
      const next = playbackReducer(seeded, {
        type: "appendToQueue",
        tracks: [T("a"), T("b", 2)],
      });
      expect(next.queue).toHaveLength(2);
      expect(next.queue[1]!.id).toBe("b");
    });
  });

  describe("insertPlayNext", () => {
    it("inserts after the current track in play order", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      const next = playbackReducer(seeded, {
        type: "insertPlayNext",
        tracks: [T("x", 4, 60_000)],
      });
      expect(next.queue).toHaveLength(4);
      expect(next.playOrder[1]).toBe(3);
      expect(next.currentIndex).toBe(0);
    });
  });

  describe("shuffle advance", () => {
    it("next follows playOrder when shuffle is on", () => {
      const seeded = playbackReducer(
        { ...initialPlaybackState, shuffle: true },
        {
          type: "playQueue",
          tracks: [T("a"), T("b", 2), T("c", 3)],
          startIndex: 1,
        },
      );
      expect(seeded.playOrder[0]).toBe(1);
      expect(seeded.currentIndex).toBe(1);
      const next = playbackReducer(seeded, { type: "next" });
      expect(next.playOrderIndex).toBe(1);
      expect(next.currentIndex).toBe(seeded.playOrder[1]);
    });
  });

  describe("jumpToPlayOrderIndex", () => {
    it("jumps to a track in the play order and starts from the beginning", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      const next = playbackReducer(seeded, { type: "jumpToPlayOrderIndex", playOrderIndex: 2 });
      expect(next.playOrderIndex).toBe(2);
      expect(next.currentIndex).toBe(2);
      expect(next.positionMs).toBe(0);
      expect(next.isPlaying).toBe(true);
    });

    it("restarts the current track when selecting the same index", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2)],
        startIndex: 0,
      });
      const midTrack = playbackReducer(seeded, { type: "seek", positionMs: 90_000 });
      const next = playbackReducer(midTrack, { type: "jumpToPlayOrderIndex", playOrderIndex: 0 });
      expect(next.positionMs).toBe(0);
      expect(next.isPlaying).toBe(true);
    });
  });

  describe("reorderPlayOrder", () => {
    it("moves an upcoming track and keeps the current track pinned", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3), T("d", 4)],
        startIndex: 0,
      });
      const next = playbackReducer(seeded, {
        type: "reorderPlayOrder",
        fromPlayOrderIndex: 3,
        toPlayOrderIndex: 1,
      });
      expect(next.playOrder).toEqual([0, 3, 1, 2]);
      expect(next.playOrderIndex).toBe(0);
      expect(next.currentIndex).toBe(0);
      expect(next.shuffle).toBe(false);
    });

    it("updates playOrderIndex when the current track is moved down", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      const next = playbackReducer(seeded, {
        type: "reorderPlayOrder",
        fromPlayOrderIndex: 0,
        toPlayOrderIndex: 2,
      });
      expect(next.playOrder).toEqual([1, 2, 0]);
      expect(next.playOrderIndex).toBe(2);
      expect(next.currentIndex).toBe(0);
    });

    it("ignores reordering into played history", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      const advanced = playbackReducer(seeded, { type: "next" });
      expect(advanced.playOrderIndex).toBe(1);
      const next = playbackReducer(advanced, {
        type: "reorderPlayOrder",
        fromPlayOrderIndex: 2,
        toPlayOrderIndex: 0,
      });
      expect(next.playOrder).toEqual(advanced.playOrder);
    });
  });

  describe("moveToPlayNext", () => {
    it("moves an upcoming track directly after the current track", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3), T("d", 4)],
        startIndex: 0,
      });
      const next = playbackReducer(seeded, { type: "moveToPlayNext", trackId: "d" });
      expect(next.playOrder).toEqual([0, 3, 1, 2]);
      expect(next.playOrderIndex).toBe(0);
      expect(next.currentIndex).toBe(0);
    });

    it("moves a played track back to play next", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      const advanced = playbackReducer(seeded, { type: "next" });
      const next = playbackReducer(advanced, { type: "moveToPlayNext", trackId: "a" });
      expect(next.playOrder).toEqual([1, 0, 2]);
      expect(next.playOrderIndex).toBe(0);
      expect(next.currentIndex).toBe(1);
    });

    it("no-ops when the track is already playing or up next", () => {
      const seeded = playbackReducer(initialPlaybackState, {
        type: "playQueue",
        tracks: [T("a"), T("b", 2), T("c", 3)],
        startIndex: 0,
      });
      expect(playbackReducer(seeded, { type: "moveToPlayNext", trackId: "a" })).toBe(seeded);
      expect(playbackReducer(seeded, { type: "moveToPlayNext", trackId: "b" })).toBe(seeded);
    });
  });

  describe("restoreState", () => {
    it("restores queue fields and applies caller-controlled isPlaying", () => {
      const next = playbackReducer(initialPlaybackState, {
        type: "restoreState",
        isPlaying: false,
        snapshot: {
          version: 1,
          queue: [T("a"), T("b", 2)],
          playOrder: [0, 1],
          playOrderIndex: 1,
          currentIndex: 1,
          positionMs: 45_000,
          durationMs: 180_000,
          shuffle: true,
          repeat: "queue",
          updatedAt: 99,
          activeTabId: "tab-1",
          isPlaying: true,
        },
      });
      expect(next.queue).toHaveLength(2);
      expect(next.currentIndex).toBe(1);
      expect(next.playOrderIndex).toBe(1);
      expect(next.positionMs).toBe(45_000);
      expect(next.shuffle).toBe(true);
      expect(next.repeat).toBe("queue");
      expect(next.isPlaying).toBe(false);
    });
  });
});
