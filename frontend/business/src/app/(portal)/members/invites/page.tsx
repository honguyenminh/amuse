"use client";

import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import {
  listOrganizationInvites,
  listClaimPresets,
  revokeOrganizationInvite,
  type ClaimPresetResponse,
  type OrganizationInviteResponse,
} from "@/lib/api/tenancyClient";
import { getPresetDisplayName } from "@/lib/members/presetDisplay";
import { formatDateTime } from "@/lib/format/dateTime";
import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

export default function PendingInvitesPage() {
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canManage = hasClaim(token, "manage:membership:all");

  const [invites, setInvites] = useState<OrganizationInviteResponse[]>([]);
  const [presets, setPresets] = useState<ClaimPresetResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [inviteToRevoke, setInviteToRevoke] = useState<OrganizationInviteResponse | null>(
    null,
  );

  const load = useCallback(async () => {
    if (!orgId || !canManage) {
      return;
    }
    setError(null);
    try {
      setInvites(await listOrganizationInvites(orgId));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load invites.");
    }
  }, [orgId, canManage]);

  useEffect(() => {
    void listClaimPresets().then(setPresets).catch(() => undefined);
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">Select an organization workspace.</p>
    );
  }

  if (!canManage) {
    return (
      <p className="text-sm text-muted-foreground">
        You do not have permission to manage pending invites.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <Button
        variant="ghost"
        className="w-fit"
        render={<Link href="/members" />}
      >
        <ArrowLeft />
        Back to members
      </Button>

      <Card>
        <CardHeader>
          <CardTitle>Pending invites</CardTitle>
          <CardDescription>
            Invitations that have not been accepted yet. Revoke any that are no longer needed.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          {invites.length === 0 ? (
            <p className="text-sm text-muted-foreground">No pending invites.</p>
          ) : (
            invites.map((invite) => (
              <div
                key={invite.id}
                className="flex flex-col gap-2 rounded-md border bg-background p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <p className="font-medium">{invite.email}</p>
                  <p className="text-xs text-muted-foreground">
                    {getPresetDisplayName(presets, invite.presetRoleLabel)} · expires{" "}
                    {formatDateTime(invite.expiresAt)}
                  </p>
                  <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">
                    {invite.claims.join(", ")}
                  </p>
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busy}
                  onClick={() => setInviteToRevoke(invite)}
                >
                  Revoke
                </Button>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <ConfirmDialog
        open={inviteToRevoke !== null}
        onOpenChange={(open) => {
          if (!open && !busy) {
            setInviteToRevoke(null);
          }
        }}
        title="Revoke invite?"
        description={
          inviteToRevoke
            ? `${inviteToRevoke.email} will no longer be able to accept this invitation.`
            : ""
        }
        confirmLabel="Revoke invite"
        destructive
        busy={busy}
        onConfirm={async () => {
          if (!orgId || !inviteToRevoke) {
            return;
          }
          setBusy(true);
          setError(null);
          try {
            await revokeOrganizationInvite(orgId, inviteToRevoke.id);
            setInviteToRevoke(null);
            await load();
          } catch (e) {
            setError(e instanceof Error ? e.message : "Failed to revoke invite.");
            throw e;
          } finally {
            setBusy(false);
          }
        }}
      />
    </div>
  );
}
