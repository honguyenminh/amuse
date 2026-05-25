"use client";

import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

export function B2cGate({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (auth.isReady && !auth.isAuthenticated) {
      router.replace("/login");
    }
  }, [auth.isReady, auth.isAuthenticated, router]);

  if (!auth.isReady) {
    return (
      <div className="flex min-h-full items-center justify-center bg-background p-8">
        <Text variant="body-large">Loading session…</Text>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  if (auth.bootstrapError) {
    return (
      <div className="flex min-h-full flex-col items-center justify-center gap-4 bg-background p-8">
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
