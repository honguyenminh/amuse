function normalizeBaseUrl(value: string): string {
  return value.replace(/\/$/, "");
}

function getMediaPublicBaseUrl(): string {
  return normalizeBaseUrl(
    process.env.MEDIA_PUBLIC_BASE_URL ??
      process.env.NEXT_PUBLIC_MEDIA_PUBLIC_BASE_URL ??
      "http://localhost:9000",
  );
}

function isBlockedHostname(hostname: string): boolean {
  const normalized = hostname.toLowerCase();
  if (
    normalized === "localhost" ||
    normalized === "127.0.0.1" ||
    normalized === "::1"
  ) {
    return process.env.NODE_ENV === "production";
  }

  if (normalized.endsWith(".local")) {
    return true;
  }

  const parts = normalized.split(".").map((part) => Number.parseInt(part, 10));
  if (parts.length === 4 && parts.every((part) => !Number.isNaN(part))) {
    const [a, b] = parts;
    if (a === 10) return true;
    if (a === 172 && b !== undefined && b >= 16 && b <= 31) return true;
    if (a === 192 && b === 168) return true;
    if (a === 169 && b === 254) return true;
  }

  return false;
}

/** Only fetch cover art from the configured public media origin (SSRF guard). */
export function isAllowedCoverArtUrl(url: string): boolean {
  try {
    const target = new URL(url);
    if (target.protocol !== "http:" && target.protocol !== "https:") {
      return false;
    }

    if (isBlockedHostname(target.hostname)) {
      return false;
    }

    const allowed = new URL(getMediaPublicBaseUrl());
    return target.origin === allowed.origin;
  } catch {
    return false;
  }
}
