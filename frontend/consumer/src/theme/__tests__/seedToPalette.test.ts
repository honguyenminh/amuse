import { describe, expect, it } from "vitest";
import { parseSeed, seedToPalette } from "../seedToPalette";
import { resolveEffectiveSeed } from "../resolveEffectiveSeed";

function hueOf(oklch: string): number {
  return Number.parseFloat(oklch.match(/oklch\([\d.]+ [\d.]+ ([\d.]+)/)?.[1] ?? "0");
}

function hueDelta(a: number, b: number): number {
  const d = Math.abs(a - b);
  return Math.min(d, 360 - d);
}

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

  it("onPrimaryContainer contrasts with primaryContainer", () => {
    const palette = seedToPalette({ l: 0.5, c: 0.2, h: 120 });
    const containerL = Number.parseFloat(
      palette.primaryContainer.match(/oklch\(([\d.]+)/)?.[1] ?? "0",
    );
    const onContainerL = Number.parseFloat(
      palette.onPrimaryContainer.match(/oklch\(([\d.]+)/)?.[1] ?? "0",
    );
    expect(Math.abs(containerL - onContainerL)).toBeGreaterThan(0.35);
  });

  it("primaryContainer hue stays near the seed", () => {
    const seed = { l: 0.55, c: 0.28, h: 285 };
    const palette = seedToPalette(seed);
    expect(hueDelta(hueOf(palette.primaryContainer), seed.h)).toBeLessThan(25);
  });

  it("includes tertiary roles", () => {
    const palette = seedToPalette({ l: 0.5, c: 0.2, h: 120 });
    expect(palette.tertiary).toMatch(/^oklch\(/);
    expect(palette.onTertiary).toMatch(/^oklch\(/);
    expect(palette.tertiaryContainer).toMatch(/^oklch\(/);
    expect(palette.onTertiaryContainer).toMatch(/^oklch\(/);
  });

  it("resolves page seed over playing seed", () => {
    const page = { l: 0.6, c: 0.2, h: 10 };
    const playing = { l: 0.4, c: 0.2, h: 200 };
    const effective = resolveEffectiveSeed({ pageSeed: page, playingSeed: playing });
    expect(effective).toEqual(page);
  });
});
