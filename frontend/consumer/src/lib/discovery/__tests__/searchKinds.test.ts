import { describe, expect, it } from "vitest";
import {
  DEFAULT_SEARCH_KINDS,
  appendSearchKindsParams,
  isAllSearchKindsSelected,
  searchKindsForRequest,
  toggleSearchKind,
} from "../searchKinds";

describe("toggleSearchKind", () => {
  it("adds a kind when it is not selected", () => {
    expect(toggleSearchKind(["artist"], "track")).toEqual(["artist", "track"]);
  });

  it("removes a kind when it is selected", () => {
    expect(toggleSearchKind(["artist", "track"], "track")).toEqual(["artist"]);
  });

  it("resets to all kinds when the last chip is deselected", () => {
    expect(toggleSearchKind(["artist"], "artist")).toEqual(DEFAULT_SEARCH_KINDS);
  });
});

describe("searchKindsForRequest", () => {
  it("omits kinds when all are selected", () => {
    expect(searchKindsForRequest(DEFAULT_SEARCH_KINDS)).toBeUndefined();
  });

  it("returns the subset when filtered", () => {
    expect(searchKindsForRequest(["artist", "playlist"])).toEqual(["artist", "playlist"]);
  });
});

describe("appendSearchKindsParams", () => {
  it("appends repeated kinds params", () => {
    const params = new URLSearchParams({ q: "alpha" });
    appendSearchKindsParams(params, ["artist", "track"]);

    expect(params.getAll("kinds")).toEqual(["artist", "track"]);
  });
});

describe("isAllSearchKindsSelected", () => {
  it("returns true for the default set", () => {
    expect(isAllSearchKindsSelected(DEFAULT_SEARCH_KINDS)).toBe(true);
  });
});
