import { rgbToOklch } from "./colorConvert";
import type { ColorSeed } from "./types";

/**
 * Try to extract a dominant OKLCH color seed from an image URL.
 *
 * Strategy:
 *  1. Load the image with `crossOrigin = "anonymous"` (server must allow CORS).
 *  2. Down-sample into a 16x16 canvas and compute an OKLab-weighted mean colour,
 *     biased toward higher-chroma pixels so muted edges do not dominate.
 *  3. Convert to OKLCH and clamp into a range suited for M3 expressive schemes
 *     (L 0.42–0.68, C 0.14–0.38).
 *
 * On any failure (CORS, network, decode) the returned promise resolves to `null`
 * and the caller can fall back to a deterministic seed.
 */
export async function extractSeedFromImage(url: string): Promise<ColorSeed | null> {
  if (typeof window === "undefined") return null;
  try {
    const img = await loadImage(url);
    const { r, g, b } = sampleWeightedMean(img);
    return clampSeed(rgbToOklch(r, g, b));
  } catch {
    return null;
  }
}

/** Stable hash-based fallback when CORS or load failures prevent extraction. */
export function deterministicSeedFromString(input: string): ColorSeed {
  let hash = 2166136261;
  for (let i = 0; i < input.length; i++) {
    hash ^= input.charCodeAt(i);
    hash = (hash * 16777619) >>> 0;
  }
  const hue = hash % 360;
  return { l: 0.55, c: 0.28, h: hue };
}

function loadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = "anonymous";
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error("image load failed"));
    img.src = url;
  });
}

function sampleWeightedMean(img: HTMLImageElement): {
  r: number;
  g: number;
  b: number;
} {
  const size = 16;
  const canvas = document.createElement("canvas");
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("canvas unavailable");
  ctx.drawImage(img, 0, 0, size, size);
  const data = ctx.getImageData(0, 0, size, size).data;

  let weightedR = 0;
  let weightedG = 0;
  let weightedB = 0;
  let totalWeight = 0;

  for (let i = 0; i < data.length; i += 4) {
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

  return {
    r: weightedR / totalWeight,
    g: weightedG / totalWeight,
    b: weightedB / totalWeight,
  };
}

function clampSeed({ l, c, h }: ColorSeed): ColorSeed {
  return {
    l: Math.min(0.68, Math.max(0.42, l)),
    c: Math.min(0.38, Math.max(0.14, c)),
    h: ((h % 360) + 360) % 360,
  };
}
