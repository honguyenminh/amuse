const HASHTAG_PATTERN = /^[a-zA-Z][a-zA-Z0-9_]{0,63}$/;

export function normalizeHashtagTag(tag: string): string | null {
  const trimmed = tag.trim();
  if (!HASHTAG_PATTERN.test(trimmed)) return null;
  return trimmed.toLowerCase();
}

export function catalogHashtagPath(tag: string): string {
  const normalized = normalizeHashtagTag(tag);
  if (!normalized) return "/hashtag/invalid";
  return `/hashtag/${encodeURIComponent(normalized)}`;
}

export function isValidHashtagTag(tag: string): boolean {
  return normalizeHashtagTag(tag) !== null;
}
