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
import { getOrganization, leaveOrganization } from "@/lib/api/tenancyClient";
import type { OrganizationResponse } from "@/lib/api/tenancyClient";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function SettingsPage() {
  const router = useRouter();
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;

  const [organization, setOrganization] = useState<OrganizationResponse | null>(null);
  const [orgLoadError, setOrgLoadError] = useState<string | null>(null);
  const [leaveDialogOpen, setLeaveDialogOpen] = useState(false);
  const [leaveBusy, setLeaveBusy] = useState(false);
  const [leaveError, setLeaveError] = useState<string | null>(null);

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
    </div>
  );
}
