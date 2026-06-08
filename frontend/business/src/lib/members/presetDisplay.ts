import type { ClaimPresetResponse } from "@/lib/api/tenancyClient";
import {
  catalogResourceKindLabel,
  parseCatalogReadClaim,
} from "@/lib/members/catalogResourceClaims";
import {
  Disc3,
  Eye,
  ShieldCheck,
  Users,
  type LucideIcon,
} from "lucide-react";

const PRESET_ICONS: Record<string, LucideIcon> = {
  "shield-check": ShieldCheck,
  users: Users,
  "disc-3": Disc3,
  eye: Eye,
};

const CLAIM_LABELS: Record<string, string> = {
  "read:org:all": "View organization profile and settings",
  "manage:org:all": "Update organization settings and transfer ownership",
  "read:membership:all": "View members and pending invites",
  "manage:membership:all": "Invite, update, and remove members",
  "manage:member_permissions:all": "Adjust member roles and fine-grained claims",
  "read:catalog:all": "View catalog",
  "upload:catalog:all": "Upload catalog masters",
  "write_draft:catalog:all": "Create and edit catalog drafts",
  "publish_public:catalog:all": "Publish catalog to the public",
  "manage:catalog:pricing:all": "Set track and release pricing (pay what you want)",
  "manage:purchase:refund:all": "Initiate refunds on purchases where the org is a payee",
  "read:payout:all": "View payout balance and statements",
  "manage:payout:profile:all": "Submit and update payout profile (Gate B KYC)",
  "manage:payout:withdraw:all": "Request seller withdrawals",
};

export function getPresetIcon(iconName: string): LucideIcon {
  return PRESET_ICONS[iconName] ?? ShieldCheck;
}

export function findPreset(
  presets: ClaimPresetResponse[],
  label: string | null | undefined,
): ClaimPresetResponse | undefined {
  if (!label) {
    return undefined;
  }
  return presets.find((preset) => preset.label === label);
}

export function getPresetDisplayName(
  presets: ClaimPresetResponse[],
  label: string | null | undefined,
): string {
  if (!label) {
    return "Custom";
  }
  return findPreset(presets, label)?.displayName ?? label;
}

export function describeClaim(
  claim: string,
  resourceLabels?: Record<string, string>,
): string {
  const catalogResource = parseCatalogReadClaim(claim);
  if (catalogResource) {
    const labelKey = `${catalogResource.kind}:${catalogResource.id}`;
    const name = resourceLabels?.[labelKey];
    const kindLabel = catalogResourceKindLabel(catalogResource.kind);
    if (name) {
      return `View ${kindLabel.toLowerCase()}: ${name}`;
    }
    return `View ${kindLabel.toLowerCase()} (${catalogResource.id.slice(0, 8)}…)`;
  }

  return CLAIM_LABELS[claim] ?? claim;
}
