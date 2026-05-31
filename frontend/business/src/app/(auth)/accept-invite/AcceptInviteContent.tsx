"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { refreshTokens } from "@/lib/api/identityClient";
import { ApiError } from "@/lib/api/types";
import {
  acceptOrganizationInvite,
  declineOrganizationInvite,
  getInvitePreview,
  type InvitePreviewResponse,
} from "@/lib/api/tenancyClient";
import { useAuth } from "@/lib/auth/AuthProvider";
import { listenerBootstrapContext } from "@/lib/auth/listenerBootstrapContext";
import { safeReturnPath } from "@/lib/auth/safeReturnPath";
import {
  setAccessToken,
  setActivePersona,
} from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

function normalizeEmail(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }
  const trimmed = value.trim().toLowerCase();
  return trimmed.length > 0 ? trimmed : null;
}

function inviteStatusLabel(status: string): string {
  switch (status.toLowerCase()) {
    case "accepted":
      return "This invitation has already been accepted.";
    case "revoked":
      return "This invitation is no longer available.";
    case "expired":
      return "This invitation has expired.";
    default:
      return "This invitation cannot be used.";
  }
}

export function AcceptInviteContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const router = useRouter();
  const auth = useAuth();

  const [preview, setPreview] = useState<InvitePreviewResponse | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [previewLoading, setPreviewLoading] = useState(true);

  const returnPath = useMemo(
    () =>
      token
        ? safeReturnPath(`/accept-invite?token=${encodeURIComponent(token)}`)
        : "/accept-invite",
    [token],
  );

  const loginHref = `/login?next=${encodeURIComponent(returnPath)}`;
  const signupHref = useMemo(() => {
    const params = new URLSearchParams({ next: returnPath });
    if (preview?.email) {
      params.set("email", preview.email);
    }
    return `/signup?${params.toString()}`;
  }, [returnPath, preview?.email]);

  useEffect(() => {
    if (!token) {
      setPreviewError("Missing invite token.");
      setPreviewLoading(false);
      return;
    }

    setPreviewLoading(true);
    setPreviewError(null);
    void getInvitePreview(token)
      .then(setPreview)
      .catch(() => setPreviewError("Invite not found or expired."))
      .finally(() => setPreviewLoading(false));
  }, [token]);

  const invitedEmail = normalizeEmail(preview?.email);
  const signedInEmail = normalizeEmail(auth.account?.email);
  const isPending = preview?.status?.toLowerCase() === "pending";
  const emailMatches =
    invitedEmail !== null && signedInEmail !== null && invitedEmail === signedInEmail;
  const canRespond =
    auth.isReady && auth.isAuthenticated && isPending && emailMatches && !previewLoading;

  async function ensureAccountSession() {
    const refreshed = await refreshTokens(listenerBootstrapContext);
    setAccessToken(refreshed.accessToken);
    setActivePersona(listenerBootstrapContext);
  }

  async function onAccept() {
    if (!token || !canRespond) {
      return;
    }
    setBusy(true);
    setActionError(null);
    try {
      await ensureAccountSession();
      const accepted = await acceptOrganizationInvite(token);
      const personas = await auth.reloadBusinessPersonas();
      const orgPersona = personas.find(
        (persona) => persona.type === "org" && persona.orgId === accepted.organizationId,
      );
      if (orgPersona) {
        await auth.selectPersona(orgPersona);
        router.replace("/dashboard");
        return;
      }
      router.replace("/select-persona?next=/dashboard");
    } catch (e) {
      if (e instanceof ApiError && e.code === "tenancy.invite_email_mismatch") {
        setActionError("Sign in with the invited email address to accept this invitation.");
      } else {
        setActionError(e instanceof Error ? e.message : "Failed to accept invite.");
      }
    } finally {
      setBusy(false);
    }
  }

  async function onDecline() {
    if (!token || !canRespond) {
      return;
    }
    setBusy(true);
    setActionError(null);
    try {
      await ensureAccountSession();
      await declineOrganizationInvite(token);
      router.replace("/login");
    } catch (e) {
      if (e instanceof ApiError && e.code === "tenancy.invite_email_mismatch") {
        setActionError("Sign in with the invited email address to decline this invitation.");
      } else {
        setActionError(e instanceof Error ? e.message : "Failed to decline invite.");
      }
    } finally {
      setBusy(false);
    }
  }

  async function onSignOutToSwitch() {
    await auth.logout();
    router.replace(loginHref);
  }

  if (!auth.isReady || previewLoading) {
    return (
      <div className="mx-auto flex min-h-dvh w-full max-w-lg items-center p-6">
        <Card className="w-full">
          <CardHeader>
            <CardTitle>Organization invitation</CardTitle>
            <CardDescription>Loading invitation…</CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-lg items-center p-6">
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Organization invitation</CardTitle>
          <CardDescription>
            {preview
              ? `${preview.organizationDisplayName} invited ${preview.email}`
              : "Review this invitation before joining the organization."}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {previewError ? (
            <p className="text-sm text-destructive">{previewError}</p>
          ) : null}

          {preview && !isPending ? (
            <p className="text-sm text-muted-foreground">
              {inviteStatusLabel(preview.status)}
            </p>
          ) : null}

          {actionError ? <p className="text-sm text-destructive">{actionError}</p> : null}

          {preview && isPending ? (
            <>
              {!auth.isAuthenticated ? (
                <div className="flex flex-col gap-3 rounded-md border bg-muted/30 p-4">
                  <p className="text-sm text-foreground">
                    Sign in to <span className="font-medium">{preview.email}</span> to accept or
                    decline this invitation.
                  </p>
                  <p className="text-sm text-muted-foreground">
                    If you do not have an account yet, create one using that same email address.
                  </p>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <Button className="flex-1" render={<Link href={loginHref} />}>
                      Sign in
                    </Button>
                    <Button
                      variant="outline"
                      className="flex-1"
                      render={<Link href={signupHref} />}
                    >
                      Create account
                    </Button>
                  </div>
                </div>
              ) : null}

              {auth.isAuthenticated && !emailMatches ? (
                <div className="flex flex-col gap-3 rounded-md border border-amber-500/40 bg-amber-500/5 p-4">
                  <p className="text-sm text-foreground">
                    You are signed in as{" "}
                    <span className="font-medium">{auth.account?.email ?? "another account"}</span>,
                    but this invitation was sent to{" "}
                    <span className="font-medium">{preview.email}</span>.
                  </p>
                  <p className="text-sm text-muted-foreground">
                    Sign out and sign in with the invited email address to continue.
                  </p>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <Button
                      variant="outline"
                      className="flex-1"
                      disabled={busy}
                      onClick={() => void onSignOutToSwitch()}
                    >
                      Sign out
                    </Button>
                    <Button className="flex-1" render={<Link href={loginHref} />}>
                      Sign in as invited user
                    </Button>
                  </div>
                </div>
              ) : null}

              {canRespond ? (
                <div className="flex flex-col gap-3">
                  <p className="text-sm text-muted-foreground">
                    You are signed in as <span className="font-medium">{preview.email}</span>.
                    Join {preview.organizationDisplayName}?
                  </p>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <Button
                      className="flex-1"
                      disabled={busy}
                      onClick={() => void onAccept()}
                    >
                      {busy ? "Working…" : "Accept invitation"}
                    </Button>
                    <Button
                      variant="outline"
                      className="flex-1"
                      disabled={busy}
                      onClick={() => void onDecline()}
                    >
                      Decline
                    </Button>
                  </div>
                </div>
              ) : null}
            </>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
