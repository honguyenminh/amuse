import { hasClaim, readAccessTokenClaims } from "@/lib/auth/jwtClaims";

/**
 * Mirror of Amuse.Domain.Platform.PlatformClaims — keep in sync when adding platform claims.
 * Prefer these helpers over raw hasClaim("manage:platform:organizations") in UI code.
 */
export const PLATFORM_ROOT = "platform:root";
export const PLATFORM_REVIEW_ORGANIZATIONS = "review:platform:organizations";
export const PLATFORM_MANAGE_ORGANIZATIONS = "manage:platform:organizations";
export const PLATFORM_MANAGE_ALL = "manage:platform:all";
export const PLATFORM_READ_ACCOUNTING = "read:platform:accounting:all";
export const PLATFORM_MANAGE_ACCOUNTING = "manage:platform:accounting:all";
export const PLATFORM_MANAGE_PURCHASES = "manage:platform:purchases:all";
export const PLATFORM_MANAGE_PAYOUTS = "manage:platform:payouts:all";
export const PLATFORM_LEGACY_REVIEW = "platform:organizations:review";

function claimSet(accessToken: string | null): Set<string> {
  if (!accessToken) {
    return new Set();
  }
  return new Set(readAccessTokenClaims(accessToken));
}

/** Full platform break-glass (root or manage:platform:all). */
export function isPlatformRoot(accessToken: string | null): boolean {
  const granted = claimSet(accessToken);
  return granted.has(PLATFORM_ROOT) || granted.has(PLATFORM_MANAGE_ALL);
}

/** Approve/reject applications and instant-approve backing orgs on create. */
export function canReviewPlatformOrganizations(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return (
    hasClaim(accessToken, PLATFORM_REVIEW_ORGANIZATIONS) ||
    hasClaim(accessToken, PLATFORM_MANAGE_ORGANIZATIONS) ||
    hasClaim(accessToken, PLATFORM_LEGACY_REVIEW)
  );
}

/** Recover closed orgs, force-transfer ownership, assume any org persona. */
export function canManagePlatformOrganizations(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return hasClaim(accessToken, PLATFORM_MANAGE_ORGANIZATIONS);
}

/** View tax invoices, VAT summaries, and accounting exports. */
export function canReadPlatformAccounting(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return (
    hasClaim(accessToken, PLATFORM_READ_ACCOUNTING) ||
    hasClaim(accessToken, PLATFORM_MANAGE_ACCOUNTING)
  );
}

/** Issue credit notes, accounting adjustments, and FX overrides. */
export function canManagePlatformAccounting(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return hasClaim(accessToken, PLATFORM_MANAGE_ACCOUNTING);
}

/** Refund any purchase and set refund fee bearer. */
export function canManagePlatformPurchases(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return hasClaim(accessToken, PLATFORM_MANAGE_PURCHASES);
}

/** Approve or reject seller withdrawals above auto threshold. */
export function canManagePlatformPayouts(accessToken: string | null): boolean {
  if (isPlatformRoot(accessToken)) {
    return true;
  }
  return hasClaim(accessToken, PLATFORM_MANAGE_PAYOUTS);
}
