import { DynamicScheme, Hct } from "@material/material-color-utilities";
import { argbToOklch, colorSeedToArgb } from "./colorConvert";
import { makePausedPalette } from "./makePausedPalette";
import { SCHEME_CONTRAST, SCHEME_VARIANT } from "./schemeConfig";
import type { ColorSeed, SemanticPalette } from "./types";

export type SeedToPaletteOptions = {
  paused?: boolean;
};

function seedToSourceHct(seed: ColorSeed): Hct {
  return Hct.fromInt(colorSeedToArgb(seed));
}

/**
 * Builds a semantic palette from a single OKLCH seed using Material Color
 * Utilities (M3 scheme variant from schemeConfig).
 */
export function seedToPalette(
  seed: ColorSeed,
  options: SeedToPaletteOptions = {},
): SemanticPalette {
  const paused = options.paused ?? false;
  const scheme = new DynamicScheme({
    sourceColorHct: seedToSourceHct(seed),
    variant: SCHEME_VARIANT,
    contrastLevel: SCHEME_CONTRAST,
    isDark: false,
  });

  const palette: SemanticPalette = {
    primary: argbToOklch(scheme.primary),
    onPrimary: argbToOklch(scheme.onPrimary),
    primaryContainer: argbToOklch(scheme.primaryContainer),
    onPrimaryContainer: argbToOklch(scheme.onPrimaryContainer),
    secondary: argbToOklch(scheme.secondary),
    onSecondary: argbToOklch(scheme.onSecondary),
    tertiaryContainer: argbToOklch(scheme.tertiaryContainer),
    onTertiaryContainer: argbToOklch(scheme.onTertiaryContainer),
    surface: argbToOklch(scheme.surface),
    onSurface: argbToOklch(scheme.onSurface),
    surfaceVariant: argbToOklch(scheme.surfaceVariant),
    onSurfaceVariant: argbToOklch(scheme.onSurfaceVariant),
    outline: argbToOklch(scheme.outline),
    error: argbToOklch(scheme.error),
    onError: argbToOklch(scheme.onError),
    background: argbToOklch(scheme.background),
    onBackground: argbToOklch(scheme.onBackground),
  };

  return paused ? makePausedPalette(palette) : palette;
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
