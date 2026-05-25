"use client";

import { useEffect, useState } from "react";
import { deterministicSeedFromString, extractSeedFromImage } from "./extractSeedFromImage";
import type { ColorSeed } from "./types";

/**
 * Resolve a `ColorSeed` from a cover art URL. Attempts canvas-based extraction first
 * and falls back to a deterministic hash of the URL (or `null` if no URL is provided).
 *
 * Returns `null` until the seed is resolved so callers can opt out of applying a seed
 * during the first paint to avoid flashing the default palette.
 */
export function useCoverArtSeed(url: string | null | undefined): ColorSeed | null {
  const [seed, setSeed] = useState<ColorSeed | null>(null);

  useEffect(() => {
    if (!url) {
      setSeed(null);
      return;
    }

    let cancelled = false;

    setSeed(deterministicSeedFromString(url));

    void extractSeedFromImage(url).then((extracted) => {
      if (cancelled || !extracted) return;
      setSeed(extracted);
    });

    return () => {
      cancelled = true;
    };
  }, [url]);

  return seed;
}
