"use client";

import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import type { ReactNode } from "react";

/**
 * Wraps every (b2c) route. It deliberately does **not** redirect to /login
 * when the visitor is anonymous — public browse pages (home, artist, release)
 * are accessible without an account, Spotify/YouTube-Music style. Login is
 * only required for actions that touch user identity (playback, library, etc.)
 * and those guards live next to the action that needs them.
 *
 * Responsibilities here:
 * - Show a brief "Loading session…" while the AuthProvider tries the refresh
 *   cookie. Anonymous visitors fall through quickly.
 * - Surface a `bootstrapError` UI if a logged-in session failed to load its
 *   listener profile, so the user can retry rather than be silently stuck.
 */
export function B2cGate({ children }: { children: ReactNode }) {
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <div className="flex h-dvh items-center justify-center bg-background p-8">
        <Text variant="body-large">Loading…</Text>
      </div>
    );
  }

  if (auth.isAuthenticated && auth.bootstrapError) {
    return (
      <div className="flex h-dvh flex-col items-center justify-center gap-4 bg-background p-8">
        <Text variant="headline-medium">Could not prepare listener profile</Text>
        <Text variant="body-medium">{auth.bootstrapError}</Text>
        <Button type="button" onClick={() => void auth.retryBootstrap()}>
          Retry
        </Button>
      </div>
    );
  }

  return children;
}
