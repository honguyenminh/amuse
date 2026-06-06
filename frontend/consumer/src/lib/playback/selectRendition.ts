import type { TrackStreamRenditionDto } from "@/lib/api/types";
import type { PlaybackSettings, PreferredQuality } from "./playbackSettings";

const TIER_CANDIDATES: Record<PreferredQuality, string[]> = {
  lossless: ["flac-0"],
  high: ["flac-0", "opus-256", "aac-256"],
  medium: ["opus-128", "aac-128", "opus-64", "aac-96"],
  low: ["opus-64", "aac-96", "opus-128", "aac-128"],
};

const TIER_RANK: Record<PreferredQuality, number> = {
  low: 0,
  medium: 1,
  high: 2,
  lossless: 3,
};

export function canPlayFlacInBrowser(): boolean {
  if (typeof MediaSource === "undefined") return false;
  return MediaSource.isTypeSupported('audio/mp4; codecs="flac"');
}

export type NetworkHints = {
  effectiveType?: string;
  saveData?: boolean;
};

export function networkQualityCeiling(hints: NetworkHints): PreferredQuality {
  if (hints.saveData) return "low";
  switch (hints.effectiveType) {
    case "slow-2g":
    case "2g":
      return "low";
    case "3g":
      return "medium";
    default:
      return "lossless";
  }
}

function minTier(a: PreferredQuality, b: PreferredQuality): PreferredQuality {
  return TIER_RANK[a] <= TIER_RANK[b] ? a : b;
}

function pickFromTier(
  renditions: TrackStreamRenditionDto[],
  tier: PreferredQuality,
  allowFlac: boolean,
): TrackStreamRenditionDto | null {
  const byId = new Map(renditions.map((r) => [r.id, r]));
  for (const id of TIER_CANDIDATES[tier]) {
    if (id === "flac-0" && !allowFlac) continue;
    const match = byId.get(id);
    if (match) return match;
  }
  return renditions[0] ?? null;
}

export function selectRendition(
  renditions: TrackStreamRenditionDto[],
  settings: PlaybackSettings,
  hints: NetworkHints = {},
): TrackStreamRenditionDto | null {
  if (renditions.length === 0) return null;

  const allowFlac = canPlayFlacInBrowser();

  if (settings.qualityMode === "manual" && settings.manualRenditionId) {
    const manual = renditions.find((r) => r.id === settings.manualRenditionId);
    if (manual) {
      if (manual.codec === "flac" && !allowFlac) {
        return (
          pickFromTier(renditions, "high", false) ??
          renditions.find((r) => r.codec !== "flac") ??
          manual
        );
      }
      return manual;
    }
  }

  let tier = settings.preferredQuality;
  if (settings.qualityMode === "auto") {
    const networkCeiling = networkQualityCeiling(hints);
    tier = minTier(tier, networkCeiling);
  }

  return pickFromTier(renditions, tier, allowFlac) ?? renditions[0]!;
}

export function formatRenditionLabel(rendition: TrackStreamRenditionDto): string {
  const codec = rendition.codec.toUpperCase();
  if (rendition.codec === "flac") {
    return `${codec} · lossless · ${rendition.sampleRateHz / 1000} kHz`;
  }
  return `${codec} · ${rendition.bitrateKbps ?? "?"} kbps · ${rendition.sampleRateHz / 1000} kHz`;
}
