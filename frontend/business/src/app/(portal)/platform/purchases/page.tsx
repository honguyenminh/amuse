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
  listPlatformPurchases,
  refundPurchase,
  type PlatformPurchaseRow,
} from "@/lib/api/financeClient";
import { formatMinor } from "@/lib/finance/formatMoney";
import { canManagePlatformPurchases } from "@/lib/auth/platformClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { useCallback, useEffect, useState } from "react";

export default function PlatformPurchasesPage() {
  const token = getAccessToken();
  const canManage = canManagePlatformPurchases(token);

  const [query, setQuery] = useState("");
  const [rows, setRows] = useState<PlatformPurchaseRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [feeBearers, setFeeBearers] = useState<Record<string, "platform" | "seller">>({});
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listPlatformPurchases(query, "paid");
      setRows(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load purchases.");
    } finally {
      setLoading(false);
    }
  }, [query]);

  useEffect(() => {
    if (canManage) {
      void load();
    }
  }, [canManage, load]);

  if (!canManage) {
    return (
      <p className="text-sm text-muted-foreground">
        Platform purchases manage claim required.
      </p>
    );
  }

  async function onRefund(purchaseId: string) {
    const reason = reasons[purchaseId]?.trim();
    if (!reason) {
      setError("Refund reason is required.");
      return;
    }
    const refundFeeBearer = feeBearers[purchaseId] ?? "platform";
    setBusyId(purchaseId);
    setError(null);
    try {
      await refundPurchase(purchaseId, { reason, refundFeeBearer });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Refund failed.");
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Purchase refunds</CardTitle>
          <CardDescription>
            Search paid purchases and issue operator refunds with fee bearer selection.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <form
            className="flex flex-wrap items-end gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              void load();
            }}
          >
            <div className="min-w-[16rem] flex-1 space-y-1">
              <Label htmlFor="purchase-query">Search (purchase id)</Label>
              <Input
                id="purchase-query"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Purchase UUID or prefix"
              />
            </div>
            <Button type="submit" disabled={loading}>
              Search
            </Button>
          </form>

          {loading ? <p className="text-sm text-muted-foreground">Loading…</p> : null}
          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          {rows.length === 0 && !loading ? (
            <p className="text-sm text-muted-foreground">No paid purchases found.</p>
          ) : (
            rows.map((row) => (
              <div key={row.id} className="space-y-3 rounded-lg border border-border p-4 text-sm">
                <div>
                  <p className="font-medium text-foreground">
                    {formatMinor(row.priceSnapshotMinor, row.currency)} · {row.purchasedUnit}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {row.id} · buyer {row.buyerAccountId.slice(0, 8)}… ·{" "}
                    {row.paymentStatus}
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  <div className="space-y-1">
                    <Label htmlFor={`reason-${row.id}`}>Refund reason</Label>
                    <Input
                      id={`reason-${row.id}`}
                      value={reasons[row.id] ?? ""}
                      onChange={(event) =>
                        setReasons((prev) => ({ ...prev, [row.id]: event.target.value }))
                      }
                    />
                  </div>
                  <div className="space-y-1">
                    <Label htmlFor={`bearer-${row.id}`}>Refund fee bearer</Label>
                    <select
                      id={`bearer-${row.id}`}
                      className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                      value={feeBearers[row.id] ?? "platform"}
                      onChange={(event) =>
                        setFeeBearers((prev) => ({
                          ...prev,
                          [row.id]: event.target.value as "platform" | "seller",
                        }))
                      }
                    >
                      <option value="platform">Platform</option>
                      <option value="seller">Seller</option>
                    </select>
                  </div>
                </div>
                <Button
                  disabled={busyId === row.id}
                  onClick={() => void onRefund(row.id)}
                >
                  Issue refund
                </Button>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
