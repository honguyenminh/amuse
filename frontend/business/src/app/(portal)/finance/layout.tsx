"use client";

import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

export default function FinanceLayout({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const token = getAccessToken();
  const isWithdrawRoute = pathname === "/finance/withdraw";
  const canAccess = isWithdrawRoute
    ? hasClaim(token, "manage:payout:withdraw:all")
    : hasClaim(token, "read:payout:all");

  if (!canAccess) {
    return (
      <p className="text-sm text-muted-foreground">
        {isWithdrawRoute
          ? "Your organization token does not include withdrawal permission."
          : "Your organization token does not include payout read access."}
      </p>
    );
  }

  return children;
}
