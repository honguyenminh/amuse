"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  getOrgBalance,
  listStatements,
  refundPurchase,
  type CurrencyBalanceRow,
  type StatementLineRow,
} from "@/lib/api/financeClient";
import { formatMinor } from "@/lib/finance/formatMoney";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

export default function FinanceBalancePage() {
  const token = getAccessToken();
  const canRead = hasClaim(token, "read:payout:all");
  const canWithdraw = hasClaim(token, "manage:payout:withdraw:all");
  const canRefund = hasClaim(token, "manage:purchase:refund:all");

  const [balances, setBalances] = useState<CurrencyBalanceRow[]>([]);
  const [gateBVerified, setGateBVerified] = useState(false);
  const [blocksWithdrawals, setBlocksWithdrawals] = useState(true);
  const [cooldownEndsAt, setCooldownEndsAt] = useState<string | null>(null);
  const [hasReceivable, setHasReceivable] = useState(false);
  const [statements, setStatements] = useState<StatementLineRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refundReasons, setRefundReasons] = useState<Record<string, string>>({});
  const [refundBusyId, setRefundBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [balance, statementPage] = await Promise.all([
        getOrgBalance(),
        listStatements(1, 10),
      ]);
      setBalances(balance.balances);
      setGateBVerified(balance.gateBVerified);
      setBlocksWithdrawals(balance.blocksWithdrawals);
      setCooldownEndsAt(balance.cooldownEndsAt);
      setHasReceivable(balance.hasOutstandingReceivable);
      setStatements(statementPage.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load balance.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canRead) {
      void load();
    }
  }, [canRead, load]);

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your organization token does not include payout read access.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Seller balance</CardTitle>
          <CardDescription>
            Per-currency pending, available, in-payout, and receivable totals from
            the internal ledger.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {loading ? <p className="text-sm text-muted-foreground">Loading…</p> : null}
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          {!loading && balances.length === 0 ? (
            <p className="text-sm text-muted-foreground">No earnings yet.</p>
          ) : null}
          {balances.map((row) => (
            <div
              key={row.currency}
              className="rounded-lg border border-border p-4 text-sm"
            >
              <p className="font-medium text-foreground">{row.currency}</p>
              <dl className="mt-2 grid grid-cols-2 gap-2 text-muted-foreground">
                <div>
                  <dt>Pending</dt>
                  <dd className="font-medium text-foreground">
                    {formatMinor(row.pendingMinor, row.currency)}
                  </dd>
                </div>
                <div>
                  <dt>Available</dt>
                  <dd className="font-medium text-foreground">
                    {formatMinor(row.availableMinor, row.currency)}
                  </dd>
                </div>
                <div>
                  <dt>In payout</dt>
                  <dd className="font-medium text-foreground">
                    {formatMinor(row.inPayoutMinor, row.currency)}
                  </dd>
                </div>
                <div>
                  <dt>Receivable</dt>
                  <dd className="font-medium text-foreground">
                    {formatMinor(row.receivableMinor, row.currency)}
                  </dd>
                </div>
              </dl>
              {row.usdEquivalentMinor != null ? (
                <p className="mt-2 text-xs text-muted-foreground">
                  ≈ {formatMinor(row.usdEquivalentMinor, "USD")} total (ECB reference)
                </p>
              ) : null}
            </div>
          ))}
          {!gateBVerified || blocksWithdrawals ? (
            <p className="text-sm text-amber-600">
              Complete payout setup (Gate B) before withdrawing.{" "}
              <Link href="/finance/payout-setup" className="underline">
                Set up payouts
              </Link>
            </p>
          ) : null}
          {hasReceivable ? (
            <p className="text-sm text-destructive">
              Withdrawals are blocked while seller receivable is outstanding.
            </p>
          ) : null}
          {cooldownEndsAt ? (
            <p className="text-sm text-muted-foreground">
              Withdrawal cooldown until{" "}
              <span className="font-medium text-foreground">
                {new Date(cooldownEndsAt).toLocaleString()}
              </span>
            </p>
          ) : null}
          {canWithdraw ? (
            <Button render={<Link href="/finance/withdraw" />}>Request withdrawal</Button>
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Recent purchase credits</CardTitle>
          <CardDescription>Statement lines from purchase allocation snapshots.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          {statements.length === 0 ? (
            <p className="text-muted-foreground">No statement lines yet.</p>
          ) : (
            statements.map((line) => (
              <div key={line.id} className="rounded-md border border-border p-3">
                <p className="font-medium text-foreground">
                  {formatMinor(line.amountMinor, line.currency)}
                </p>
                <p className="text-xs text-muted-foreground">
                  Purchase {line.purchaseId.slice(0, 8)}… · track{" "}
                  {line.trackId.slice(0, 8)}… · {line.shareBps / 100}% share
                </p>
                <p className="text-xs text-muted-foreground">
                  {new Date(line.creditedAt).toLocaleString()}
                </p>
                {canRefund ? (
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <input
                      className="h-8 min-w-[12rem] flex-1 rounded-md border border-input bg-background px-2 text-xs"
                      placeholder="Refund reason"
                      value={refundReasons[line.purchaseId] ?? ""}
                      onChange={(event) =>
                        setRefundReasons((prev) => ({
                          ...prev,
                          [line.purchaseId]: event.target.value,
                        }))
                      }
                    />
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={refundBusyId === line.purchaseId}
                      onClick={async () => {
                        const reason = refundReasons[line.purchaseId]?.trim();
                        if (!reason) {
                          setError("Refund reason is required.");
                          return;
                        }
                        setRefundBusyId(line.purchaseId);
                        setError(null);
                        try {
                          await refundPurchase(line.purchaseId, { reason });
                          await load();
                        } catch (err) {
                          setError(
                            err instanceof Error ? err.message : "Refund failed.",
                          );
                        } finally {
                          setRefundBusyId(null);
                        }
                      }}
                    >
                      Refund purchase
                    </Button>
                  </div>
                ) : null}
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
