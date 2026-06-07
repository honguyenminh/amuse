import { describe, expect, it } from "vitest";
import type { PlaylistSummaryDto } from "@/lib/api/types";
import {
  activePlaylistLibraryFilterCount,
  applyPlaylistLibraryFilters,
  DEFAULT_PLAYLIST_LIBRARY_FILTERS,
  isDefaultPlaylistLibraryFilters,
  playlistLibraryFilterSummary,
} from "../playlistLibraryFilters";

function playlist(
  overrides: Partial<PlaylistSummaryDto> & Pick<PlaylistSummaryDto, "id" | "title">,
): PlaylistSummaryDto {
  return {
    kind: "user",
    description: null,
    visibility: "private",
    trackCount: 1,
    updatedAt: "2026-01-01T00:00:00Z",
    owner: null,
    forkedFromPlaylistId: null,
    isOwned: false,
    isSaved: false,
    isFollowed: false,
    isDeletable: true,
    coverArtUrls: [],
    ...overrides,
  };
}

describe("playlistLibraryFilters", () => {
  const items = [
    playlist({ id: "1", title: "Mine private", isOwned: true, visibility: "private" }),
    playlist({ id: "2", title: "Mine public", isOwned: true, visibility: "public" }),
    playlist({
      id: "3",
      title: "Followed public",
      isFollowed: true,
      visibility: "public",
      owner: { listenerProfileId: "o1", displayName: "DJ", avatarUrl: null },
    }),
    playlist({
      id: "4",
      title: "Saved only",
      isSaved: true,
      visibility: "public",
      owner: { listenerProfileId: "o2", displayName: "Sam", avatarUrl: null },
    }),
  ];

  it("filters by ownership", () => {
    expect(
      applyPlaylistLibraryFilters(items, { ownership: "mine", visibility: "all" }).map(
        (p) => p.id,
      ),
    ).toEqual(["1", "2"]);
    expect(
      applyPlaylistLibraryFilters(items, { ownership: "following", visibility: "all" }).map(
        (p) => p.id,
      ),
    ).toEqual(["3"]);
  });

  it("filters by visibility", () => {
    expect(
      applyPlaylistLibraryFilters(items, { ownership: "all", visibility: "public" }).map(
        (p) => p.id,
      ),
    ).toEqual(["2", "3", "4"]);
  });

  it("combines ownership and visibility filters", () => {
    expect(
      applyPlaylistLibraryFilters(items, { ownership: "mine", visibility: "public" }).map(
        (p) => p.id,
      ),
    ).toEqual(["2"]);
  });

  it("reports active filter state", () => {
    expect(isDefaultPlaylistLibraryFilters(DEFAULT_PLAYLIST_LIBRARY_FILTERS)).toBe(true);
    expect(
      activePlaylistLibraryFilterCount({ ownership: "following", visibility: "private" }),
    ).toBe(2);
    expect(playlistLibraryFilterSummary({ ownership: "mine", visibility: "public" })).toBe(
      "Mine · Public",
    );
  });
});
