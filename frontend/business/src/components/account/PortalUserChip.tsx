"use client";

import { UserAvatar } from "@/components/account/UserAvatar";
import { useAuth } from "@/lib/auth/AuthProvider";
import { cn } from "@/lib/utils";
import Link from "next/link";

type PortalUserChipProps = {
  compact?: boolean;
  className?: string;
};

export function PortalUserChip({ compact = false, className }: PortalUserChipProps) {
  const auth = useAuth();
  const displayName =
    auth.portalProfile?.displayName ?? auth.account?.email?.split("@")[0] ?? "Account";
  const email = auth.account?.email ?? null;

  return (
    <Link
      href="/settings/account"
      className={cn(
        "flex items-center gap-2 rounded-lg hover:bg-sidebar-accent",
        compact ? "px-2 py-1" : "px-2 py-2",
        className,
      )}
    >
      <UserAvatar
        displayName={auth.portalProfile?.displayName}
        email={email}
        accentSeed={auth.portalProfile?.avatarAccentSeed}
        avatarUrl={auth.portalProfile?.avatarUrl}
        size={compact ? "sm" : "md"}
      />
      {!compact ? (
        <span className="min-w-0 flex-1 text-left text-sm leading-tight">
          <span className="block truncate font-medium">{displayName}</span>
          {email ? (
            <span className="block truncate text-xs text-muted-foreground">{email}</span>
          ) : null}
        </span>
      ) : (
        <span className="hidden text-sm font-medium lg:inline">{displayName}</span>
      )}
    </Link>
  );
}
