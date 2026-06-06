export const AVATAR_ACCENT_COUNT = 12;

const ACCENT_CLASSES = [
  "bg-primary text-on-primary",
  "bg-secondary text-on-secondary",
  "bg-error text-on-error",
  "bg-[oklch(0.55_0.15_30)] text-white",
  "bg-[oklch(0.55_0.15_140)] text-white",
  "bg-[oklch(0.55_0.15_200)] text-white",
  "bg-[oklch(0.55_0.15_260)] text-white",
  "bg-[oklch(0.55_0.15_300)] text-white",
  "bg-[oklch(0.45_0.12_60)] text-white",
  "bg-[oklch(0.45_0.12_180)] text-white",
  "bg-[oklch(0.45_0.12_240)] text-white",
  "bg-[oklch(0.45_0.12_320)] text-white",
] as const;

export function normalizeAvatarAccentSeed(
  seed: number | null | undefined,
): number {
  if (seed == null || Number.isNaN(seed)) {
    return 0;
  }
  return ((seed % AVATAR_ACCENT_COUNT) + AVATAR_ACCENT_COUNT) % AVATAR_ACCENT_COUNT;
}

export function avatarAccentClass(seed: number | null | undefined): string {
  return ACCENT_CLASSES[normalizeAvatarAccentSeed(seed)] ?? ACCENT_CLASSES[0];
}

export function resolveInitials(
  displayName: string | null | undefined,
  email: string | null | undefined,
): string {
  const source = displayName?.trim() || email?.split("@")[0]?.trim() || "?";
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
  }
  return source.slice(0, 2).toUpperCase();
}
