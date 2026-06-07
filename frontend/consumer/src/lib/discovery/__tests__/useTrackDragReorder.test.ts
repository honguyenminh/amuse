import { describe, expect, it } from "vitest";
import { computeReorderTargetIndex } from "../useTrackDragReorder";

describe("computeReorderTargetIndex", () => {
  it("returns null for a no-op move", () => {
    expect(computeReorderTargetIndex(2, 2)).toBeNull();
    expect(computeReorderTargetIndex(2, 3)).toBeNull();
  });

  it("moves an item up in the list", () => {
    expect(computeReorderTargetIndex(3, 1)).toBe(1);
  });

  it("moves an item down in the list", () => {
    expect(computeReorderTargetIndex(1, 4)).toBe(3);
  });

  it("supports inserting at the end", () => {
    expect(computeReorderTargetIndex(1, 5)).toBe(4);
  });
});
