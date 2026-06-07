function normalizeBaseUrl(value: string | undefined, fallback: string): string {
  return value?.replace(/\/$/, "") ?? fallback;
}

export function getApiBaseUrl(): string {
  return normalizeBaseUrl(process.env.NEXT_PUBLIC_API_BASE_URL, "http://localhost:5000");
}

/** Server-side API base URL; prefers in-cluster address when configured. */
export function getServerApiBaseUrl(): string {
  return normalizeBaseUrl(
    process.env.API_INTERNAL_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL,
    "http://localhost:5000",
  );
}

/** Resolves catalog stream paths returned by the API (often relative) against the API host. */
export function resolveApiUrl(pathOrUrl: string): string {
  if (/^https?:\/\//i.test(pathOrUrl)) {
    return pathOrUrl;
  }

  const base = getApiBaseUrl();
  return pathOrUrl.startsWith("/") ? `${base}${pathOrUrl}` : `${base}/${pathOrUrl}`;
}

export const WEB_CLIENT_HEADER = "web";
