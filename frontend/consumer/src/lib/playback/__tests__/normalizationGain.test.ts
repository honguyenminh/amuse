import { describe, expect, it } from "vitest";
import type { TrackStreamLoudness } from "@/lib/api/types";
import { computeNormalizationGain } from "../normalizationGain";

const sampleLoudness: TrackStreamLoudness = {
  integratedLufs: -23,
  truePeakDbtp: -10,
  targetIntegratedLufs: -14,
  targetTruePeakDbtp: -1,
  linearGainLu: 9,
};

describe("computeNormalizationGain", () => {
  it("returns 1 when normalization is disabled", () => {
    expect(computeNormalizationGain(sampleLoudness, false)).toBe(1);
  });

  it("returns 1 when loudness metadata is missing", () => {
    expect(computeNormalizationGain(null, true)).toBe(1);
    expect(computeNormalizationGain(undefined, true)).toBe(1);
  });

  it("applies linear gain from metadata when enabled", () => {
    expect(computeNormalizationGain(sampleLoudness, true)).toBeCloseTo(2.818, 2);
  });

  it("attenuates loud tracks with negative linearGainLu", () => {
    const loud: TrackStreamLoudness = { ...sampleLoudness, linearGainLu: -4 };
    expect(computeNormalizationGain(loud, true)).toBeCloseTo(0.631, 2);
  });
});
