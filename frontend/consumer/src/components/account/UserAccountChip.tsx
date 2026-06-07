"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { UserAvatar } from "@/components/account/UserAvatar";
import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { cn } from "@/lib/cn";
import Link from "next/link";
import { useRef, useState } from "react";

type UserAccountChipProps = {
  compact?: boolean;
  menuPlacement?: "top" | "bottom";
  className?: string;
};

export function UserAccountChip({
  compact = false,
  menuPlacement = "top",
  className,
}: UserAccountChipProps) {
  const auth = useAuth();
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  if (!auth.isAuthenticated || !auth.listenerProfile) {
    return null;
  }

  const displayName = auth.listenerProfile.displayName ?? "Listener";
  const email = auth.account?.email ?? null;

  return (
    <div className={cn("relative", className)}>
      <button
        ref={triggerRef}
        type="button"
        className={cn(
          "flex items-center gap-2 text-left",
          compact
            ? "size-10 shrink-0 justify-center rounded-full hover:bg-surface-variant"
            : "w-full rounded-xl border-2 border-outline bg-surface px-2 py-2 hover:bg-surface-variant",
        )}
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((value) => !value)}
      >
        <UserAvatar
          displayName={auth.listenerProfile.displayName}
          email={email}
          accentSeed={auth.listenerProfile.avatarAccentSeed}
          avatarUrl={auth.listenerProfile.avatarUrl}
          size="md"
        />
        {!compact ? (
          <span className="min-w-0 flex-1">
            <Text variant="label-medium" className="block truncate">
              {displayName}
            </Text>
            {email ? (
              <Text variant="label-medium" className="block truncate text-on-surface-variant">
                {email}
              </Text>
            ) : null}
          </span>
        ) : null}
      </button>

      <AnchoredPopup
        open={open}
        onClose={() => setOpen(false)}
        anchorRef={triggerRef}
        preferredPlacement={menuPlacement === "top" ? "top" : "bottom"}
        align={compact && menuPlacement === "bottom" ? "end" : "start"}
        className="w-56 rounded-xl border-2 border-outline bg-surface p-2 shadow-none"
        role="menu"
      >
        <Link
          href="/settings/account"
          role="menuitem"
          className="block rounded-lg px-3 py-2 hover:bg-surface-variant"
          onClick={() => setOpen(false)}
        >
          Account
        </Link>
        <Link
          href="/settings"
          role="menuitem"
          className="block rounded-lg px-3 py-2 hover:bg-surface-variant"
          onClick={() => setOpen(false)}
        >
          Playback settings
        </Link>
        <Button
          type="button"
          variant="outlined"
          className="mt-2 w-full"
          onClick={() => {
            setOpen(false);
            void auth.logout();
          }}
        >
          Log out
        </Button>
      </AnchoredPopup>
    </div>
  );
}
