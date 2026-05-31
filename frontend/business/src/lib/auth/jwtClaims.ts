export function readAccessTokenClaims(accessToken: string): string[] {
  const parts = accessToken.split(".");
  if (parts.length < 2) {
    return [];
  }

  try {
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = payload.padEnd(payload.length + ((4 - (payload.length % 4)) % 4), "=");
    const json = JSON.parse(atob(padded)) as { claims?: string | string[] };
    if (Array.isArray(json.claims)) {
      return json.claims;
    }
    if (typeof json.claims === "string") {
      return [json.claims];
    }
    return [];
  } catch {
    return [];
  }
}

export function hasClaim(accessToken: string | null, required: string): boolean {
  if (!accessToken) {
    return false;
  }

  const granted = new Set(readAccessTokenClaims(accessToken));
  if (granted.has(required)) {
    return true;
  }

  const segments = required.split(":");
  if (segments.length < 3) {
    return false;
  }

  const scopeWide = `${segments[0]}:${segments[1]}:all`;
  return granted.has(scopeWide);
}
