export const CATALOG_RESOURCE_KINDS = [
  "artist",
  "release",
  "track",
  "release_group",
] as const;

export type CatalogResourceKind = (typeof CATALOG_RESOURCE_KINDS)[number];

export type CatalogResourceRef = {
  kind: CatalogResourceKind;
  id: string;
  label: string;
};

const CATALOG_READ_CLAIM_PATTERN =
  /^read:catalog:(artist|release|track|release_group):([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i;

export const CATALOG_READ_ALL_CLAIM = "read:catalog:all";

export function buildCatalogReadClaim(kind: CatalogResourceKind, id: string): string {
  return `read:catalog:${kind}:${id}`;
}

export function parseCatalogReadClaim(claim: string): Pick<CatalogResourceRef, "kind" | "id"> | null {
  const match = CATALOG_READ_CLAIM_PATTERN.exec(claim.trim());
  if (!match) {
    return null;
  }

  return {
    kind: match[1] as CatalogResourceKind,
    id: match[2].toLowerCase(),
  };
}

export function isCatalogResourceReadClaim(claim: string): boolean {
  return parseCatalogReadClaim(claim) !== null;
}

export function extractCatalogResourceClaims(claims: string[]): Pick<CatalogResourceRef, "kind" | "id">[] {
  return claims
    .map(parseCatalogReadClaim)
    .filter((entry): entry is Pick<CatalogResourceRef, "kind" | "id"> => entry !== null);
}

export function catalogResourceKindLabel(kind: CatalogResourceKind): string {
  switch (kind) {
    case "artist":
      return "Artist";
    case "release":
      return "Release";
    case "track":
      return "Track";
    case "release_group":
      return "Release group";
    default:
      return kind;
  }
}
