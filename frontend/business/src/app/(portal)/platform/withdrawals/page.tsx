"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  approvePlatformWithdrawal,
  completePlatformWithdrawal,
  failPlatformWithdrawal,
  listPlatformWithdrawals,
  type PlatformWithdrawalRow,
} from "@/lib/api/financeClient";
import { formatMinor } from "@/lib/finance/formatMoney";
import { canManagePlatformPayouts } from "@/lib/auth/platformClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { useCallback, useEffect, useState } from "react";

export default function PlatformWithdrawalsPage() {
  const token = getAccessToken();
  const canManage = canManagePlatformPayouts(token);

  const [rows, setRows] = useState<PlatformWithdrawalRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [transferRefs, setTransferRefs] = useState<Record<string, string>>({});
  const [proofKeys, setProofKeys] = useState<Record<string, string>>({});
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listPlatformWithdrawals("pendingApproval");
      setRows(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load withdrawals.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canManage) {
      void load();
    }
  }, [canManage, load]);

  if (!canManage) {
    return (
      <p className="text-sm text-muted-foreground">
        Platform payout manage claim required.
      </p>
    );
  }

  async function onApprove(withdrawalId: string) {
    setBusyId(withdrawalId);
    try {
      await approvePlatformWithdrawal(withdrawalId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Approve failed.");
    } finally {
      setBusyId(null);
    }
  }

  async function onComplete(withdrawalId: string) {
    const transferReference = transferRefs[withdrawalId]?.trim();
    if (!transferReference) {
      setError("Transfer reference is required to complete.");
      return;
    }
    setBusyId(withdrawalId);
    try {
      await completePlatformWithdrawal(
        withdrawalId,
        transferReference,
        proofKeys[withdrawalId]?.trim() || null,
      );
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Complete failed.");
    } finally {
      setBusyId(null);
    }
  }

  async function onFail(withdrawalId: string) {
    setBusyId(withdrawalId);
    try {
      await failPlatformWithdrawal(withdrawalId);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Fail failed.");
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Withdrawal queue (DA1 manual)</CardTitle>
          <CardDescription>
            Approve, complete with bank reference, or fail to return funds to
            available balance.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {loading ? <p className="text-sm text-muted-foreground">Loading…</p> : null}
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          {rows.length === 0 && !loading ? (
            <p className="text-sm text-muted-foreground">No pending withdrawals.</p>
          ) : null}
          {rows.map((row) => (
            <div key={row.id} className="rounded-lg border border-border p-4 text-sm">
              <p className="font-medium text-foreground">
                {formatMinor(row.amountMinor, row.currency)} · org{" "}
                {row.organizationId.slice(0, 8)}…
              </p>
              <p className="text-xs text-muted-foreground">
                Requested {new Date(row.requestedAt).toLocaleString()} · {row.status}
              </p>
              <div className="mt-3 grid gap-2 sm:grid-cols-2">
                <div className="space-y-1">
                  <Label htmlFor={`ref-${row.id}`}>Transfer reference</Label>
                  <Input
                    id={`ref-${row.id}`}
                    value={transferRefs[row.id] ?? ""}
                    onChange={(event) =>
                      setTransferRefs((current) => ({
                        ...current,
                        [row.id]: event.target.value,
                      }))
                    }
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor={`proof-${row.id}`}>Proof object key (optional)</Label>
                  <Input
                    id={`proof-${row.id}`}
                    value={proofKeys[row.id] ?? ""}
                    onChange={(event) =>
                      setProofKeys((current) => ({
                        ...current,
                        [row.id]: event.target.value,
                      }))
                    }
                  />
                </div>
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busyId === row.id}
                  onClick={() => void onApprove(row.id)}
                >
                  Approve
                </Button>
                <Button
                  size="sm"
                  disabled={busyId === row.id}
                  onClick={() => void onComplete(row.id)}
                >
                  Complete
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  disabled={busyId === row.id}
                  onClick={() => void onFail(row.id)}
                >
                  Fail
                </Button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
