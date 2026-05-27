import { describe, expect, it } from "vitest";
import { makePausedVariant } from "../makePausedVariant";
import { parseSeed, seedToPalette } from "../seedToPalette";
import { resolveEffectiveSeed } from "../resolveEffectiveSeed";

describe("seedToPalette", () => {
  it("parses oklch string", () => {
    const seed = parseSeed("oklch(0.5 0.2 120)");
    expect(seed).toEqual({ l: 0.5, c: 0.2, h: 120 });
  });

  it("builds semantic roles", () => {
    const palette = seedToPalette({ l: 0.5, c: 0.2, h: 120 });
    expect(palette.primary).toMatch(/^oklch\(/);
    expect(palette.onPrimary).toMatch(/^oklch\(/);
    expect(palette.surface).toMatch(/^oklch\(/);
  });

  it("onPrimaryContainer is dark when container lightness is mid-high", () => {
    const palette = seedToPalette({ l: 0.5, c: 0.2, h: 120 });
    // Container L = 0.62 — must not pick light-on-light (old threshold bug at exactly 0.62).
    expect(palette.onPrimaryContainer).toMatch(/oklch\(0\.1/);
  });

  it("paused variant lowers chroma", () => {
    const base = { l: 0.5, c: 0.3, h: 90 };
    const paused = makePausedVariant(base);
    expect(paused.c).toBeLessThan(base.c);
  });

  it("resolves page seed over playing seed", () => {
    const page = { l: 0.6, c: 0.2, h: 10 };
    const playing = { l: 0.4, c: 0.2, h: 200 };
    const effective = resolveEffectiveSeed({ pageSeed: page, playingSeed: playing });
    expect(effective).toEqual(page);
  });
});
