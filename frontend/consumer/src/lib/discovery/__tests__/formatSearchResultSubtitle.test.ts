import { describe, expect, it } from "vitest";
import {
  formatPlaylistSearchSubtitle,
  formatSearchItemSubtitle,
  searchItemKindLabel,
} from "../formatSearchResultSubtitle";

describe("searchItemKindLabel", () => {
  it("maps known catalog kinds", () => {
    expect(searchItemKindLabel("artist")).toBe("Artist");
    expect(searchItemKindLabel("release")).toBe("Release");
    expect(searchItemKindLabel("track")).toBe("Track");
    expect(searchItemKindLabel("playlist")).toBe("Playlist");
  });
});

describe("formatSearchItemSubtitle", () => {
  it("shows only the type when there is no detail", () => {
    expect(formatSearchItemSubtitle("artist", null)).toBe("Artist");
  });

  it("prefixes detail with the type", () => {
    expect(formatSearchItemSubtitle("release", "Artist Name")).toBe(
      "Release · Artist Name",
    );
    expect(formatSearchItemSubtitle("track", "Artist Name — Album Title")).toBe(
      "Track · Artist Name — Album Title",
    );
  });
});

describe("formatPlaylistSearchSubtitle", () => {
  it("includes playlist type, owner, and track count", () => {
    expect(formatPlaylistSearchSubtitle("Alex", 1)).toBe("Playlist · Alex · 1 track");
    expect(formatPlaylistSearchSubtitle("Alex", 3)).toBe("Playlist · Alex · 3 tracks");
  });
});
