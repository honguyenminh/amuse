"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import Link from "next/link";
import type { ReactNode } from "react";

type LibraryAuthGateProps = {
  children: ReactNode;
};

export function LibraryAuthGate({ children }: LibraryAuthGateProps) {
  const auth = useAuth();

  if (!auth.isReady) {
    return (
      <Card>
        <Text variant="body-medium">Loading…</Text>
      </Card>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <Card>
        <Text variant="title-large">Log in to view your library</Text>
        <Text variant="label-medium" className="mt-1 text-on-surface-variant">
          Playlists, liked tracks, and saved releases are available after you sign in.
        </Text>
        <Link href="/login" className="mt-4 inline-block">
          <Button type="button">Log in</Button>
        </Link>
      </Card>
    );
  }

  return children;
}
