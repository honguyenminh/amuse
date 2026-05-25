import type { ColorSeed } from "./types";

/**
 * Try to extract a dominant OKLCH color seed from an image URL.
 *
 * Strategy:
 *  1. Load the image with `crossOrigin = "anonymous"` (server must allow CORS).
 *  2. Down-sample into a 16x16 canvas and compute an OKLab-weighted mean colour,
 *     biased toward higher-chroma pixels so muted edges do not dominate.
 *  3. Convert to OKLCH and clamp into a perceptually pleasant range for use as
 *     a `ColorSeed` (L 0.45–0.65, C 0.10–0.26).
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
  return { l: 0.55, c: 0.2, h: hue };
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
    const weight = 0.1 + chromaProxy * 2;
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

/**
 * Approximate OKLab conversion (https://bottosson.github.io/posts/oklab/).
 * Inputs are sRGB in [0,1]; output is OKLCH (l in [0,1], c ≥ 0, h in degrees).
 */
function rgbToOklch(r: number, g: number, b: number): ColorSeed {
  const lr = srgbToLinear(r);
  const lg = srgbToLinear(g);
  const lb = srgbToLinear(b);

  const lms_l = 0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb;
  const lms_m = 0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb;
  const lms_s = 0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb;

  const lp = Math.cbrt(lms_l);
  const mp = Math.cbrt(lms_m);
  const sp = Math.cbrt(lms_s);

  const L = 0.2104542553 * lp + 0.793617785 * mp - 0.0040720468 * sp;
  const A = 1.9779984951 * lp - 2.428592205 * mp + 0.4505937099 * sp;
  const B = 0.0259040371 * lp + 0.7827717662 * mp - 0.808675766 * sp;

  const c = Math.hypot(A, B);
  let h = (Math.atan2(B, A) * 180) / Math.PI;
  if (h < 0) h += 360;
  return { l: L, c, h };
}

function srgbToLinear(v: number): number {
  return v <= 0.04045 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
}

function clampSeed({ l, c, h }: ColorSeed): ColorSeed {
  return {
    l: Math.min(0.65, Math.max(0.45, l)),
    c: Math.min(0.26, Math.max(0.1, c)),
    h: ((h % 360) + 360) % 360,
  };
}
