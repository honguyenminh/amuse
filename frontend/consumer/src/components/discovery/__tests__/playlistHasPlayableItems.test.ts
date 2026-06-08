import { describe, expect, it } from "vitest";
import { playlistHasPlayableItems } from "../PlaylistDetailView";

describe("playlistHasPlayableItems", () => {
  it("returns false when items are missing or empty", () => {
    expect(playlistHasPlayableItems(undefined)).toBe(false);
    expect(playlistHasPlayableItems([])).toBe(false);
  });

  it("returns true when any item has audio", () => {
    expect(
      playlistHasPlayableItems([
        {
          itemId: "1",
          trackId: "t1",
          position: 1,
          title: "A",
          artistName: "Artist",
          durationMs: 1000,
          hasAudio: false,
          coverArtUrl: null,
          releaseId: "r1",
          releaseTitle: "Release",
        },
        {
          itemId: "2",
          trackId: "t2",
          position: 2,
          title: "B",
          artistName: "Artist",
          durationMs: 1000,
          hasAudio: true,
          coverArtUrl: null,
          releaseId: "r1",
          releaseTitle: "Release",
        },
      ]),
    ).toBe(true);
  });

  it("returns false when no items have audio", () => {
    expect(
      playlistHasPlayableItems([
        {
          itemId: "1",
          trackId: "t1",
          position: 1,
          title: "A",
          artistName: "Artist",
          durationMs: 1000,
          hasAudio: false,
          coverArtUrl: null,
          releaseId: "r1",
          releaseTitle: "Release",
        },
      ]),
    ).toBe(false);
  });
});
