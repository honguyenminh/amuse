import type { TrackStreamRenditionDto } from "@/lib/api/types";
import type { PlaybackSettings, PreferredQuality } from "./playbackSettings";
import {
  pickPreferringOpus,
  pickRenditionForThroughputKbps,
  renditionLadderIndex,
} from "./renditionLadder";

const TIER_ORDER: PreferredQuality[] = ["low", "medium", "high", "lossless"];

const MANUAL_TIER_CANDIDATES: Record<PreferredQuality, string[]> = {
  lossless: ["flac-0"],
  high: ["flac-0", "opus-256", "aac-256"],
  medium: ["opus-128", "aac-128", "opus-64", "aac-96"],
  low: ["opus-64", "aac-96", "opus-128", "aac-128"],
};

const AUTO_TIER_CANDIDATES: Record<PreferredQuality, string[]> = {
  lossless: ["flac-0", "opus-256", "aac-256"],
  high: ["opus-256", "aac-256"],
  medium: ["opus-128", "aac-128", "opus-64", "aac-96"],
  low: ["opus-64", "aac-96"],
};

const TIER_RANK: Record<PreferredQuality, number> = {
  low: 0,
  medium: 1,
  high: 2,
  lossless: 3,
};

/** Auto FLAC needs more headroom than lossy; throttled "Good 3G" (~1536 kbps) must not qualify. */
const AUTO_FLAC_MIN_THROUGHPUT_KBPS = 1800;
const AUTO_FLAC_SUSTAIN_FACTOR = 0.85;
const AUTO_LOSSY_SUSTAIN_FACTOR = 0.7;
const AUTO_FLAC_MIN_DOWNLINK_MBPS = 2.5;

export function canPlayFlacInBrowser(): boolean {
  if (typeof MediaSource === "undefined") return false;
  return MediaSource.isTypeSupported('audio/mp4; codecs="flac"');
}

export type NetworkHints = {
  effectiveType?: string;
  saveData?: boolean;
  /** Estimated downlink in Mbps from the Network Information API. */
  downlinkMbps?: number;
  /** Measured or estimated throughput in kbps (e.g. from dash.js). */
  throughputKbps?: number;
  /** Number of quality tiers to step down after recent rebuffering. */
  stallDowngradeSteps?: number;
  /** When false, unpaid preview cap (~128 kbps); omit or true for purchased owners. */
  isOwner?: boolean;
};

export function hasNavigatorNetworkSignal(hints: NetworkHints): boolean {
  if (hints.saveData) return true;
  if (hints.effectiveType !== undefined && hints.effectiveType.length > 0) return true;
  if (hints.downlinkMbps !== undefined && hints.downlinkMbps > 0) return true;
  return false;
}

/** Caps auto quality from the Network Information API (Chromium; not available in Firefox). */
export function networkQualityCeiling(hints: NetworkHints): PreferredQuality {
  if (hints.saveData) return "low";

  if (hints.downlinkMbps !== undefined) {
    if (hints.downlinkMbps < 0.5) return "low";
    if (hints.downlinkMbps < 1.0) return "medium";
    if (hints.downlinkMbps < 2.5) return "high";
    return "high";
  }

  switch (hints.effectiveType) {
    case "slow-2g":
    case "2g":
      return "low";
    case "3g":
      return "medium";
    case "4g":
      return "high";
    default:
      return "lossless";
  }
}

function minTier(a: PreferredQuality, b: PreferredQuality): PreferredQuality {
  return TIER_RANK[a] <= TIER_RANK[b] ? a : b;
}

function downgradeTier(tier: PreferredQuality, steps: number): PreferredQuality {
  const index = TIER_ORDER.indexOf(tier);
  return TIER_ORDER[Math.max(0, index - steps)] ?? "low";
}

function canSustainFlacInAuto(throughputKbps: number, flacBitrateKbps: number): boolean {
  return (
    throughputKbps >= AUTO_FLAC_MIN_THROUGHPUT_KBPS &&
    throughputKbps * AUTO_FLAC_SUSTAIN_FACTOR >= flacBitrateKbps
  );
}

function canUseLosslessTierInAuto(hints: NetworkHints): boolean {
  if (hints.downlinkMbps !== undefined && hints.downlinkMbps >= AUTO_FLAC_MIN_DOWNLINK_MBPS) {
    return true;
  }
  if (
    hints.throughputKbps !== undefined &&
    hints.throughputKbps >= AUTO_FLAC_MIN_THROUGHPUT_KBPS
  ) {
    return true;
  }
  return false;
}

/** Defer FLAC until the link proves it can carry lossless; avoids starting on FLAC over Good 3G. */
function effectiveAutoTier(tier: PreferredQuality, hints: NetworkHints): PreferredQuality {
  if (tier !== "lossless") return tier;
  return canUseLosslessTierInAuto(hints) ? "lossless" : "high";
}

function renditionsAllowedForTier(
  renditions: TrackStreamRenditionDto[],
  tier: PreferredQuality,
  autoMode: boolean,
): TrackStreamRenditionDto[] {
  const candidates = autoMode ? AUTO_TIER_CANDIDATES : MANUAL_TIER_CANDIDATES;
  const allowedIds = new Set<string>();
  for (const t of TIER_ORDER) {
    for (const id of candidates[t]) {
      allowedIds.add(id);
    }
    if (t === tier) break;
  }
  const filtered = renditions.filter((r) => allowedIds.has(r.id));
  return filtered.length > 0 ? filtered : renditions;
}

function renditionBitrateKbps(rendition: TrackStreamRenditionDto): number {
  return rendition.bitrateKbps ?? Math.round(rendition.bandwidth / 1000);
}

function pickFromTier(
  renditions: TrackStreamRenditionDto[],
  tier: PreferredQuality,
  allowFlac: boolean,
  autoMode: boolean,
): TrackStreamRenditionDto | null {
  const candidates = autoMode ? AUTO_TIER_CANDIDATES : MANUAL_TIER_CANDIDATES;
  const byId = new Map(renditions.map((r) => [r.id, r]));
  for (const id of candidates[tier]) {
    if (id === "flac-0" && !allowFlac) continue;
    const match = byId.get(id);
    if (match) return match;
  }
  const remaining = renditions.filter((r) => allowFlac || r.codec !== "flac");
  return pickPreferringOpus(remaining) ?? renditions[0] ?? null;
}

function pickAutoRenditionForTier(
  renditions: TrackStreamRenditionDto[],
  tier: PreferredQuality,
  hints: NetworkHints,
  allowFlac: boolean,
): TrackStreamRenditionDto | null {
  const tierPick = pickFromTier(renditions, tier, allowFlac, true);
  if (!tierPick) return null;

  const throughputKbps = hints.throughputKbps;
  if (throughputKbps === undefined || throughputKbps <= 0) {
    return tierPick;
  }

  if (tierPick.codec === "flac") {
    if (!canSustainFlacInAuto(throughputKbps, renditionBitrateKbps(tierPick))) {
      const allowed = renditionsAllowedForTier(renditions, "high", true).filter(
        (r) => r.codec !== "flac",
      );
      return pickRenditionForThroughputKbps(allowed, throughputKbps, false) ?? tierPick;
    }
    return tierPick;
  }

  const tierBitrate = renditionBitrateKbps(tierPick);
  if (throughputKbps * AUTO_LOSSY_SUSTAIN_FACTOR >= tierBitrate) {
    return tierPick;
  }

  const allowed = renditionsAllowedForTier(renditions, tier, true).filter(
    (r) => allowFlac || r.codec !== "flac",
  );
  const throughputPick = pickRenditionForThroughputKbps(allowed, throughputKbps, allowFlac);
  if (!throughputPick) return tierPick;

  return renditionLadderIndex(throughputPick) < renditionLadderIndex(tierPick)
    ? throughputPick
    : tierPick;
}

export function selectRendition(
  renditions: TrackStreamRenditionDto[],
  settings: PlaybackSettings,
  hints: NetworkHints = {},
): TrackStreamRenditionDto | null {
  if (renditions.length === 0) return null;

  const isOwner = hints.isOwner !== false;
  const allowFlac = isOwner && canPlayFlacInBrowser();

  if (settings.qualityMode === "manual" && settings.manualRenditionId) {
    const manual = renditions.find((r) => r.id === settings.manualRenditionId);
    if (manual) {
      if (manual.codec === "flac" && !allowFlac) {
        return (
          pickFromTier(renditions, "high", false, false) ??
          renditions.find((r) => r.codec !== "flac") ??
          manual
        );
      }
      return manual;
    }
  }

  let tier = settings.preferredQuality;
  if (!isOwner) {
    tier = minTier(tier, "medium");
  }
  if (settings.qualityMode === "auto") {
    if (hasNavigatorNetworkSignal(hints)) {
      tier = minTier(tier, networkQualityCeiling(hints));
    }
    if (isOwner) {
      tier = effectiveAutoTier(tier, hints);
    }

    if (hints.stallDowngradeSteps && hints.stallDowngradeSteps > 0) {
      tier = downgradeTier(tier, hints.stallDowngradeSteps);
    }

    return (
      pickAutoRenditionForTier(renditions, tier, hints, allowFlac) ??
      pickFromTier(renditions, tier, allowFlac, true) ??
      renditions[0]!
    );
  }

  return (
    pickFromTier(renditions, tier, allowFlac, false) ??
    renditions[0]!
  );
}

export function formatRenditionLabel(rendition: TrackStreamRenditionDto): string {
  const codec = rendition.codec.toUpperCase();
  if (rendition.codec === "flac") {
    return `${codec} · lossless · ${rendition.sampleRateHz / 1000} kHz`;
  }
  return `${codec} · ${rendition.bitrateKbps ?? "?"} kbps · ${rendition.sampleRateHz / 1000} kHz`;
}
