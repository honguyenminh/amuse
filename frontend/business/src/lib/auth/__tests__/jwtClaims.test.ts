import { describe, expect, it } from "vitest";
import {
  canReadCatalogResource,
  hasAnyCatalogReadClaim,
  hasClaim,
} from "@/lib/auth/jwtClaims";

function tokenWithClaims(claims: string[]): string {
  const payload = btoa(JSON.stringify({ claims }));
  return `header.${payload}.signature`;
}

describe("jwtClaims catalog access", () => {
  it("hasAnyCatalogReadClaim accepts scope-wide and per-resource claims", () => {
    expect(hasAnyCatalogReadClaim(tokenWithClaims(["read:catalog:all"]))).toBe(true);
    expect(
      hasAnyCatalogReadClaim(
        tokenWithClaims(["read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890"]),
      ),
    ).toBe(true);
    expect(hasAnyCatalogReadClaim(tokenWithClaims(["read:org:all"]))).toBe(false);
  });

  it("canReadCatalogResource matches exact or scope-wide catalog read", () => {
    const artistId = "0194a7b2-c3d4-7890-abcd-ef1234567890";
    const scopedToken = tokenWithClaims([`read:catalog:artist:${artistId}`]);
    const allToken = tokenWithClaims(["read:catalog:all"]);

    expect(canReadCatalogResource(scopedToken, "artist", artistId)).toBe(true);
    expect(canReadCatalogResource(scopedToken, "artist", "0194a7b2-c3d4-7890-abcd-ef1234567891")).toBe(
      false,
    );
    expect(canReadCatalogResource(allToken, "artist", artistId)).toBe(true);
    expect(hasClaim(allToken, `read:catalog:artist:${artistId}`)).toBe(true);
  });

  it("artist-scoped read implies child release and release group access", () => {
    const artistId = "0194a7b2-c3d4-7890-abcd-ef1234567890";
    const releaseId = "0194a7b2-c3d4-7890-abcd-ef1234567891";
    const groupId = "0194a7b2-c3d4-7890-abcd-ef1234567892";
    const scopedToken = tokenWithClaims([`read:catalog:artist:${artistId}`]);

    expect(
      canReadCatalogResource(scopedToken, "release", releaseId, { artistId }),
    ).toBe(true);
    expect(
      canReadCatalogResource(scopedToken, "release_group", groupId, { artistId }),
    ).toBe(true);
    expect(
      canReadCatalogResource(scopedToken, "release", releaseId, {
        artistId: "0194a7b2-c3d4-7890-abcd-ef1234567899",
      }),
    ).toBe(false);
  });
});
