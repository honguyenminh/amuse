"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { ClaimPresetResponse, OrganizationMemberResponse } from "@/lib/api/tenancyClient";
import {
  describeClaim,
  getPresetDisplayName,
  getPresetIcon,
} from "@/lib/members/presetDisplay";
import { cn } from "@/lib/utils";
import { Check, Lock } from "lucide-react";
import { useState } from "react";

type MemberRoleDialogProps = {
  member: OrganizationMemberResponse | null;
  presets: ClaimPresetResponse[];
  busy: boolean;
  onOpenChange: (open: boolean) => void;
  onApplyPreset: (memberId: string, presetLabel: string) => Promise<void>;
};

export function MemberRoleDialog({
  member,
  presets,
  busy,
  onOpenChange,
  onApplyPreset,
}: MemberRoleDialogProps) {
  const open = member !== null;
  const [pendingPreset, setPendingPreset] = useState<string | null>(
    member?.presetRoleLabel ?? null,
  );

  const displayName = member?.email ?? member?.accountId ?? "Member";
  const currentPresetLabel = member?.presetRoleLabel ?? null;
  const hasChanges = pendingPreset !== null && pendingPreset !== currentPresetLabel;

  async function onSave() {
    if (!member || !pendingPreset || !hasChanges) {
      return;
    }
    await onApplyPreset(member.id, pendingPreset);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>Assign role</DialogTitle>
          <DialogDescription>
            Choose a preset role for <span className="font-medium text-foreground">{displayName}</span>.
            Preset roles bundle permissions that control what this member can access.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="min-h-0 flex-1 overflow-y-auto py-2">
          <div className="flex flex-col gap-3">
            {presets.map((preset) => {
              const Icon = getPresetIcon(preset.icon);
              const selected = pendingPreset === preset.label;
              const isCurrent = currentPresetLabel === preset.label;

              return (
                <button
                  key={preset.label}
                  type="button"
                  disabled={busy}
                  onClick={() => setPendingPreset(preset.label)}
                  className={cn(
                    "rounded-lg border p-4 text-left transition-colors",
                    selected
                      ? "border-primary bg-primary/5 ring-1 ring-primary"
                      : "border-border bg-background hover:bg-muted/40",
                  )}
                >
                  <div className="flex items-start gap-3">
                    <div
                      className={cn(
                        "flex size-10 shrink-0 items-center justify-center rounded-md border",
                        selected ? "border-primary bg-primary/10 text-primary" : "bg-muted text-muted-foreground",
                      )}
                    >
                      <Icon className="size-5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="font-medium text-foreground">{preset.displayName}</span>
                        {isCurrent ? (
                          <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                            Current
                          </span>
                        ) : null}
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground">{preset.description}</p>
                      <ul className="mt-3 space-y-1.5">
                        {preset.claims.map((claim) => (
                          <li key={claim} className="flex items-start gap-2 text-xs text-muted-foreground">
                            <Check className="mt-0.5 size-3.5 shrink-0 text-foreground/70" />
                            <span>{describeClaim(claim)}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>

          <div className="mt-6 rounded-lg border border-dashed bg-muted/20 p-4">
            <div className="flex items-start gap-3">
              <div className="flex size-10 shrink-0 items-center justify-center rounded-md border bg-muted text-muted-foreground">
                <Lock className="size-5" />
              </div>
              <div>
                <p className="font-medium text-foreground">Fine-grained claims</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Custom per-resource permissions will be available here once catalog resources can be
                  managed from the portal. For now, use preset roles above.
                </p>
              </div>
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" disabled={busy} onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" disabled={busy || !hasChanges} onClick={() => void onSave()}>
            {busy ? "Saving…" : "Save role"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export { getPresetDisplayName };
