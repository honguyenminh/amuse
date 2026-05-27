import type { ColorSeed, SemanticPalette } from "./types";
import { seedToPalette } from "./seedToPalette";

/** Default app seed - saturated violet (expressive bold baseline). */
export const DEFAULT_APP_SEED: ColorSeed = { l: 0.52, c: 0.22, h: 285 };

export const DEFAULT_PALETTE: SemanticPalette = seedToPalette(DEFAULT_APP_SEED);

/** Demo seeds for artist/release placeholder pages. */
export const DEMO_ARTIST_SEEDS: Record<string, ColorSeed> = {
  demo: { l: 0.58, c: 0.24, h: 25 },
};

export const DEMO_ALBUM_SEEDS: Record<string, ColorSeed> = {
  demo: { l: 0.5, c: 0.26, h: 145 },
};
