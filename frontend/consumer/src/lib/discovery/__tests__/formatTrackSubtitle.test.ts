import { describe, expect, it } from "vitest";
import { formatTrackSubtitle } from "../formatTrackSubtitle";

describe("formatTrackSubtitle", () => {
  it("joins artist and release with middle dot when both are present", () => {
    expect(formatTrackSubtitle("Artist Name", "Release Title")).toBe(
      "Artist Name · Release Title",
    );
  });

  it("returns artist only when release is missing", () => {
    expect(formatTrackSubtitle("Artist Name", null)).toBe("Artist Name");
    expect(formatTrackSubtitle("Artist Name", undefined)).toBe("Artist Name");
    expect(formatTrackSubtitle("Artist Name", "")).toBe("Artist Name");
  });

  it("returns release only when artist is missing", () => {
    expect(formatTrackSubtitle(null, "Release Title")).toBe("Release Title");
    expect(formatTrackSubtitle(undefined, "Release Title")).toBe("Release Title");
    expect(formatTrackSubtitle("", "Release Title")).toBe("Release Title");
  });

  it("returns null when both are missing or whitespace", () => {
    expect(formatTrackSubtitle(null, null)).toBeNull();
    expect(formatTrackSubtitle(undefined, undefined)).toBeNull();
    expect(formatTrackSubtitle("", "")).toBeNull();
    expect(formatTrackSubtitle("  ", "  ")).toBeNull();
  });

  it("trims whitespace from both fields", () => {
    expect(formatTrackSubtitle("  Artist  ", "  Release  ")).toBe("Artist · Release");
  });
});
