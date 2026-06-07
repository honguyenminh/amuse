import { describe, expect, it } from "vitest";
import { isPublicBrowsePath } from "@/lib/routing/publicBrowsePaths";

describe("isPublicBrowsePath", () => {
  it("matches catalog and discovery browse routes", () => {
    expect(isPublicBrowsePath("/home")).toBe(true);
    expect(isPublicBrowsePath("/artist/asu")).toBe(true);
    expect(isPublicBrowsePath("/artist/asu/release/wholesale")).toBe(true);
    expect(isPublicBrowsePath("/artist/asu/release-group/wholesale")).toBe(true);
    expect(isPublicBrowsePath("/release/019e9fac-28e1-7ac2-9791-c7cd7366751a")).toBe(true);
    expect(isPublicBrowsePath("/playlist/019e9fac-28e1-7ac2-9791-c7cd7366751a")).toBe(true);
    expect(isPublicBrowsePath("/search")).toBe(true);
    expect(isPublicBrowsePath("/search?q=test")).toBe(true);
    expect(isPublicBrowsePath("/hashtag/jpop")).toBe(true);
  });

  it("does not match authenticated-only routes", () => {
    expect(isPublicBrowsePath("/library")).toBe(false);
    expect(isPublicBrowsePath("/settings")).toBe(false);
    expect(isPublicBrowsePath("/onboarding")).toBe(false);
    expect(isPublicBrowsePath("/playing")).toBe(false);
  });
});
