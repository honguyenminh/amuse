import type { SemanticPalette } from "./types";

const CSS_VAR_MAP: Record<keyof SemanticPalette, string> = {
  primary: "--amuse-primary",
  onPrimary: "--amuse-on-primary",
  primaryContainer: "--amuse-primary-container",
  onPrimaryContainer: "--amuse-on-primary-container",
  secondary: "--amuse-secondary",
  onSecondary: "--amuse-on-secondary",
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

export function applyThemeVariables(
  palette: SemanticPalette,
  target: HTMLElement = document.documentElement,
): void {
  for (const [key, cssVar] of Object.entries(CSS_VAR_MAP) as [
    keyof SemanticPalette,
    string,
  ][]) {
    target.style.setProperty(cssVar, palette[key]);
  }
}
