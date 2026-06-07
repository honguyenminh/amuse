import { seedToPalette } from "./seedToPalette";
import type { ColorSeed, SemanticPalette } from "./types";

const CSS_VAR_MAP: Record<keyof SemanticPalette, string> = {
  primary: "--amuse-primary",
  onPrimary: "--amuse-on-primary",
  primaryContainer: "--amuse-primary-container",
  onPrimaryContainer: "--amuse-on-primary-container",
  secondary: "--amuse-secondary",
  onSecondary: "--amuse-on-secondary",
  secondaryContainer: "--amuse-secondary-container",
  onSecondaryContainer: "--amuse-on-secondary-container",
  tertiary: "--amuse-tertiary",
  onTertiary: "--amuse-on-tertiary",
  tertiaryContainer: "--amuse-tertiary-container",
  onTertiaryContainer: "--amuse-on-tertiary-container",
  surface: "--amuse-surface",
  onSurface: "--amuse-on-surface",
  surfaceVariant: "--amuse-surface-variant",
  onSurfaceVariant: "--amuse-on-surface-variant",
  outline: "--amuse-outline",
  error: "--amuse-error",
  onError: "--amuse-on-error",
  background: "--amuse-background",
  onBackground: "--amuse-on-background",
};

export function paletteToRootCss(palette: SemanticPalette): string {
  const declarations = (
    Object.entries(CSS_VAR_MAP) as [keyof SemanticPalette, string][]
  ).map(([key, cssVar]) => `${cssVar}:${palette[key]};`);
  return `:root{${declarations.join("")}}`;
}

export function seedToRootCss(seed: ColorSeed): string {
  return paletteToRootCss(seedToPalette(seed));
}
