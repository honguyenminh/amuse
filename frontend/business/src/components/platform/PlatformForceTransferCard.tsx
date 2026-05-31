"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { forceTransferOrganizationOwnership } from "@/lib/api/platformClient";
import { useState } from "react";

export function PlatformForceTransferCard() {
  const [organizationId, setOrganizationId] = useState("");
  const [targetMemberId, setTargetMemberId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  async function onConfirm() {
    const orgId = organizationId.trim();
    const memberId = targetMemberId.trim();
    if (!orgId || !memberId) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      await forceTransferOrganizationOwnership(orgId, memberId);
      setConfirmOpen(false);
      setOrganizationId("");
      setTargetMemberId("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Force transfer failed.");
      throw err;
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Force transfer ownership</CardTitle>
          <CardDescription>
            Support-only action when the current owner cannot transfer (lost access,
            compromise, or legal request). The target must be an active member of the
            organization.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <label htmlFor="force-transfer-org-id" className="text-sm font-medium">
                Organization ID
              </label>
              <Input
                id="force-transfer-org-id"
                value={organizationId}
                onChange={(event) => setOrganizationId(event.target.value)}
                placeholder="00000000-0000-7000-8000-000000000001"
                className="font-mono text-xs"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label htmlFor="force-transfer-member-id" className="text-sm font-medium">
                New owner member ID
              </label>
              <Input
                id="force-transfer-member-id"
                value={targetMemberId}
                onChange={(event) => setTargetMemberId(event.target.value)}
                placeholder="Member row ID from /members"
                className="font-mono text-xs"
              />
            </div>
          </div>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          <Button
            type="button"
            variant="destructive"
            disabled={busy || !organizationId.trim() || !targetMemberId.trim()}
            onClick={() => setConfirmOpen(true)}
          >
            Force transfer ownership
          </Button>
        </CardContent>
      </Card>

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={(open) => {
          if (!open && !busy) {
            setConfirmOpen(false);
          }
        }}
        title="Force transfer ownership?"
        description="The current owner will lose owner status. The target member becomes owner with full admin claims. This cannot be undone from the portal."
        confirmLabel="Transfer ownership"
        destructive
        busy={busy}
        onConfirm={onConfirm}
      />
    </>
  );
}
