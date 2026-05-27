"use client";

import { PortalShell } from "@/components/portal/PortalShell";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth/AuthProvider";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

type PortalGateProps = {
  children: ReactNode;
};

export function PortalGate({ children }: PortalGateProps) {
  const auth = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
      return;
    }
    if (auth.needsPersonaSelection) {
      router.replace("/select-persona");
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.needsPersonaSelection,
    pathname,
    router,
  ]);

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh flex-col gap-4 p-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full max-w-xl" />
        <Skeleton className="h-32 w-full max-w-xl" />
      </div>
    );
  }

  if (!auth.isAuthenticated || auth.needsPersonaSelection) {
    return null;
  }

  if (auth.bootstrapError) {
    return (
      <div className="flex min-h-dvh flex-col items-center justify-center gap-4 p-6">
        <p className="max-w-md text-center text-sm text-muted-foreground">
          {auth.bootstrapError}
        </p>
        <div className="flex gap-2">
          <Button onClick={() => void auth.retryBootstrap()}>Retry</Button>
          <Button variant="outline" onClick={() => void auth.logout()}>
            Sign out
          </Button>
        </div>
      </div>
    );
  }

  return <PortalShell>{children}</PortalShell>;
}
