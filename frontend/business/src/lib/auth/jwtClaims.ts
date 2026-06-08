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

import {
  buildCatalogReadClaim,
  CATALOG_READ_ALL_CLAIM,
  type CatalogResourceKind,
} from "@/lib/members/catalogResourceClaims";

export function hasAnyCatalogReadClaim(accessToken: string | null): boolean {
  if (!accessToken) {
    return false;
  }

  return readAccessTokenClaims(accessToken).some((claim) => {
    const parts = claim.split(":");
    return parts.length >= 3 && parts[0] === "read" && parts[1] === "catalog";
  });
}

export type CatalogReadContext = {
  artistId?: string;
  releaseId?: string;
  releaseGroupId?: string;
};

export function canReadCatalogResource(
  accessToken: string | null,
  kind: CatalogResourceKind,
  resourceId: string,
  context: CatalogReadContext = {},
): boolean {
  if (!accessToken) {
    return false;
  }

  if (hasClaim(accessToken, CATALOG_READ_ALL_CLAIM)) {
    return true;
  }

  if (hasClaim(accessToken, buildCatalogReadClaim(kind, resourceId))) {
    return true;
  }

  const artistId = context.artistId?.toLowerCase();
  const releaseId = context.releaseId?.toLowerCase();
  const releaseGroupId = context.releaseGroupId?.toLowerCase();

  if (
    artistId &&
    (kind === "release_group" || kind === "release" || kind === "track") &&
    hasClaim(accessToken, buildCatalogReadClaim("artist", artistId))
  ) {
    return true;
  }

  if (
    releaseGroupId &&
    kind === "release" &&
    hasClaim(accessToken, buildCatalogReadClaim("release_group", releaseGroupId))
  ) {
    return true;
  }

  if (
    releaseId &&
    kind === "track" &&
    hasClaim(accessToken, buildCatalogReadClaim("release", releaseId))
  ) {
    return true;
  }

  return false;
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
