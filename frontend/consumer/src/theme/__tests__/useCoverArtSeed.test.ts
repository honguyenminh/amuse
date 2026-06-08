import { describe, expect, it } from "vitest";
import { deterministicSeedFromString } from "@/theme/extractSeedFromImage";

describe("deterministicSeedFromString", () => {
  it("returns a clamped seed synchronously for hash fallback", () => {
    const seed = deterministicSeedFromString("https://cdn.example.com/cover.jpg");
    expect(seed.l).toBeGreaterThanOrEqual(0.42);
    expect(seed.l).toBeLessThanOrEqual(0.68);
    expect(seed.c).toBeGreaterThanOrEqual(0.14);
    expect(seed.h).toBeGreaterThanOrEqual(0);
    expect(seed.h).toBeLessThan(360);
  });
});
