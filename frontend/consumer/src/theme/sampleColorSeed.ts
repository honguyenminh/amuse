import { rgbToOklch } from "./colorConvert";
import type { ColorSeed } from "./types";

export function clampColorSeed({ l, c, h }: ColorSeed): ColorSeed {
  return {
    l: Math.min(0.68, Math.max(0.42, l)),
    c: Math.min(0.38, Math.max(0.14, c)),
    h: ((h % 360) + 360) % 360,
  };
}

export function colorSeedFromWeightedRgba(
  data: Uint8Array | Uint8ClampedArray,
  channels = 4,
): ColorSeed {
  let weightedR = 0;
  let weightedG = 0;
  let weightedB = 0;
  let totalWeight = 0;

  for (let i = 0; i < data.length; i += channels) {
    const r = data[i] / 255;
    const g = data[i + 1] / 255;
    const b = data[i + 2] / 255;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    const chromaProxy = max - min;
    const weight = 0.08 + chromaProxy * 3.5;
    weightedR += r * weight;
    weightedG += g * weight;
    weightedB += b * weight;
    totalWeight += weight;
  }

  return clampColorSeed(
    rgbToOklch(weightedR / totalWeight, weightedG / totalWeight, weightedB / totalWeight),
  );
}
