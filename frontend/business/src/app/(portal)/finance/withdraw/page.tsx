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
  createWithdrawal,
  getOrgBalance,
  listWithdrawals,
  type WithdrawalRow,
} from "@/lib/api/financeClient";
import { formatMinor, parseMajorToMinor } from "@/lib/finance/formatMoney";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

const MIN_USD_MAJOR = 10;

export default function FinanceWithdrawPage() {
  const token = getAccessToken();
  const canWithdraw = hasClaim(token, "manage:payout:withdraw:all");

  const [currency, setCurrency] = useState("USD");
  const [amountMajor, setAmountMajor] = useState("");
  const [availableMinor, setAvailableMinor] = useState(0);
  const [cooldownEndsAt, setCooldownEndsAt] = useState<string | null>(null);
  const [blocksWithdrawals, setBlocksWithdrawals] = useState(true);
  const [hasReceivable, setHasReceivable] = useState(false);
  const [history, setHistory] = useState<WithdrawalRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [balance, withdrawals] = await Promise.all([
        getOrgBalance(),
        listWithdrawals(),
      ]);
      const primary = balance.balances.find((row) => row.availableMinor > 0)
        ?? balance.balances[0];
      if (primary) {
        setCurrency(primary.currency);
        setAvailableMinor(primary.availableMinor);
      }
      setCooldownEndsAt(balance.cooldownEndsAt);
      setBlocksWithdrawals(balance.blocksWithdrawals);
      setHasReceivable(balance.hasOutstandingReceivable);
      setHistory(withdrawals);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load withdrawal data.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (canWithdraw) {
      void load();
    }
  }, [canWithdraw, load]);

  const amountMinor = useMemo(() => parseMajorToMinor(amountMajor), [amountMajor]);

  const withdrawDisabled =
    loading
    || submitting
    || blocksWithdrawals
    || hasReceivable
    || cooldownEndsAt !== null
    || amountMinor === null
    || amountMinor <= 0
    || amountMinor > availableMinor;

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (withdrawDisabled || amountMinor === null) {
      return;
    }
    setSubmitting(true);
    setError(null);
    setSuccess(null);
    try {
      const created = await createWithdrawal(amountMinor, currency.trim().toUpperCase());
      setSuccess(
        `Withdrawal requested (${created.status}). Platform ops will approve manual bank transfer.`,
      );
      setAmountMajor("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Withdrawal request failed.");
    } finally {
      setSubmitting(false);
    }
  }

  if (!canWithdraw) {
    return (
      <p className="text-sm text-muted-foreground">
        Your organization token does not include withdrawal permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Request withdrawal</CardTitle>
          <CardDescription>
            DA1 manual rail: requests queue for platform approval. Minimum{" "}
            {MIN_USD_MAJOR} USD equivalent; 7-day cooldown after each completed
            payout.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? <p className="text-sm text-muted-foreground">Loading…</p> : null}
          {blocksWithdrawals ? (
            <p className="mb-4 text-sm text-amber-600">
              Gate B payout profile must be verified.{" "}
              <Link href="/finance/payout-setup" className="underline">
                Complete payout setup
              </Link>
            </p>
          ) : null}
          {hasReceivable ? (
            <p className="mb-4 text-sm text-destructive">
              Withdrawals blocked while receivable is outstanding.
            </p>
          ) : null}
          {cooldownEndsAt ? (
            <p className="mb-4 text-sm text-muted-foreground">
              Cooldown active until {new Date(cooldownEndsAt).toLocaleString()}.
            </p>
          ) : null}
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="withdraw-currency">Currency</Label>
              <Input
                id="withdraw-currency"
                maxLength={3}
                value={currency}
                onChange={(event) => setCurrency(event.target.value.toUpperCase())}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="withdraw-amount">Amount</Label>
              <Input
                id="withdraw-amount"
                inputMode="decimal"
                placeholder="0.00"
                value={amountMajor}
                onChange={(event) => setAmountMajor(event.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Available: {formatMinor(availableMinor, currency)} · min ≈{" "}
                {MIN_USD_MAJOR}.00 USD equivalent
              </p>
            </div>
            {error ? <p className="text-sm text-destructive">{error}</p> : null}
            {success ? <p className="text-sm text-emerald-600">{success}</p> : null}
            <div className="flex gap-2">
              <Button type="submit" disabled={withdrawDisabled}>
                {submitting ? "Submitting…" : "Submit withdrawal"}
              </Button>
              <Button variant="outline" render={<Link href="/finance/balance" />}>
                View balance
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Withdrawal history</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          {history.length === 0 ? (
            <p className="text-muted-foreground">No withdrawals yet.</p>
          ) : (
            history.map((row) => (
              <div key={row.id} className="rounded-md border border-border p-3">
                <p className="font-medium text-foreground">
                  {formatMinor(row.amountMinor, row.currency)} · {row.status}
                </p>
                <p className="text-xs text-muted-foreground">
                  Requested {new Date(row.requestedAt).toLocaleString()}
                  {row.transferReference ? ` · ref ${row.transferReference}` : ""}
                </p>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
