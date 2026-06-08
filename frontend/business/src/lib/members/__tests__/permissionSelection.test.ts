import { describe, expect, it } from "vitest";
import type { ClaimPresetResponse } from "@/lib/api/tenancyClient";
import {
  buildPermissionPayload,
  claimsMatchPreset,
  setCatalogResourceClaims,
  toggleScopeWideClaim,
} from "@/lib/members/permissionSelection";

const presets: ClaimPresetResponse[] = [
  {
    label: "viewer",
    displayName: "Viewer",
    description: "Read-only",
    icon: "eye",
    claims: ["read:org:all", "read:membership:all", "read:catalog:all"],
  },
  {
    label: "member_manager",
    displayName: "Member manager",
    description: "Manage members",
    icon: "users",
    claims: ["read:org:all", "read:membership:all", "manage:membership:all"],
  },
];

describe("permissionSelection", () => {
  it("detects preset match from claims", () => {
    expect(claimsMatchPreset(presets, presets[1].claims)).toBe("member_manager");
    expect(claimsMatchPreset(presets, ["read:org:all"])).toBeNull();
  });

  it("builds preset payload when preset label is set", () => {
    expect(
      buildPermissionPayload({
        presetLabel: "viewer",
        claims: presets[0].claims,
      }),
    ).toEqual({ presetRoleLabel: "viewer" });
  });

  it("builds explicit claims payload for custom selection", () => {
    expect(
      buildPermissionPayload({
        presetLabel: null,
        claims: ["read:org:all", "read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890"],
      }),
    ).toEqual({
      presetRoleLabel: null,
      claims: [
        "read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890",
        "read:org:all",
      ],
    });
  });

  it("clears per-resource catalog claims when read:catalog:all is enabled", () => {
    const next = toggleScopeWideClaim(
      {
        presetLabel: null,
        claims: [
          "read:org:all",
          "read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890",
        ],
      },
      presets,
      "read:catalog:all",
      true,
    );

    expect(next.claims).toEqual(["read:catalog:all", "read:org:all"]);
    expect(next.presetLabel).toBeNull();
  });

  it("removes read:catalog:all when setting per-resource catalog claims", () => {
    const next = setCatalogResourceClaims(
      {
        presetLabel: "viewer",
        claims: presets[0].claims,
      },
      presets,
      ["read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890"],
    );

    expect(next.claims).toEqual([
      "read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890",
      "read:membership:all",
      "read:org:all",
    ]);
    expect(next.presetLabel).toBeNull();
  });
});
