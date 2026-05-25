import type { ColorSeed } from "./types";

/** Lower chroma + slightly lifted lightness for faded paused UI. */
export function makePausedVariant(seed: ColorSeed): ColorSeed {
  return {
    l: clamp(seed.l + 0.06, 0, 1),
    c: seed.c * 0.45,
    h: seed.h,
  };
}

function clamp(n: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, n));
}
