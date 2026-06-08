import { describe, expect, it } from "vitest";
import {
  catalogReleaseByIdHref,
  catalogReleaseHref,
  catalogReleasePathFromEdition,
  catalogReleasePathFromSummary,
} from "../paths";

describe("catalogReleaseHref", () => {
  it("appends a title query for instant TopBar chrome during client navigation", () => {
    expect(
      catalogReleaseHref("aurora-lights", "midnight-drive", { title: "Midnight Drive" }),
    ).toBe("/artist/aurora-lights/release/midnight-drive?title=Midnight+Drive");
  });

  it("leaves canonical paths unchanged when no title hint is provided", () => {
    expect(catalogReleaseHref("aurora-lights", "midnight-drive")).toBe(
      "/artist/aurora-lights/release/midnight-drive",
    );
  });
});

describe("catalogReleaseByIdHref", () => {
  it("supports legacy id routes with optional title hints", () => {
    expect(catalogReleaseByIdHref("rel-1", { title: "Midnight Drive" })).toBe(
      "/release/rel-1?title=Midnight+Drive",
    );
  });
});

describe("catalogReleasePathFromSummary", () => {
  it("includes release title in navigation hrefs", () => {
    expect(
      catalogReleasePathFromSummary({
        id: "rel-1",
        slug: "midnight-drive",
        artistSlug: "aurora-lights",
        title: "Midnight Drive",
        releaseType: "album",
        releaseDate: "2024-01-01T00:00:00Z",
        coverArtUrl: null,
      }),
    ).toContain("title=Midnight+Drive");
  });
});

describe("catalogReleasePathFromEdition", () => {
  it("includes edition title in navigation hrefs", () => {
    expect(
      catalogReleasePathFromEdition("aurora-lights", {
        id: "rel-2",
        slug: "midnight-drive-deluxe",
        title: "Midnight Drive (Deluxe)",
        releaseType: "album",
        releaseDate: "2024-06-01T00:00:00Z",
        coverArtUrl: null,
      }),
    ).toContain("title=Midnight+Drive+%28Deluxe%29");
  });
});
