import type { ClaimPresetResponse, OrganizationMemberResponse } from "@/lib/api/tenancyClient";
import {
  extractCatalogResourceClaims,
  isCatalogResourceReadClaim,
} from "@/lib/members/catalogResourceClaims";
import { findPreset, getPresetDisplayName } from "@/lib/members/presetDisplay";

export type PermissionSelection = {
  claims: string[];
  presetLabel: string | null;
};

export type PermissionApiPayload = {
  presetRoleLabel?: string | null;
  claims?: string[] | null;
};

function normalizeClaims(claims: string[]): string[] {
  return [...new Set(claims.map((claim) => claim.trim()).filter(Boolean))].sort((a, b) =>
    a.localeCompare(b),
  );
}

function claimsEqual(left: string[], right: string[]): boolean {
  const normalizedLeft = normalizeClaims(left);
  const normalizedRight = normalizeClaims(right);
  if (normalizedLeft.length !== normalizedRight.length) {
    return false;
  }

  return normalizedLeft.every((claim, index) => claim === normalizedRight[index]);
}

export function claimsMatchPreset(
  presets: ClaimPresetResponse[],
  claims: string[],
): string | null {
  const normalized = normalizeClaims(claims);
  for (const preset of presets) {
    if (claimsEqual(preset.claims, normalized)) {
      return preset.label;
    }
  }

  return null;
}

export function selectionFromClaims(
  presets: ClaimPresetResponse[],
  claims: string[],
  presetRoleLabel?: string | null,
): PermissionSelection {
  const normalized = normalizeClaims(claims);
  const matchedPreset = claimsMatchPreset(presets, normalized);
  if (matchedPreset) {
    return { claims: normalized, presetLabel: matchedPreset };
  }

  if (presetRoleLabel && findPreset(presets, presetRoleLabel)) {
    const preset = findPreset(presets, presetRoleLabel);
    if (preset && claimsEqual(preset.claims, normalized)) {
      return { claims: normalized, presetLabel: presetRoleLabel };
    }
  }

  return { claims: normalized, presetLabel: null };
}

export function selectionFromMember(
  presets: ClaimPresetResponse[],
  member: OrganizationMemberResponse,
): PermissionSelection {
  return selectionFromClaims(presets, member.claims, member.presetRoleLabel);
}

export function defaultInviteSelection(presets: ClaimPresetResponse[]): PermissionSelection {
  const preset =
    findPreset(presets, "member_manager") ??
    presets[0];
  if (!preset) {
    return { claims: ["read:org:all"], presetLabel: null };
  }

  return {
    claims: normalizeClaims(preset.claims),
    presetLabel: preset.label,
  };
}

export function buildPermissionPayload(selection: PermissionSelection): PermissionApiPayload {
  if (selection.presetLabel) {
    return { presetRoleLabel: selection.presetLabel };
  }

  return {
    presetRoleLabel: null,
    claims: normalizeClaims(selection.claims),
  };
}

export function selectionsEqual(
  left: PermissionSelection,
  right: PermissionSelection,
): boolean {
  return (
    left.presetLabel === right.presetLabel &&
    claimsEqual(left.claims, right.claims)
  );
}

export function applyPresetSelection(
  preset: ClaimPresetResponse,
): PermissionSelection {
  return {
    presetLabel: preset.label,
    claims: normalizeClaims(preset.claims),
  };
}

export function toggleScopeWideClaim(
  selection: PermissionSelection,
  presets: ClaimPresetResponse[],
  claim: string,
  enabled: boolean,
): PermissionSelection {
  const claims = new Set(selection.claims);
  if (enabled) {
    claims.add(claim);
  } else {
    claims.delete(claim);
  }

  let nextClaims = [...claims];
  if (claim === "read:catalog:all" && enabled) {
    nextClaims = nextClaims.filter((entry) => !isCatalogResourceReadClaim(entry));
  }

  const normalized = normalizeClaims(nextClaims);
  return {
    claims: normalized,
    presetLabel: claimsMatchPreset(presets, normalized),
  };
}

export function setCatalogResourceClaims(
  selection: PermissionSelection,
  presets: ClaimPresetResponse[],
  resourceClaims: string[],
): PermissionSelection {
  const withoutCatalogResources = selection.claims.filter(
    (claim) => !isCatalogResourceReadClaim(claim),
  );
  const withoutCatalogReadAll = withoutCatalogResources.filter(
    (claim) => claim !== "read:catalog:all",
  );
  const normalized = normalizeClaims([
    ...withoutCatalogReadAll,
    ...resourceClaims,
  ]);

  return {
    claims: normalized,
    presetLabel: claimsMatchPreset(presets, normalized),
  };
}

export function summarizePermissionSelection(
  presets: ClaimPresetResponse[],
  selection: PermissionSelection,
): { title: string; detail: string | null; resourceCount: number } {
  const resourceCount = extractCatalogResourceClaims(selection.claims).length;
  if (selection.presetLabel) {
    return {
      title: getPresetDisplayName(presets, selection.presetLabel),
      detail:
        resourceCount > 0
          ? `${resourceCount} catalog resource${resourceCount === 1 ? "" : "s"}`
          : null,
      resourceCount,
    };
  }

  const claimCount = selection.claims.length;
  return {
    title: `Custom (${claimCount} claim${claimCount === 1 ? "" : "s"})`,
    detail:
      resourceCount > 0
        ? `${resourceCount} catalog resource${resourceCount === 1 ? "" : "s"}`
        : null,
    resourceCount,
  };
}
