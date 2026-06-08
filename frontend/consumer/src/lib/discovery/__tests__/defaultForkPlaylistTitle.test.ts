import { describe, expect, it } from "vitest";
import {
  defaultForkPlaylistTitle,
  MAX_PLAYLIST_TITLE_LENGTH,
} from "../defaultForkPlaylistTitle";

describe("defaultForkPlaylistTitle", () => {
  it("appends (fork) to the source title", () => {
    expect(defaultForkPlaylistTitle("Summer hits")).toBe("Summer hits (fork)");
  });

  it("trims whitespace from the source title", () => {
    expect(defaultForkPlaylistTitle("  Mix  ")).toBe("Mix (fork)");
  });

  it("truncates long titles so the result fits the max length", () => {
    const longTitle = "a".repeat(MAX_PLAYLIST_TITLE_LENGTH);
    const result = defaultForkPlaylistTitle(longTitle);
    expect(result.endsWith(" (fork)")).toBe(true);
    expect(result.length).toBe(MAX_PLAYLIST_TITLE_LENGTH);
  });
});
