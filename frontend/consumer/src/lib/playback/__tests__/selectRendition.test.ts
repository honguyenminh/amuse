import { describe, expect, it } from "vitest";
import type { TrackStreamRenditionDto } from "@/lib/api/types";
import { defaultPlaybackSettings } from "../playbackSettings";
import { selectRendition } from "../selectRendition";

const renditions: TrackStreamRenditionDto[] = [
  {
    id: "flac-0",
    codec: "flac",
    bitrateKbps: null,
    bandwidth: 800_000,
    sampleRateHz: 48_000,
    adaptationSetId: "flac",
    representationId: "0",
  },
  {
    id: "opus-128",
    codec: "opus",
    bitrateKbps: 128,
    bandwidth: 128_000,
    sampleRateHz: 48_000,
    adaptationSetId: "opus",
    representationId: "2",
  },
  {
    id: "aac-96",
    codec: "aac",
    bitrateKbps: 96,
    bandwidth: 96_000,
    sampleRateHz: 48_000,
    adaptationSetId: "aac",
    representationId: "4",
  },
];

describe("selectRendition", () => {
  it("picks opus-128 for medium tier", () => {
    const chosen = selectRendition(renditions, {
      ...defaultPlaybackSettings,
      preferredQuality: "medium",
      qualityMode: "auto",
    });
    expect(chosen?.id).toBe("opus-128");
  });

  it("caps auto mode to low on 2g", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { effectiveType: "2g" },
    );
    expect(chosen?.id).toBe("aac-96");
  });
});
