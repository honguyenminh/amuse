"use client";

import { useEffect, useState } from "react";

const SETTLE_TOLERANCE_MS = 250;

type ScrubCallbacks = {
  beginScrub: () => void;
  endScrub: (positionMs: number) => void;
};

/**
 * Local playhead override while scrubbing and briefly after release so the UI
 * does not snap back to the pre-seek audio readout before the element catches up.
 */
export function useScrubPosition(
  smoothMs: number,
  maxMs: number,
  { beginScrub, endScrub }: ScrubCallbacks,
) {
  const [scrubMs, setScrubMs] = useState<number | null>(null);
  const [isScrubbing, setIsScrubbing] = useState(false);

  const displayMs = scrubMs ?? Math.min(smoothMs, maxMs);

  useEffect(() => {
    if (scrubMs === null || isScrubbing) return;
    if (Math.abs(smoothMs - scrubMs) <= SETTLE_TOLERANCE_MS) {
      setScrubMs(null);
    }
  }, [smoothMs, scrubMs, isScrubbing]);

  const commitSeek = (positionMs: number) => {
    endScrub(positionMs);
    setScrubMs(positionMs);
  };

  return {
    displayMs,
    sliderProps: {
      onChange: (next: number) => {
        if (isScrubbing) {
          setScrubMs(next);
          return;
        }
        commitSeek(next);
      },
      onScrubStart: () => {
        setIsScrubbing(true);
        beginScrub();
        setScrubMs(displayMs);
      },
      onScrubEnd: (final: number) => {
        if (!isScrubbing) return;
        setIsScrubbing(false);
        commitSeek(final);
      },
    },
  };
}
