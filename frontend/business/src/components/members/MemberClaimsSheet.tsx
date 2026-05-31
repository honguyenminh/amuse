"use client";

import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import type { ClaimPresetResponse, OrganizationMemberResponse } from "@/lib/api/tenancyClient";
import { describeClaim, getPresetDisplayName } from "@/lib/members/presetDisplay";
import { Check, Copy } from "lucide-react";
import { useMemo, useState } from "react";

type MemberClaimsSheetProps = {
  member: OrganizationMemberResponse | null;
  presets: ClaimPresetResponse[];
  canManagePermissions: boolean;
  onEditRole: (member: OrganizationMemberResponse) => void;
  onOpenChange: (open: boolean) => void;
};

export function MemberClaimsSheet({
  member,
  presets,
  canManagePermissions,
  onEditRole,
  onOpenChange,
}: MemberClaimsSheetProps) {
  const [copied, setCopied] = useState(false);
  const open = member !== null;

  const sortedClaims = useMemo(
    () => [...(member?.claims ?? [])].sort((a, b) => a.localeCompare(b)),
    [member?.claims],
  );

  const displayName = member?.email ?? member?.accountId ?? "Member";

  async function onCopyAll() {
    if (!member || member.claims.length === 0) {
      return;
    }
    try {
      await navigator.clipboard.writeText(member.claims.join("\n"));
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="flex w-full flex-col sm:max-w-md">
        <SheetHeader className="border-b pb-4">
          <SheetTitle className="pr-8">{displayName}</SheetTitle>
          <SheetDescription className="space-y-1">
            {member?.isOwner ? <span className="block text-foreground">Organization owner</span> : null}
            <span className="block">
              Role:{" "}
              <span className="text-foreground">
                {getPresetDisplayName(presets, member?.presetRoleLabel)}
              </span>
            </span>
            <span className="block">
              {sortedClaims.length} claim{sortedClaims.length === 1 ? "" : "s"}
            </span>
          </SheetDescription>
        </SheetHeader>

        <div className="min-h-0 flex-1 overflow-y-auto px-4">
          {sortedClaims.length === 0 ? (
            <p className="py-6 text-sm text-muted-foreground">This member has no claims assigned.</p>
          ) : (
            <ul className="divide-y">
              {sortedClaims.map((claim) => (
                <li key={claim} className="py-3">
                  <div className="flex items-start gap-2">
                    <Check className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-foreground">{describeClaim(claim)}</p>
                      <code className="mt-1 block break-all text-xs text-muted-foreground">{claim}</code>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <SheetFooter className="border-t pt-4">
          {canManagePermissions && member && !member.isOwner ? (
            <Button
              type="button"
              className="w-full"
              onClick={() => {
                onOpenChange(false);
                onEditRole(member);
              }}
            >
              Change role
            </Button>
          ) : null}
          <Button
            type="button"
            variant="outline"
            className="w-full"
            disabled={sortedClaims.length === 0}
            onClick={() => void onCopyAll()}
          >
            {copied ? <Check /> : <Copy />}
            {copied ? "Copied" : "Copy all claims"}
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
