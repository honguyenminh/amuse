import {
  argbFromRgb,
  blueFromArgb,
  greenFromArgb,
  redFromArgb,
} from "@material/material-color-utilities";
import type { ColorSeed } from "./types";

function clamp(n: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, n));
}

function normalizeHue(h: number): number {
  const x = h % 360;
  return x < 0 ? x + 360 : x;
}

export function formatOklch(l: number, c: number, h: number): string {
  return `oklch(${clamp(l, 0, 1).toFixed(3)} ${Math.max(0, c).toFixed(3)} ${normalizeHue(h).toFixed(1)})`;
}

export function parseOklch(value: string): ColorSeed | null {
  const match = value.match(/oklch\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)/i);
  if (!match) return null;
  return {
    l: Number.parseFloat(match[1]),
    c: Number.parseFloat(match[2]),
    h: Number.parseFloat(match[3]),
  };
}

function srgbToLinear(v: number): number {
  return v <= 0.04045 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
}

function linearToSrgb(v: number): number {
  return v <= 0.0031308 ? v * 12.92 : 1.055 * Math.pow(v, 1 / 2.4) - 0.055;
}

/** sRGB channels in [0, 1] → OKLCH seed. */
export function rgbToOklch(r: number, g: number, b: number): ColorSeed {
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

function oklchToRgb(seed: ColorSeed): { r: number; g: number; b: number } {
  const hRad = (seed.h * Math.PI) / 180;
  const a = seed.c * Math.cos(hRad);
  const b = seed.c * Math.sin(hRad);

  const lp = seed.l + 0.3963377774 * a + 0.2158037573 * b;
  const mp = seed.l - 0.1055613458 * a - 0.0638541728 * b;
  const sp = seed.l - 0.0894841775 * a - 1.291485548 * b;

  const lms_l = lp ** 3;
  const lms_m = mp ** 3;
  const lms_s = sp ** 3;

  const lr = 4.0767416621 * lms_l - 3.3077115913 * lms_m + 0.2309699292 * lms_s;
  const lg = -1.2684380046 * lms_l + 2.6097574011 * lms_m - 0.3413193965 * lms_s;
  const lb = -0.0041960863 * lms_l - 0.7034196147 * lms_m + 1.707614701 * lms_s;

  return {
    r: linearToSrgb(lr),
    g: linearToSrgb(lg),
    b: linearToSrgb(lb),
  };
}

export function colorSeedToArgb(seed: ColorSeed): number {
  const { r, g, b } = oklchToRgb(seed);
  return argbFromRgb(
    Math.round(clamp(r, 0, 1) * 255),
    Math.round(clamp(g, 0, 1) * 255),
    Math.round(clamp(b, 0, 1) * 255),
  );
}

export function argbToOklch(argb: number): string {
  const seed = rgbToOklch(
    redFromArgb(argb) / 255,
    greenFromArgb(argb) / 255,
    blueFromArgb(argb) / 255,
  );
  return formatOklch(seed.l, seed.c, seed.h);
}
