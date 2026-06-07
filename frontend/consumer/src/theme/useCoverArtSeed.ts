"use client";

import { useEffect, useState } from "react";
import { deterministicSeedFromString, extractSeedFromImage } from "./extractSeedFromImage";
import type { ColorSeed } from "./types";

type UseCoverArtSeedOptions = {
  /** Accurate seed from SSR — skips client hash fallback and re-extraction. */
  initialSeed?: ColorSeed | null;
};

/**
 * Resolve a `ColorSeed` from a cover art URL.
 *
 * When `initialSeed` is provided (SSR pages), it is used immediately with no
 * client-side re-extraction to avoid palette flashes after hydration.
 *
 * Otherwise waits for canvas extraction and falls back to a deterministic hash
 * of the URL only when extraction fails.
 */
export function useCoverArtSeed(
  url: string | null | undefined,
  options: UseCoverArtSeedOptions = {},
): ColorSeed | null {
  const { initialSeed = null } = options;
  const [seed, setSeed] = useState<ColorSeed | null>(initialSeed);

  useEffect(() => {
    if (initialSeed) {
      setSeed(initialSeed);
      return;
    }

    if (!url) {
      setSeed(null);
      return;
    }

    let cancelled = false;

    void extractSeedFromImage(url).then((extracted) => {
      if (cancelled) return;
      setSeed(extracted ?? deterministicSeedFromString(url));
    });

    return () => {
      cancelled = true;
    };
  }, [url, initialSeed]);

  return seed;
}
