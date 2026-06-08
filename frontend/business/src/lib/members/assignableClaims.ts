import type { OrganizationCapabilities } from "@/lib/api/tenancyClient";
import { CATALOG_READ_ALL_CLAIM } from "@/lib/members/catalogResourceClaims";

export type ClaimGroup = {
  id: string;
  label: string;
  claims: string[];
};

export const SCOPE_WIDE_CLAIM_GROUPS: ClaimGroup[] = [
  {
    id: "org",
    label: "Organization",
    claims: ["read:org:all", "manage:org:all"],
  },
  {
    id: "membership",
    label: "Membership",
    claims: [
      "read:membership:all",
      "manage:membership:all",
      "manage:member_permissions:all",
    ],
  },
  {
    id: "catalog",
    label: "Catalog",
    claims: [
      CATALOG_READ_ALL_CLAIM,
      "upload:catalog:all",
      "write_draft:catalog:all",
      "publish_public:catalog:all",
      "manage:catalog:pricing:all",
    ],
  },
  {
    id: "payout",
    label: "Payout & purchases",
    claims: [
      "read:payout:all",
      "manage:purchase:refund:all",
      "manage:payout:profile:all",
      "manage:payout:withdraw:all",
    ],
  },
];

export const ALL_SCOPE_WIDE_ASSIGNABLE_CLAIMS = SCOPE_WIDE_CLAIM_GROUPS.flatMap(
  (group) => group.claims,
);

function parseClaimSegments(claim: string): { action: string; scope: string; target: string } | null {
  const parts = claim.trim().split(":");
  if (parts.length < 3) {
    return null;
  }

  return {
    action: parts[0] ?? "",
    scope: parts[1] ?? "",
    target: parts.slice(2).join(":"),
  };
}

function isCatalogResourceTarget(target: string): boolean {
  return /^(artist|release|track|release_group):[0-9a-f-]{36}$/i.test(target);
}

export function isClaimAssignable(
  claim: string,
  capabilities: OrganizationCapabilities,
): boolean {
  const parsed = parseClaimSegments(claim);
  if (!parsed) {
    return false;
  }

  const { action, scope, target } = parsed;

  if (isCatalogResourceTarget(target)) {
    return action === "read" && (capabilities.canReadOrg || capabilities.canReadMembership);
  }

  switch (scope) {
    case "org":
      return (
        capabilities.canReadOrg &&
        (action === "read" || action === "manage")
      );
    case "membership":
      return (
        capabilities.canReadMembership &&
        (action === "read" || action === "manage")
      );
    case "member_permissions":
      return capabilities.canReadMembership && action === "manage";
    case "catalog":
      if (action === "read") {
        return capabilities.canReadOrg || capabilities.canReadMembership;
      }
      if (action === "upload") {
        return capabilities.canUpload;
      }
      if (action === "write_draft") {
        return capabilities.canWriteDraft;
      }
      if (action === "publish_public") {
        return capabilities.canPublishPublic;
      }
      if (action === "manage" && target === "pricing:all") {
        return capabilities.canPublishPublic;
      }
      return false;
    case "purchase":
      return (
        capabilities.canReadPayout &&
        action === "manage" &&
        target === "refund:all"
      );
    case "payout":
      if (action === "read" && target === "all") {
        return capabilities.canReadPayout;
      }
      if (
        action === "manage" &&
        (target === "profile:all" || target === "withdraw:all")
      ) {
        return capabilities.canReadPayout;
      }
      return false;
    default:
      return false;
  }
}

export function filterAssignableClaims(
  claims: string[],
  capabilities: OrganizationCapabilities,
): string[] {
  return [...new Set(claims)]
    .filter((claim) => isClaimAssignable(claim, capabilities))
    .sort((a, b) => a.localeCompare(b));
}

export function getAssignableScopeWideClaims(
  capabilities: OrganizationCapabilities,
): string[] {
  return filterAssignableClaims(ALL_SCOPE_WIDE_ASSIGNABLE_CLAIMS, capabilities);
}

export function getClaimGroupsForCapabilities(
  capabilities: OrganizationCapabilities,
): ClaimGroup[] {
  return SCOPE_WIDE_CLAIM_GROUPS.map((group) => ({
    ...group,
    claims: group.claims.filter((claim) => isClaimAssignable(claim, capabilities)),
  })).filter((group) => group.claims.length > 0);
}
