"use client";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { contextLabel } from "@/lib/auth/resolveBusinessPersonas";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { getOrgBalance, getPayoutProfile } from "@/lib/api/financeClient";
import { isPlatformPersonaActive } from "@/lib/auth/resolveBusinessPersonas";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function DashboardPage() {
  const auth = useAuth();
  const token = getAccessToken();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);
  const canReadPayout = !isPlatform && hasClaim(token, "read:payout:all");

  const [gateBVerified, setGateBVerified] = useState<boolean | null>(null);
  const [payoutStatus, setPayoutStatus] = useState<string | null>(null);

  useEffect(() => {
    if (!canReadPayout) {
      return;
    }
    void (async () => {
      try {
        const [balance, profile] = await Promise.all([
          getOrgBalance(),
          getPayoutProfile(),
        ]);
        setGateBVerified(balance.gateBVerified);
        setPayoutStatus(profile?.verificationStatus ?? "notStarted");
      } catch {
        setGateBVerified(false);
        setPayoutStatus(null);
      }
    })();
  }, [canReadPayout]);

  const personaLabel =
    auth.activePersona && auth.businessPersonas.length > 0
      ? contextLabel(auth.activePersona, auth.businessPersonas)
      : "Unknown";

  const showPayoutSetupCta =
    canReadPayout && gateBVerified === false && payoutStatus !== "underReview";

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      {showPayoutSetupCta ? (
        <Card className="border-amber-500/40 bg-amber-500/5">
          <CardHeader>
            <CardTitle>Complete payout setup to withdraw</CardTitle>
            <CardDescription>
              Seller earnings can accrue before Gate B is verified, but withdrawals
              stay disabled until your payout profile is approved.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              <Button render={<Link href="/finance/payout-setup" />}>
                Set up payouts
              </Button>
              <Button variant="outline" render={<Link href="/finance/balance" />}>
                View balance
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Welcome to Amuse Console</CardTitle>
          <CardDescription>
            Base portal shell for business and platform administration.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <p>
            Active persona:{" "}
            <span className="font-medium text-foreground">{personaLabel}</span>
          </p>
          <p>
            Account:{" "}
            <span className="font-medium text-foreground">
              {auth.account?.accountId ?? "—"}
            </span>
          </p>
          {canReadPayout && payoutStatus ? (
            <p>
              Payout profile:{" "}
              <span className="font-medium text-foreground">{payoutStatus}</span>
            </p>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
