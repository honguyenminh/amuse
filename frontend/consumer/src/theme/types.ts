/** OKLCH seed: L 0–1, C ≥ 0, H 0–360 */
export type ColorSeed = {
  l: number;
  c: number;
  h: number;
};

export type SemanticPalette = {
  primary: string;
  onPrimary: string;
  primaryContainer: string;
  onPrimaryContainer: string;
  secondary: string;
  onSecondary: string;
  tertiaryContainer: string;
  onTertiaryContainer: string;
  surface: string;
  onSurface: string;
  surfaceVariant: string;
  onSurfaceVariant: string;
  outline: string;
  error: string;
  onError: string;
  background: string;
  onBackground: string;
};

export const SEMANTIC_PALETTE_KEYS: (keyof SemanticPalette)[] = [
  "primary",
  "onPrimary",
  "primaryContainer",
  "onPrimaryContainer",
  "secondary",
  "onSecondary",
  "tertiaryContainer",
  "onTertiaryContainer",
  "surface",
  "onSurface",
  "surfaceVariant",
  "onSurfaceVariant",
  "outline",
  "error",
  "onError",
  "background",
  "onBackground",
];
