import { formatOklch, parseOklch } from "./colorConvert";
import { SEMANTIC_PALETTE_KEYS, type SemanticPalette } from "./types";

/** Chroma multiplier for accent / surface roles when playback is paused. */
const ACCENT_CHROMA_FACTOR = 0.42;
/** Slight lightness lift so paused UI feels washed back, not muddy. */
const ACCENT_LIGHTNESS_LIFT = 0.055;
/** Roles that must stay at full strength when paused (legibility + destructive emphasis). */
const NO_FADE_KEYS = new Set<keyof SemanticPalette>([
  "onPrimary",
  "onPrimaryContainer",
  "onSecondary",
  "onSecondaryContainer",
  "onTertiary",
  "onTertiaryContainer",
  "onSurface",
  "onSurfaceVariant",
  "onBackground",
  "error",
  "onError",
]);

function fadeRole(
  key: keyof SemanticPalette,
  value: string,
): string {
  if (NO_FADE_KEYS.has(key)) return value;

  const parsed = parseOklch(value);
  if (!parsed) return value;

  return formatOklch(
    Math.min(1, parsed.l + ACCENT_LIGHTNESS_LIFT),
    parsed.c * ACCENT_CHROMA_FACTOR,
    parsed.h,
  );
}

/**
 * Fades a generated semantic palette for paused playback.
 * Applied after M3 scheme generation so variants like VIBRANT (which ignore
 * seed chroma) still visibly soften on pause.
 */
export function makePausedPalette(palette: SemanticPalette): SemanticPalette {
  const faded = { ...palette };
  for (const key of SEMANTIC_PALETTE_KEYS) {
    faded[key] = fadeRole(key, palette[key]);
  }
  return faded;
}
