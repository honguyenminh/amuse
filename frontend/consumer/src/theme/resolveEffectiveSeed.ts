import type { ColorSeed } from "./types";
import { DEFAULT_APP_SEED } from "./defaultPalette";

export type SeedResolutionInput = {
  pageSeed: ColorSeed | null;
  playingSeed: ColorSeed | null;
  defaultSeed?: ColorSeed;
};

/**
 * Priority: page custom seed > currently playing > app default.
 */
export function resolveEffectiveSeed(input: SeedResolutionInput): ColorSeed {
  if (input.pageSeed) return input.pageSeed;
  if (input.playingSeed) return input.playingSeed;
  return input.defaultSeed ?? DEFAULT_APP_SEED;
}
