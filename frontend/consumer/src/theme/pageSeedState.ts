import type { ColorSeed } from "./types";
import { DEFAULT_APP_SEED } from "./defaultPalette";
import { resolveEffectiveSeed } from "./resolveEffectiveSeed";

/** Clear page seed on unmount only when this owner still owns the active seed. */
export function pageSeedAfterOwnerUnmount(
  current: ColorSeed | null,
  ownerSeed: ColorSeed | null,
): ColorSeed | null {
  return current === ownerSeed ? null : current;
}

export function resolveThemeSeed(input: {
  pageSeed: ColorSeed | null;
  playingSeed: ColorSeed | null;
  defaultSeed?: ColorSeed;
}): ColorSeed {
  return resolveEffectiveSeed({
    pageSeed: input.pageSeed,
    playingSeed: input.playingSeed,
    defaultSeed: input.defaultSeed ?? DEFAULT_APP_SEED,
  });
}
