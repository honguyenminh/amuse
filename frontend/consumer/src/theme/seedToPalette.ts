import type { ColorSeed, SemanticPalette } from "./types";

function oklch(l: number, c: number, h: number): string {
  return `oklch(${clamp(l, 0, 1).toFixed(3)} ${Math.max(0, c).toFixed(3)} ${normalizeHue(h).toFixed(1)})`;
}

function clamp(n: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, n));
}

function normalizeHue(h: number): number {
  const x = h % 360;
  return x < 0 ? x + 360 : x;
}

function contrastOn(l: number, h: number): string {
  return l > 0.62 ? oklch(0.18, 0.02, h) : oklch(0.97, 0.01, h);
}

/**
 * Builds a Material-like semantic palette from a single OKLCH seed.
 */
export function seedToPalette(seed: ColorSeed): SemanticPalette {
  const { l, c, h } = seed;
  const primary = oklch(l, c, h);
  const onPrimary = contrastOn(l, h);
  const primaryContainer = oklch(clamp(l + 0.12, 0, 1), c * 0.55, h);
  const onPrimaryContainer = contrastOn(l + 0.12, h);
  const secondary = oklch(clamp(l - 0.05, 0, 1), c * 0.75, h + 30);
  const onSecondary = contrastOn(l - 0.05, h);
  const surface = oklch(0.98, c * 0.04, h);
  const onSurface = oklch(0.15, 0.02, h);
  const surfaceVariant = oklch(0.93, c * 0.08, h);
  const onSurfaceVariant = oklch(0.28, 0.03, h);
  const background = oklch(0.99, c * 0.03, h);
  const onBackground = oklch(0.12, 0.02, h);

  return {
    primary,
    onPrimary,
    primaryContainer,
    onPrimaryContainer,
    secondary,
    onSecondary,
    surface,
    onSurface,
    surfaceVariant,
    onSurfaceVariant,
    outline: oklch(0.55, c * 0.12, h),
    error: oklch(0.55, 0.22, 25),
    onError: oklch(0.99, 0.01, 25),
    background,
    onBackground,
  };
}

export function parseSeed(input: string | ColorSeed | undefined | null): ColorSeed | null {
  if (!input) return null;
  if (typeof input !== "string") {
    if (
      Number.isFinite(input.l) &&
      Number.isFinite(input.c) &&
      Number.isFinite(input.h)
    ) {
      return { l: input.l, c: input.c, h: input.h };
    }
    return null;
  }

  const trimmed = input.trim();
  const match = trimmed.match(
    /oklch\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)\s*\)/i,
  );
  if (match) {
    return {
      l: Number.parseFloat(match[1]),
      c: Number.parseFloat(match[2]),
      h: Number.parseFloat(match[3]),
    };
  }

  return null;
}
