import type { TrackStreamRenditionDto } from "@/lib/api/types";

/** Ascending quality; Opus is listed before AAC at each bitrate tier. */
export const RENDITION_LADDER_ORDER = [
  "opus-64",
  "aac-96",
  "opus-128",
  "aac-128",
  "opus-256",
  "aac-256",
  "flac-0",
] as const;

const LADDER_INDEX = new Map<string, number>(
  RENDITION_LADDER_ORDER.map((id, index) => [id, index]),
);

export function renditionLadderIndex(rendition: TrackStreamRenditionDto): number {
  return LADDER_INDEX.get(rendition.id) ?? rendition.bandwidth;
}

export function sortRenditionsByLadder(
  renditions: TrackStreamRenditionDto[],
): TrackStreamRenditionDto[] {
  return [...renditions].sort(
    (a, b) => renditionLadderIndex(a) - renditionLadderIndex(b),
  );
}

/** @deprecated Use sortRenditionsByLadder — kept as alias for callers sorting by bandwidth. */
export function sortRenditionsByBandwidth(
  renditions: TrackStreamRenditionDto[],
): TrackStreamRenditionDto[] {
  return sortRenditionsByLadder(renditions);
}

export function nextLowerRendition(
  current: TrackStreamRenditionDto,
  renditions: TrackStreamRenditionDto[],
): TrackStreamRenditionDto | null {
  const byId = new Map(renditions.map((r) => [r.id, r]));
  const currentIndex = LADDER_INDEX.get(current.id);
  if (currentIndex === undefined || currentIndex <= 0) return null;

  for (let i = currentIndex - 1; i >= 0; i--) {
    const id = RENDITION_LADDER_ORDER[i];
    const rendition = byId.get(id);
    if (rendition?.codec === "opus") return rendition;
  }

  for (let i = currentIndex - 1; i >= 0; i--) {
    const id = RENDITION_LADDER_ORDER[i];
    const rendition = byId.get(id);
    if (rendition) return rendition;
  }

  return null;
}

function renditionBitrateKbps(rendition: TrackStreamRenditionDto): number {
  return rendition.bitrateKbps ?? Math.round(rendition.bandwidth / 1000);
}

function pickHighestRenditionWithinBitrateCap(
  sorted: TrackStreamRenditionDto[],
  targetKbps: number,
): TrackStreamRenditionDto | null {
  let bestOpus: TrackStreamRenditionDto | null = null;
  let bestAac: TrackStreamRenditionDto | null = null;

  for (const rendition of sorted) {
    if (renditionBitrateKbps(rendition) > targetKbps) continue;
    if (rendition.codec === "opus") bestOpus = rendition;
    if (rendition.codec === "aac") bestAac = rendition;
  }

  return bestOpus ?? bestAac;
}

export function pickRenditionForThroughputKbps(
  renditions: TrackStreamRenditionDto[],
  throughputKbps: number,
  allowFlac: boolean,
): TrackStreamRenditionDto | null {
  if (renditions.length === 0) return null;

  const sorted = sortRenditionsByLadder(renditions).filter(
    (r) => allowFlac || r.codec !== "flac",
  );
  if (sorted.length === 0) return null;

  const conservativeTarget = Math.max(64, Math.floor(throughputKbps * 0.7));
  const conservative = pickHighestRenditionWithinBitrateCap(sorted, conservativeTarget);
  if (conservative) return conservative;

  const relaxedTarget = Math.max(64, Math.floor(throughputKbps));
  return pickHighestRenditionWithinBitrateCap(sorted, relaxedTarget);
}

/** Limits an auto downgrade to at most one ladder step below the current rendition. */
export function limitDowngradeToOneStep(
  current: TrackStreamRenditionDto,
  chosen: TrackStreamRenditionDto,
  renditions: TrackStreamRenditionDto[],
): TrackStreamRenditionDto {
  if (renditionLadderIndex(chosen) >= renditionLadderIndex(current)) return chosen;
  return nextLowerRendition(current, renditions) ?? chosen;
}

export function pickPreferringOpus(
  renditions: TrackStreamRenditionDto[],
): TrackStreamRenditionDto | null {
  if (renditions.length === 0) return null;
  const sorted = sortRenditionsByLadder(renditions);
  return (
    sorted.find((r) => r.codec === "opus") ??
    sorted.find((r) => r.codec === "aac") ??
    sorted[0] ??
    null
  );
}
