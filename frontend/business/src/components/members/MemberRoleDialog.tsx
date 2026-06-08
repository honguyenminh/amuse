"use client";

import { CatalogResourceClaimsPicker } from "@/components/members/CatalogResourceClaimsPicker";
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
import type {
  ClaimPresetResponse,
  OrganizationCapabilities,
  OrganizationMemberResponse,
} from "@/lib/api/tenancyClient";
import { getClaimGroupsForCapabilities } from "@/lib/members/assignableClaims";
import { CATALOG_READ_ALL_CLAIM } from "@/lib/members/catalogResourceClaims";
import {
  applyPresetSelection,
  selectionsEqual,
  setCatalogResourceClaims,
  toggleScopeWideClaim,
  type PermissionSelection,
} from "@/lib/members/permissionSelection";
import {
  describeClaim,
  getPresetDisplayName,
  getPresetIcon,
} from "@/lib/members/presetDisplay";
import { cn } from "@/lib/utils";
import { Check } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

type MemberRoleDialogProps = {
  mode: "invite" | "edit";
  open: boolean;
  member: OrganizationMemberResponse | null;
  presets: ClaimPresetResponse[];
  capabilities: OrganizationCapabilities | null;
  initialSelection: PermissionSelection;
  busy: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: (selection: PermissionSelection) => Promise<void>;
};

export function MemberRoleDialog({
  mode,
  open,
  member,
  presets,
  capabilities,
  initialSelection,
  busy,
  onOpenChange,
  onConfirm,
}: MemberRoleDialogProps) {
  const [selection, setSelection] = useState<PermissionSelection>(initialSelection);

  useEffect(() => {
    if (open) {
      setSelection(initialSelection);
    }
  }, [open, initialSelection]);

  const displayName =
    mode === "invite"
      ? "new member"
      : (member?.email ?? member?.accountId ?? "Member");

  const hasChanges = !selectionsEqual(selection, initialSelection);
  const claimGroups = useMemo(
    () => (capabilities ? getClaimGroupsForCapabilities(capabilities) : []),
    [capabilities],
  );
  const catalogReadAllEnabled = selection.claims.includes(CATALOG_READ_ALL_CLAIM);
  const resourceClaims = selection.claims.filter((claim) => claim.startsWith("read:catalog:") && claim !== CATALOG_READ_ALL_CLAIM);

  async function onSave() {
    if (!hasChanges) {
      return;
    }
    await onConfirm(selection);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>{mode === "invite" ? "Choose permissions" : "Assign permissions"}</DialogTitle>
          <DialogDescription>
            {mode === "invite" ? (
              <>Select preset roles or customize individual permissions for the invited member.</>
            ) : (
              <>
                Configure permissions for{" "}
                <span className="font-medium text-foreground">{displayName}</span>. Preset roles
                bundle common access patterns; you can also fine-tune scope-wide and catalog
                resource access below.
              </>
            )}
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="min-h-0 flex-1 overflow-y-auto py-2">
          <div className="flex flex-col gap-3">
            {presets.map((preset) => {
              const Icon = getPresetIcon(preset.icon);
              const selected = selection.presetLabel === preset.label;
              const isCurrent =
                mode === "edit" && member?.presetRoleLabel === preset.label && selected;

              return (
                <button
                  key={preset.label}
                  type="button"
                  disabled={busy}
                  onClick={() => setSelection(applyPresetSelection(preset))}
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
                        selected
                          ? "border-primary bg-primary/10 text-primary"
                          : "bg-muted text-muted-foreground",
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

          {capabilities ? (
            <div className="mt-6 space-y-4 rounded-lg border bg-muted/10 p-4">
              <div>
                <p className="font-medium text-foreground">Scope-wide permissions</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  Toggle individual organization permissions. Selecting a preset above replaces
                  these toggles.
                </p>
              </div>

              {claimGroups.map((group) => (
                <div key={group.id} className="space-y-2">
                  <p className="text-sm font-medium text-foreground">{group.label}</p>
                  <div className="space-y-2">
                    {group.claims.map((claim) => {
                      const enabled = selection.claims.includes(claim);
                      return (
                        <label
                          key={claim}
                          className="flex cursor-pointer items-start gap-3 rounded-md border bg-background px-3 py-2"
                        >
                          <input
                            type="checkbox"
                            className="mt-1"
                            checked={enabled}
                            disabled={busy}
                            onChange={(event) =>
                              setSelection(
                                toggleScopeWideClaim(
                                  selection,
                                  presets,
                                  claim,
                                  event.target.checked,
                                ),
                              )
                            }
                          />
                          <span className="min-w-0">
                            <span className="block text-sm text-foreground">
                              {describeClaim(claim)}
                            </span>
                            <code className="mt-0.5 block text-xs text-muted-foreground">{claim}</code>
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </div>
              ))}

              {!selection.claims.includes("read:org:all") ? (
                <p className="text-sm text-amber-700 dark:text-amber-300">
                  Consider including <code className="text-xs">read:org:all</code> so this member
                  can view basic organization settings.
                </p>
              ) : null}
            </div>
          ) : null}

          <div className="mt-6 space-y-3 rounded-lg border border-dashed bg-muted/20 p-4">
            <div>
              <p className="font-medium text-foreground">Catalog resource access</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Grant read access to specific artists, releases, tracks, or release groups instead
                of the entire catalog.
              </p>
            </div>
            <CatalogResourceClaimsPicker
              disabled={catalogReadAllEnabled}
              selectedClaims={resourceClaims}
              onSelectedClaimsChange={(claims) =>
                setSelection(setCatalogResourceClaims(selection, presets, claims))
              }
            />
          </div>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" disabled={busy} onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" disabled={busy || !hasChanges} onClick={() => void onSave()}>
            {busy
              ? mode === "invite"
                ? "Applying…"
                : "Saving…"
              : mode === "invite"
                ? "Apply permissions"
                : "Save permissions"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export { getPresetDisplayName };
