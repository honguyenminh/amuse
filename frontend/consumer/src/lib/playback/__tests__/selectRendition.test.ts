import { describe, expect, it, vi } from "vitest";
import type { TrackStreamRenditionDto } from "@/lib/api/types";
import { defaultPlaybackSettings } from "../playbackSettings";
import {
  limitDowngradeToOneStep,
  nextLowerRendition,
  pickRenditionForThroughputKbps,
} from "../renditionLadder";
import { networkQualityCeiling, selectRendition } from "../selectRendition";

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
    id: "opus-256",
    codec: "opus",
    bitrateKbps: 256,
    bandwidth: 256_000,
    sampleRateHz: 48_000,
    adaptationSetId: "opus",
    representationId: "3",
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
    id: "opus-64",
    codec: "opus",
    bitrateKbps: 64,
    bandwidth: 64_000,
    sampleRateHz: 48_000,
    adaptationSetId: "opus",
    representationId: "1",
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

  it("picks opus-256 for high tier when network is unknown (Firefox)", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      {},
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("keeps opus-256 on fast measured throughput within high tier", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { throughputKbps: 64_000 },
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("defers flac in auto lossless until the link proves lossless capacity", () => {
    vi.stubGlobal("MediaSource", {
      isTypeSupported: (type: string) => type.includes("flac"),
    });
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "lossless", qualityMode: "auto" },
      {},
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("picks flac in auto lossless when throughput proves lossless capacity", () => {
    vi.stubGlobal("MediaSource", {
      isTypeSupported: (type: string) => type.includes("flac"),
    });
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "lossless", qualityMode: "auto" },
      { throughputKbps: 2500 },
    );
    expect(chosen?.id).toBe("flac-0");
  });

  it("does not pick flac on Good 3G throughput (~1536 kbps)", () => {
    vi.stubGlobal("MediaSource", {
      isTypeSupported: (type: string) => type.includes("flac"),
    });
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "lossless", qualityMode: "auto" },
      { throughputKbps: 1536 },
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("steps down tier after stall downgrade memory", () => {
    vi.stubGlobal("MediaSource", {
      isTypeSupported: (type: string) => type.includes("flac"),
    });
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "lossless", qualityMode: "auto" },
      { throughputKbps: 2500, stallDowngradeSteps: 1 },
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("falls back from flac in auto when the browser does not support it", () => {
    vi.stubGlobal("MediaSource", { isTypeSupported: () => false });
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "lossless", qualityMode: "auto" },
      {},
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("prefers opus-64 over aac-96 on low tier with 2g", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { effectiveType: "2g" },
    );
    expect(chosen?.id).toBe("opus-64");
  });

  it("downgrades within preferred tier when throughput cannot sustain it", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { throughputKbps: 100 },
    );
    expect(chosen?.id).toBe("opus-64");
  });

  it("keeps high tier pick when throughput can sustain it", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { throughputKbps: 500 },
    );
    expect(chosen?.id).toBe("opus-256");
  });

  it("does not exceed medium when preferred is high on 3g", () => {
    const chosen = selectRendition(
      renditions,
      { ...defaultPlaybackSettings, preferredQuality: "high", qualityMode: "auto" },
      { effectiveType: "3g" },
    );
    expect(chosen?.id).toBe("opus-128");
  });

  it("still allows flac in manual lossless mode", () => {
    vi.stubGlobal("MediaSource", { isTypeSupported: () => true });
    const chosen = selectRendition(renditions, {
      ...defaultPlaybackSettings,
      preferredQuality: "lossless",
      qualityMode: "manual",
      manualRenditionId: "flac-0",
    });
    expect(chosen?.id).toBe("flac-0");
  });

  it("caps non-owner manual lossless to medium tier", () => {
    const chosen = selectRendition(
      renditions,
      {
        ...defaultPlaybackSettings,
        qualityMode: "manual",
        preferredQuality: "lossless",
      },
      { isOwner: false },
    );
    expect(chosen?.id).toBe("opus-128");
  });
});

describe("pickRenditionForThroughputKbps", () => {
  it("does not fall back to opus-64 when throughput is high", () => {
    const chosen = pickRenditionForThroughputKbps(renditions, 64_000, false);
    expect(chosen?.id).toBe("opus-256");
  });
});

describe("limitDowngradeToOneStep", () => {
  it("steps down at most one rung per auto poll", () => {
    const current = renditions.find((r) => r.id === "opus-256")!;
    const target = renditions.find((r) => r.id === "opus-64")!;
    expect(limitDowngradeToOneStep(current, target, renditions)?.id).toBe("opus-128");
  });
});

describe("nextLowerRendition", () => {
  it("steps down to opus-64 before aac-96", () => {
    const current = renditions.find((r) => r.id === "opus-128")!;
    expect(nextLowerRendition(current, renditions)?.id).toBe("opus-64");
  });
});

describe("networkQualityCeiling", () => {
  it("does not constrain when network type is unknown", () => {
    expect(networkQualityCeiling({})).toBe("lossless");
  });

  it("uses effectiveType when present", () => {
    expect(networkQualityCeiling({ effectiveType: "4g" })).toBe("high");
    expect(networkQualityCeiling({ effectiveType: "3g" })).toBe("medium");
  });
});
