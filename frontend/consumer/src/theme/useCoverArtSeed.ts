"use client";

import { useLayoutEffect, useMemo, useState } from "react";
import { deterministicSeedFromString, extractSeedFromImage } from "./extractSeedFromImage";
import type { ColorSeed } from "./types";

type UseCoverArtSeedOptions = {
  /** Accurate seed from SSR — skips client hash fallback and re-extraction. */
  initialSeed?: ColorSeed | null;
};

type ExtractionState = {
  url: string;
  seed: ColorSeed;
};

/**
 * Resolve a `ColorSeed` from a cover art URL.
 *
 * When `initialSeed` is provided (SSR pages), it is returned synchronously with
 * no client-side re-extraction to avoid palette flashes after hydration.
 *
 * Otherwise uses a deterministic hash immediately (legacy behavior), then
 * replaces it when canvas extraction succeeds.
 */
export function useCoverArtSeed(
  url: string | null | undefined,
  options: UseCoverArtSeedOptions = {},
): ColorSeed | null {
  const { initialSeed = null } = options;
  const [extraction, setExtraction] = useState<ExtractionState | null>(null);
  const hashSeed = useMemo(
    () => (url ? deterministicSeedFromString(url) : null),
    [url],
  );

  useLayoutEffect(() => {
    if (initialSeed || !url) {
      return;
    }

    let cancelled = false;

    void extractSeedFromImage(url).then((extracted) => {
      if (cancelled) {
        return;
      }
      setExtraction({
        url,
        seed: extracted ?? deterministicSeedFromString(url),
      });
    });

    return () => {
      cancelled = true;
    };
  }, [url, initialSeed]);

  if (initialSeed) {
    return initialSeed;
  }

  if (!url) {
    return null;
  }

  return extraction?.url === url ? extraction.seed : hashSeed;
}
