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
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import {
  deleteOrganization,
  getOrganization,
  leaveOrganization,
} from "@/lib/api/tenancyClient";
import type { OrganizationResponse } from "@/lib/api/tenancyClient";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function SettingsPage() {
  const router = useRouter();
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canManageOrg = hasClaim(token, "manage:org:all");

  const [organization, setOrganization] = useState<OrganizationResponse | null>(null);
  const [orgLoadError, setOrgLoadError] = useState<string | null>(null);
  const [leaveDialogOpen, setLeaveDialogOpen] = useState(false);
  const [leaveBusy, setLeaveBusy] = useState(false);
  const [leaveError, setLeaveError] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteBusy, setDeleteBusy] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const loadOrganization = useCallback(async () => {
    if (!orgId) {
      setOrganization(null);
      return;
    }
    setOrgLoadError(null);
    try {
      setOrganization(await getOrganization(orgId));
    } catch (e) {
      setOrganization(null);
      setOrgLoadError(e instanceof Error ? e.message : "Failed to load workspace.");
    }
  }, [orgId]);

  useEffect(() => {
    void loadOrganization();
  }, [loadOrganization]);

  async function onConfirmLeave() {
    if (!orgId) {
      return;
    }
    setLeaveBusy(true);
    setLeaveError(null);
    try {
      await leaveOrganization(orgId);
      setLeaveDialogOpen(false);
      const personas = await auth.reloadBusinessPersonas();
      const nextPersona =
        personas.find((persona) => persona.type !== "org" || persona.orgId !== orgId) ??
        personas[0];
      if (nextPersona) {
        await auth.selectPersona(nextPersona);
        router.replace("/dashboard");
      } else {
        router.replace("/select-persona?switch=1&returnTo=/dashboard");
      }
    } catch (e) {
      const message = e instanceof Error ? e.message : "Failed to leave organization.";
      setLeaveError(message);
      throw e;
    } finally {
      setLeaveBusy(false);
    }
  }

  async function onConfirmDelete() {
    if (!orgId) {
      return;
    }
    setDeleteBusy(true);
    setDeleteError(null);
    try {
      await deleteOrganization(orgId);
      setDeleteDialogOpen(false);
      const personas = await auth.reloadBusinessPersonas();
      const nextPersona =
        personas.find((persona) => persona.type !== "org" || persona.orgId !== orgId) ??
        personas[0];
      if (nextPersona) {
        await auth.selectPersona(nextPersona);
        router.replace("/dashboard");
      } else {
        router.replace("/select-persona?switch=1&returnTo=/dashboard");
      }
    } catch (e) {
      const message =
        e instanceof Error ? e.message : "Failed to delete organization.";
      setDeleteError(message);
      throw e;
    } finally {
      setDeleteBusy(false);
    }
  }

  const showOwnerDelete =
    organization?.isOwner === true && canManageOrg && organization.lifecycleStatus !== "closed";

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      {orgId && organization ? (
        <Card>
          <CardHeader>
            <CardTitle>Organization membership</CardTitle>
            <CardDescription>
              Your membership in {organization.displayName}.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {organization.isOwner ? (
              <p className="text-sm text-muted-foreground">
                As the organization owner, you cannot leave this organization. Transfer
                ownership to another member first, or contact platform support if you need
                an owner change.
              </p>
            ) : (
              <>
                <p className="text-sm text-muted-foreground">
                  Leave this organization to end your membership. You can be invited again
                  later with a new invite link.
                </p>
                {leaveError ? <p className="text-sm text-destructive">{leaveError}</p> : null}
                <Button
                  type="button"
                  variant="destructive"
                  disabled={leaveBusy}
                  onClick={() => setLeaveDialogOpen(true)}
                >
                  Leave organization
                </Button>
              </>
            )}
          </CardContent>
        </Card>
      ) : orgId && orgLoadError ? (
        <p className="text-sm text-destructive">{orgLoadError}</p>
      ) : null}

      {showOwnerDelete ? (
        <Card className="border-destructive/30">
          <CardHeader>
            <CardTitle>Delete organization</CardTitle>
            <CardDescription>
              Permanently close {organization.displayName} for all members. The organization
              is removed from your list; platform support can recover it if needed.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              Only the organization owner can do this. Members will lose access immediately.
              This is a soft delete — data is retained for platform recovery.
            </p>
            {deleteError ? <p className="text-sm text-destructive">{deleteError}</p> : null}
            <Button
              type="button"
              variant="destructive"
              disabled={deleteBusy || leaveBusy}
              onClick={() => setDeleteDialogOpen(true)}
            >
              Delete organization
            </Button>
          </CardContent>
        </Card>
      ) : organization?.isOwner && !canManageOrg ? (
        <p className="text-sm text-muted-foreground">
          Your session is missing organization management permission. Switch workspace or
          sign in again, then return here to delete the organization.
        </p>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Settings</CardTitle>
          <CardDescription>
            Account and workspace preferences for the business portal.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-6">
          <section className="flex flex-col gap-2">
            <h2 className="text-sm font-medium">Organizations</h2>
            <p className="text-sm text-muted-foreground">
              Create another indie group or backing organization on your signed-in
              account. Platform operators can still review backing org applications
              from Applications.
            </p>
            <Button render={<Link href="/create-organization?returnTo=/settings" />}>
              Add organization
            </Button>
          </section>
        </CardContent>
      </Card>

      <ConfirmDialog
        open={leaveDialogOpen}
        onOpenChange={(open) => {
          if (!open && !leaveBusy) {
            setLeaveDialogOpen(false);
          }
        }}
        title="Leave organization?"
        description={
          organization
            ? `You will lose access to ${organization.displayName} in this portal. An admin can invite you again later.`
            : ""
        }
        confirmLabel="Leave organization"
        destructive
        busy={leaveBusy}
        onConfirm={onConfirmLeave}
      />

      <ConfirmDialog
        open={deleteDialogOpen}
        onOpenChange={(open) => {
          if (!open && !deleteBusy) {
            setDeleteDialogOpen(false);
          }
        }}
        title="Delete organization?"
        description={
          organization
            ? `This will close ${organization.displayName} for everyone. You and all members will lose access. Platform operators may be able to recover the organization later.`
            : ""
        }
        confirmLabel="Delete organization"
        destructive
        busy={deleteBusy}
        onConfirm={onConfirmDelete}
      />
    </div>
  );
}
