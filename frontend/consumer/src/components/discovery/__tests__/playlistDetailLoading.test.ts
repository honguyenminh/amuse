import { describe, expect, it } from "vitest";
import { shouldClientRefreshPlaylistDetail } from "../playlistDetailLoading";

describe("shouldClientRefreshPlaylistDetail", () => {
  it("refreshes for authenticated listeners", () => {
    expect(shouldClientRefreshPlaylistDetail(true, false)).toBe(true);
  });

  it("refreshes for liked collection", () => {
    expect(shouldClientRefreshPlaylistDetail(false, true)).toBe(true);
  });

  it("allows SSR snapshot for anonymous public playlist views", () => {
    expect(shouldClientRefreshPlaylistDetail(false, false)).toBe(false);
  });
});
