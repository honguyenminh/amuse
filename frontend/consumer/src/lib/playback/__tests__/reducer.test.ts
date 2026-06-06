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
});
