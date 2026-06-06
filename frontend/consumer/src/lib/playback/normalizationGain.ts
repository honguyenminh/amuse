import type { TrackStreamLoudness } from "@/lib/api/types";

export function computeNormalizationGain(
  loudness: TrackStreamLoudness | null | undefined,
  enabled: boolean,
): number {
  if (!enabled || !loudness) return 1;
  return Math.pow(10, loudness.linearGainLu / 20);
}
