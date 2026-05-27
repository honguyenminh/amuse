export function getApiBaseUrl(): string {
  return (
    process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ??
    "http://localhost:5000"
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
