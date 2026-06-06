import { describe, expect, it } from "vitest";
import { parseOklch } from "../colorConvert";
import { seedToPalette } from "../seedToPalette";

function chromaOf(oklch: string): number {
  return parseOklch(oklch)?.c ?? 0;
}

describe("makePausedPalette via seedToPalette", () => {
  const seed = { l: 0.55, c: 0.28, h: 285 };

  it("paused palette differs from playing even when seed chroma is unchanged", () => {
    const playing = seedToPalette(seed, { paused: false });
    const paused = seedToPalette(seed, { paused: true });
    expect(paused.primaryContainer).not.toBe(playing.primaryContainer);
    expect(paused.surface).not.toBe(playing.surface);
  });

  it("lowers chroma on accent roles when paused", () => {
    const playing = seedToPalette(seed, { paused: false });
    const paused = seedToPalette(seed, { paused: true });
    expect(chromaOf(paused.primaryContainer)).toBeLessThan(
      chromaOf(playing.primaryContainer) * 0.85,
    );
    expect(chromaOf(paused.primary)).toBeLessThan(chromaOf(playing.primary) * 0.85);
  });

  it("leaves text foreground colors unchanged when paused", () => {
    const playing = seedToPalette(seed, { paused: false });
    const paused = seedToPalette(seed, { paused: true });
    expect(paused.onSurface).toBe(playing.onSurface);
    expect(paused.onPrimaryContainer).toBe(playing.onPrimaryContainer);
    expect(paused.onBackground).toBe(playing.onBackground);
  });
});
