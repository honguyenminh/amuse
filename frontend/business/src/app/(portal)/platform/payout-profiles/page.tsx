"use client";

import { PlatformPersonaGate } from "@/components/portal/PlatformPersonaGate";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  approvePlatformPayoutProfile,
  listPlatformPayoutProfiles,
  rejectPlatformPayoutProfile,
  type PlatformPayoutProfileRow,
} from "@/lib/api/financeClient";
import { canManagePlatformPayouts } from "@/lib/auth/platformClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { useCallback, useEffect, useState } from "react";

function formatTimestamp(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export default function PlatformPayoutProfilesPage() {
  return (
    <PlatformPersonaGate>
      <PayoutProfilesContent />
    </PlatformPersonaGate>
  );
}

function PayoutProfilesContent() {
  const token = getAccessToken();
  const canManage = canManagePlatformPayouts(token);

  const [profiles, setProfiles] = useState<PlatformPayoutProfileRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listPlatformPayoutProfiles("underReview");
      setProfiles(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load payout profiles.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function onApprove(organizationId: string) {
    setBusyId(organizationId);
    try {
      await approvePlatformPayoutProfile(organizationId);
      setProfiles((items) => items.filter((item) => item.organizationId !== organizationId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Approval failed.");
    } finally {
      setBusyId(null);
    }
  }

  async function onReject(organizationId: string) {
    const reason = window.prompt("Rejection reason (required):");
    if (!reason?.trim()) {
      return;
    }
    setBusyId(organizationId);
    try {
      await rejectPlatformPayoutProfile(organizationId, reason.trim());
      setProfiles((items) => items.filter((item) => item.organizationId !== organizationId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Rejection failed.");
    } finally {
      setBusyId(null);
    }
  }

  if (!canManage) {
    return (
      <p className="text-sm text-muted-foreground">
        Your platform token does not include payout review permissions.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Payout profile review</CardTitle>
          <CardDescription>
            Review seller KYC and bank details (Gate B) before withdrawals are enabled.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {loading ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-32 w-full" />
            </div>
          ) : null}

          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          {!loading && profiles.length === 0 ? (
            <p className="text-sm text-muted-foreground">No profiles awaiting review.</p>
          ) : null}

          <ul className="flex flex-col gap-4">
            {profiles.map((item) => {
              const busy = busyId === item.organizationId;
              return (
                <li
                  key={item.id}
                  className="flex flex-col gap-4 rounded-lg border bg-card p-4"
                >
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div className="space-y-1">
                      <p className="text-lg font-semibold">{item.legalName}</p>
                      <p className="text-sm text-muted-foreground">
                        {item.legalEntityType} · {item.countryCode} · {item.payoutRail}
                      </p>
                      <p className="font-mono text-xs text-muted-foreground">
                        Org {item.organizationId}
                      </p>
                    </div>
                    <div className="flex shrink-0 gap-2">
                      <Button size="sm" disabled={busy} onClick={() => void onApprove(item.organizationId)}>
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={busy}
                        onClick={() => void onReject(item.organizationId)}
                      >
                        Reject
                      </Button>
                    </div>
                  </div>
                  <dl className="grid gap-2 text-sm sm:grid-cols-2">
                    <div>
                      <dt className="text-muted-foreground">Bank</dt>
                      <dd>
                        {item.bankName ?? "—"}{" "}
                        {item.bankAccountMasked ? `(${item.bankAccountMasked})` : ""}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Updated</dt>
                      <dd>{formatTimestamp(item.updatedAt)}</dd>
                    </div>
                    <div className="sm:col-span-2">
                      <dt className="text-muted-foreground">Documents</dt>
                      <dd className="break-all font-mono text-xs">
                        {item.documentObjectKeys.join(", ") || "—"}
                      </dd>
                    </div>
                  </dl>
                </li>
              );
            })}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
