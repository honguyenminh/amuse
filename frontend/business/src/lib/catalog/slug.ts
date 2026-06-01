export const SLUG_MAX_LENGTH = 96;
const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function stripDiacritics(value: string): string {
  return value.normalize("NFD").replace(/\p{M}/gu, "").normalize("NFC");
}

export function normalizeSlugInput(rawSlug: string): string {
  if (!rawSlug.trim()) {
    return "";
  }

  let normalized = stripDiacritics(rawSlug.trim().toLowerCase());
  normalized = normalized.replaceAll("_", "-");
  normalized = normalized.replace(/&+/g, " and ");

  const parts: string[] = [];
  let current = "";
  for (const ch of normalized) {
    if (/[a-z0-9]/.test(ch)) {
      current += ch;
      continue;
    }
    if (current.length > 0 && (ch === "-" || ch === " " || ch === "/" || ch === "." || ch === ",")) {
      parts.push(current);
      current = "";
    }
  }
  if (current.length > 0) {
    parts.push(current);
  }

  let result = parts.join("-").replace(/-+/g, "-").replace(/^-+|-+$/g, "");
  if (result.length > SLUG_MAX_LENGTH) {
    result = result.slice(0, SLUG_MAX_LENGTH).replace(/-+$/g, "");
  }
  return result;
}

export function suggestArtistSlugFromName(name: string): string {
  if (!name.trim()) {
    return "";
  }

  let normalized = stripDiacritics(name.trim().toLowerCase());
  normalized = normalized.replace(/&+/g, " and ");
  normalized = normalized.replace(/[''`´]+/g, "");

  const parts: string[] = [];
  let current = "";
  for (const ch of normalized) {
    if (/[a-z0-9]/.test(ch)) {
      current += ch;
      continue;
    }
    if (current.length > 0) {
      parts.push(current);
      current = "";
    }
  }
  if (current.length > 0) {
    parts.push(current);
  }

  let result = parts.join("-").replace(/-+/g, "-").replace(/^-+|-+$/g, "");
  if (result.length > SLUG_MAX_LENGTH) {
    result = result.slice(0, SLUG_MAX_LENGTH).replace(/-+$/g, "");
  }
  return result;
}

export function isValidArtistSlug(slug: string): boolean {
  return slug.length > 0 && slug.length <= SLUG_MAX_LENGTH && SLUG_PATTERN.test(slug);
}

export function slugValidationMessage(slug: string): string | null {
  const normalized = normalizeSlugInput(slug);
  if (!normalized) {
    return "Slug is required.";
  }
  if (!isValidArtistSlug(normalized)) {
    return "Use lowercase letters, numbers, and single hyphens (e.g. my-artist-name).";
  }
  return null;
}
