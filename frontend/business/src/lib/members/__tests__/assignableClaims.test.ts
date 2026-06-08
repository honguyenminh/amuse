import { describe, expect, it } from "vitest";
import type { OrganizationCapabilities } from "@/lib/api/tenancyClient";
import {
  filterAssignableClaims,
  isClaimAssignable,
} from "@/lib/members/assignableClaims";

const activeIndieCapabilities: OrganizationCapabilities = {
  canReadOrg: true,
  canReadMembership: true,
  canUpload: true,
  canWriteDraft: true,
  canPublishPublic: true,
  canReadPayout: true,
  claimStrings: [],
};

const pendingBackingCapabilities: OrganizationCapabilities = {
  canReadOrg: true,
  canReadMembership: true,
  canUpload: false,
  canWriteDraft: false,
  canPublishPublic: false,
  canReadPayout: false,
  claimStrings: [],
};

describe("assignableClaims", () => {
  it("allows per-resource catalog read for active indie org", () => {
    const claim = "read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890";
    expect(isClaimAssignable(claim, activeIndieCapabilities)).toBe(true);
    expect(filterAssignableClaims([claim], activeIndieCapabilities)).toEqual([claim]);
  });

  it("rejects per-resource catalog write claims", () => {
    const claim = "upload:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890";
    expect(isClaimAssignable(claim, activeIndieCapabilities)).toBe(false);
  });

  it("filters admin catalog writes for pending backing org", () => {
    const claims = [
      "read:org:all",
      "upload:catalog:all",
      "write_draft:catalog:all",
    ];
    expect(filterAssignableClaims(claims, pendingBackingCapabilities)).toEqual([
      "read:org:all",
    ]);
  });
});
